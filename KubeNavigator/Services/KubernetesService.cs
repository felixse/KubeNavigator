using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.KubeConfigModels;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace KubeNavigator.Services;

public record NodeMetrics(
    Dictionary<string, ResourceUsage> NodeUsage,
    int TotalNodes,
    int ReadyNodes,
    int TotalPods,
    int RequestedPods,
    CpuQuantity TotalCpu,
    MemoryQuantity TotalMemory
);

public partial class KubernetesService
{
    private readonly ILogger<KubernetesService> _logger;
    private IKubernetes? _kubernetes;
    private readonly string _contextName;
    private readonly ISettingsService _settingsService;

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        Kubernetes k8s,
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (k8s.Credentials is not null)
        {
            await k8s.Credentials.ProcessHttpRequestAsync(request, cancellationToken);
        }

        return await k8s.HttpClient.SendAsync(request, cancellationToken);
    }

    public KubernetesService(
        string contextName,
        ILogger<KubernetesService> logger,
        ISettingsService settingsService
    )
    {
        _contextName = contextName;
        _logger = logger;
        _settingsService = settingsService;
    }

    public static async Task<IReadOnlyList<string>> LoadContextNamesAsync()
    {
        var configContent = await File.ReadAllTextAsync(
            KubernetesClientConfiguration.KubeConfigDefaultLocation
        );
        var config = KubernetesYaml.Deserialize<K8SConfiguration>(configContent);
        return config.Contexts.Select(c => c.Name).ToList();
    }

    public async Task InitializeAsync()
    {
        Log.CreatingKubernetesClient(_logger, _contextName);
        var config = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(
            new FileInfo(KubernetesClientConfiguration.KubeConfigDefaultLocation),
            _contextName
        );
        _kubernetes = new Kubernetes(config);
        Log.KubernetesClientCreated(_logger, _contextName);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Log.TestingConnection(_logger, _contextName);
            await _kubernetes.CoreV1.ListNamespaceAsync(cancellationToken: cancellationToken);
            Log.ConnectionSuccessful(_logger, _contextName);
            return true;
        }
        catch (Exception ex)
        {
            Log.ConnectionFailed(_logger, _contextName, ex);
            return false;
        }
    }

    public async Task<GenericKubernetesItems<T>> ListResourcesAsync<T>(
        ResourceType resourceType,
        CancellationToken cancellationToken = default
    )
        where T : IKubernetesObject<V1ObjectMeta>
    {
        try
        {
            Log.ListingResources(_logger, resourceType.Plural, _contextName);
            var client = new GenericClient(
                _kubernetes,
                resourceType.Group,
                resourceType.Version,
                resourceType.Plural
            );
            var list = await client.ListAsync<GenericKubernetesItems<T>>();
            foreach (var item in list.Items)
            {
                item.Kind = resourceType.Kind;
                item.ApiVersion = resourceType.Version;
            }
            Log.ResourcesListed(_logger, list.Items.Count, resourceType.Plural, _contextName);
            return list;
        }
        catch (Exception ex)
        {
            Log.ListResourcesFailed(_logger, resourceType.Plural, _contextName, ex);
            throw;
        }
    }

    public Watcher<T> WatchResources<T>(
        ResourceType resourceType,
        string? resourceVersion,
        Action<WatchEventType, T> onEvent,
        Action<Exception>? onError = null,
        Action? onClosed = null
    )
        where T : IKubernetesObject<V1ObjectMeta>
    {
        Log.StartingWatcher(_logger, resourceType.Plural, _contextName);

        var k8s = (Kubernetes)_kubernetes!;

        // Build the watch URL:
        //   core group  → /api/{version}/{plural}
        //   named group → /apis/{group}/{version}/{plural}
        var basePath = string.IsNullOrEmpty(resourceType.Group)
            ? $"api/{resourceType.Version}/{resourceType.Plural}"
            : $"apis/{resourceType.Group}/{resourceType.Version}/{resourceType.Plural}";

        var query = "watch=true&allowWatchBookmarks=true&timeoutSeconds=300";
        if (!string.IsNullOrEmpty(resourceVersion))
        {
            query += $"&resourceVersion={Uri.EscapeDataString(resourceVersion)}";
        }

        var watchUri = new Uri(k8s.BaseUri, $"{basePath}?{query}");

        return new Watcher<T>(
            async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, watchUri);
                if (k8s.Credentials is not null)
                {
                    await k8s.Credentials.ProcessHttpRequestAsync(request, CancellationToken.None);
                }

                // ResponseHeadersRead is critical: without it, SendAsync buffers
                // the entire response body before returning, which for a long-lived
                // streaming watch means the Watcher only sees events after the
                // server closes the connection — defeating the purpose of watching.
                var response = await k8s.HttpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None
                );
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                return new StreamReader(stream);
            },
            onEvent,
            onError,
            onClosed
        );
    }

    public async Task<IEnumerable<(string ResourceName, string Error)>> DeleteResourcesAsync(
        ResourceType resourceType,
        IEnumerable<IKubernetesObject<V1ObjectMeta>> resources,
        CancellationToken cancellationToken = default
    )
    {
        var client = new GenericClient(
            _kubernetes,
            resourceType.Group,
            resourceType.Version,
            resourceType.Plural
        );
        var errors = new List<(string ResourceName, string Error)>();
        var resourcesList = resources.ToList();

        Log.DeletingResources(_logger, resourcesList.Count, resourceType.Plural, _contextName);

        foreach (var resource in resourcesList)
        {
            try
            {
                Log.DeletingResource(
                    _logger,
                    resource.Name(),
                    resource.Namespace(),
                    resourceType.Plural,
                    _contextName
                );

                if (resourceType.IsNamespaceScoped)
                {
                    await client.DeleteNamespacedAsync<GenericKubernetesObject>(
                        resource.Namespace(),
                        resource.Name()
                    );
                }
                else
                {
                    await client.DeleteAsync<GenericKubernetesObject>(resource.Name());
                }

                Log.ResourceDeleted(
                    _logger,
                    resource.Name(),
                    resource.Namespace(),
                    resourceType.Plural,
                    _contextName
                );
            }
            catch (Exception ex)
            {
                Log.DeleteResourceFailed(
                    _logger,
                    resource.Name(),
                    resource.Namespace(),
                    resourceType.Plural,
                    _contextName,
                    ex
                );
                errors.Add((resource.Name(), ex.Message));
            }
        }

        if (errors.Any())
        {
            Log.DeleteResourcesCompletedWithErrors(
                _logger,
                errors.Count,
                resourcesList.Count,
                resourceType.Plural,
                _contextName
            );
        }
        else
        {
            Log.DeleteResourcesCompleted(
                _logger,
                resourcesList.Count,
                resourceType.Plural,
                _contextName
            );
        }

        return errors;
    }

    public async Task<Stream> OpenPodLogStreamAsync(
        V1Pod pod,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.OpeningPodLogStream(_logger, pod.Name(), pod.Namespace(), _contextName);
            var stream = await _kubernetes.CoreV1.ReadNamespacedPodLogAsync(
                pod.Name(),
                pod.Namespace(),
                pretty: true,
                follow: true,
                tailLines: 1000,
                cancellationToken: cancellationToken
            );
            Log.PodLogStreamOpened(_logger, pod.Name(), pod.Namespace(), _contextName);
            return stream;
        }
        catch (Exception ex)
        {
            Log.OpenPodLogStreamFailed(_logger, pod.Name(), pod.Namespace(), _contextName, ex);
            throw;
        }
    }

    public async Task<string> ReadPodLogsAsync(
        V1Pod pod,
        CancellationToken cancellationToken = default
    )
    {
        using var stream = await _kubernetes.CoreV1.ReadNamespacedPodLogAsync(
            pod.Name(),
            pod.Namespace(),
            pretty: true,
            follow: false,
            cancellationToken: cancellationToken
        );
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task<PodExecSession> OpenPodExecSessionAsync(
        V1Pod pod,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.OpeningPodExecSession(_logger, pod.Name(), pod.Namespace(), _contextName);
            var webSocket = await _kubernetes.WebSocketNamespacedPodExecAsync(
                pod.Name(),
                pod.Namespace(),
                ["sh", "-c", "clear; (bash || sh || echo 'no shell found')"],
                cancellationToken: cancellationToken
            );
            Log.PodExecSessionOpened(_logger, pod.Name(), pod.Namespace(), _contextName);
            return new PodExecSession(webSocket);
        }
        catch (Exception ex)
        {
            Log.OpenPodExecSessionFailed(_logger, pod.Name(), pod.Namespace(), _contextName, ex);
            throw;
        }
    }

    public async Task<System.Net.WebSockets.WebSocket> OpenPodPortForwardAsync(
        IKubernetesObject<V1ObjectMeta> resource,
        int targetPort,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.OpeningPodPortForward(
                _logger,
                resource.Name(),
                resource.Namespace(),
                targetPort,
                _contextName
            );
            var webSocket = await _kubernetes.WebSocketNamespacedPodPortForwardAsync(
                resource.Name(),
                resource.Namespace(),
                [targetPort],
                cancellationToken: cancellationToken
            );
            Log.PodPortForwardOpened(
                _logger,
                resource.Name(),
                resource.Namespace(),
                targetPort,
                _contextName
            );
            return webSocket;
        }
        catch (Exception ex)
        {
            Log.OpenPodPortForwardFailed(
                _logger,
                resource.Name(),
                resource.Namespace(),
                targetPort,
                _contextName,
                ex
            );
            throw;
        }
    }

    public async Task<string> GetResourceAsYamlAsync(
        ResourceType resourceType,
        string resourceName,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.GettingResourceYaml(
                _logger,
                resourceName,
                resourceNamespace ?? string.Empty,
                resourceType.Plural,
                _contextName
            );

            var basePath = string.IsNullOrEmpty(resourceType.Group)
                ? $"api/{resourceType.Version}"
                : $"apis/{resourceType.Group}/{resourceType.Version}";

            var path =
                resourceType.IsNamespaceScoped && !string.IsNullOrEmpty(resourceNamespace)
                    ? $"{basePath}/namespaces/{resourceNamespace}/{resourceType.Plural}/{resourceName}"
                    : $"{basePath}/{resourceType.Plural}/{resourceName}";

            var k8s = (Kubernetes)_kubernetes!;
            var requestUri = new Uri(k8s.BaseUri, path);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var result = await SendAuthenticatedAsync(k8s, request, cancellationToken);
            result.EnsureSuccessStatusCode();

            var json = await result.Content.ReadAsStringAsync(cancellationToken);

            if (_settingsService.Settings.HideManagedFields)
            {
                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(json);
                jsonNode?["metadata"]?.AsObject().Remove("managedFields");
                json = jsonNode?.ToJsonString() ?? json;
            }

            var deserializer = YamlSerializerFactory.Deserializer;
            var resource = deserializer.Deserialize(new StringReader(json));
            var yaml = YamlSerializerFactory.Serializer.Serialize(resource!);

            Log.ResourceYamlRetrieved(
                _logger,
                resourceName,
                resourceNamespace ?? string.Empty,
                resourceType.Plural,
                _contextName
            );
            return yaml;
        }
        catch (Exception ex)
        {
            Log.GetResourceYamlFailed(
                _logger,
                resourceName,
                resourceNamespace ?? string.Empty,
                resourceType.Plural,
                _contextName,
                ex
            );
            throw;
        }
    }

    public async Task PatchResourceFromYamlAsync(
        string originalYaml,
        string modifiedYaml,
        ResourceType resourceType,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.ApplyingResourceYaml(_logger, resourceType.Plural, _contextName);

            var resourceNameFromYaml = ExtractResourceNameFromYaml(modifiedYaml);

            var basePath = string.IsNullOrEmpty(resourceType.Group)
                ? $"api/{resourceType.Version}"
                : $"apis/{resourceType.Group}/{resourceType.Version}";

            var path =
                resourceType.IsNamespaceScoped && !string.IsNullOrEmpty(resourceNamespace)
                    ? $"{basePath}/namespaces/{resourceNamespace}/{resourceType.Plural}/{resourceNameFromYaml}"
                    : $"{basePath}/{resourceType.Plural}/{resourceNameFromYaml}";

            // Convert both YAML versions to JSON
            var deserializer = YamlSerializerFactory.Deserializer;

            var originalObject = deserializer.Deserialize(new StringReader(originalYaml));
            var originalJson = KubernetesJson.Serialize(originalObject);

            var modifiedObject = deserializer.Deserialize(new StringReader(modifiedYaml));
            var modifiedJson = KubernetesJson.Serialize(modifiedObject);

            // Compute RFC 6902 JSON Patch between original and modified
            var patch = JsonDiffPatcher.Diff(
                originalJson,
                modifiedJson,
                new JsonPatchDeltaFormatter()
            );

            if (patch is not JsonArray { Count: > 0 })
            {
                Log.ResourceYamlApplied(
                    _logger,
                    resourceNameFromYaml,
                    resourceNamespace ?? string.Empty,
                    resourceType.Plural,
                    _contextName
                );
                return; // No changes detected
            }

            var patchJson = patch.ToJsonString();
            _logger.LogDebug(
                "Sending JSON Patch with {OperationCount} operations to {Path}",
                patch.AsArray().Count,
                path
            );

            var k8s = (Kubernetes)_kubernetes!;
            var requestUri = new Uri(k8s.BaseUri, path);
            var content = new StringContent(
                patchJson,
                Encoding.UTF8,
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/json-patch+json")
            );

            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = content,
            };

            var result = await SendAuthenticatedAsync(k8s, request, cancellationToken);

            if (!result.IsSuccessStatusCode)
            {
                var responseBody = await result.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Failed to patch resource ({result.StatusCode}): {responseBody}"
                );
            }

            Log.ResourceYamlApplied(
                _logger,
                resourceNameFromYaml,
                resourceNamespace ?? string.Empty,
                resourceType.Plural,
                _contextName
            );
        }
        catch (Exception ex)
        {
            Log.ApplyResourceYamlFailed(_logger, resourceType.Plural, _contextName, ex);
            throw;
        }
    }

    public async Task CreateResourceFromYamlAsync(
        string yaml,
        ResourceType resourceType,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.CreatingResourceFromYaml(_logger, resourceType.Plural, _contextName);

            var deserializer = YamlSerializerFactory.Deserializer;
            var yamlObject = deserializer.Deserialize(new StringReader(yaml));
            var json = KubernetesJson.Serialize(yamlObject);

            // When namespace-scoped, always resolve the namespace from the
            // YAML body so the URL is correct even when no explicit namespace
            // was provided by the caller (e.g. "All Namespaces" filter).
            var effectiveNamespace = resourceNamespace;
            if (resourceType.IsNamespaceScoped && string.IsNullOrEmpty(effectiveNamespace))
            {
                effectiveNamespace = ExtractResourceNamespaceFromYaml(yaml);
            }

            var basePath = string.IsNullOrEmpty(resourceType.Group)
                ? $"api/{resourceType.Version}"
                : $"apis/{resourceType.Group}/{resourceType.Version}";

            var path =
                resourceType.IsNamespaceScoped && !string.IsNullOrEmpty(effectiveNamespace)
                    ? $"{basePath}/namespaces/{effectiveNamespace}/{resourceType.Plural}"
                    : $"{basePath}/{resourceType.Plural}";

            var k8s = (Kubernetes)_kubernetes!;
            var requestUri = new Uri(k8s.BaseUri, path);
            var content = new StringContent(
                json,
                Encoding.UTF8,
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
            );

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = content,
            };
            var result = await SendAuthenticatedAsync(k8s, request, cancellationToken);

            if (!result.IsSuccessStatusCode)
            {
                var responseBody = await result.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Failed to create resource ({result.StatusCode}): {responseBody}"
                );
            }

            var resourceName = ExtractResourceNameFromYaml(yaml);
            Log.ResourceCreatedFromYaml(
                _logger,
                resourceName,
                effectiveNamespace ?? string.Empty,
                resourceType.Plural,
                _contextName
            );
        }
        catch (Exception ex)
        {
            Log.CreateResourceFromYamlFailed(_logger, resourceType.Plural, _contextName, ex);
            throw;
        }
    }

    public async Task SaveConfigMapAsync(V1ConfigMap configMap)
    {
        await _kubernetes.CoreV1.ReplaceNamespacedConfigMapAsync(
            configMap,
            configMap.Metadata.Name,
            configMap.Metadata.Namespace()
        );
    }

    public async Task SaveSecretAsync(V1Secret secret)
    {
        await _kubernetes.CoreV1.ReplaceNamespacedSecretAsync(
            secret,
            secret.Metadata.Name,
            secret.Metadata.Namespace()
        );
    }

    public async Task<Dictionary<(string Namespace, string Name), ResourceUsage>?> GetPodMetricsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.FetchingPodMetrics(_logger, _contextName);
            var podMetricsList = await _kubernetes!.GetKubernetesPodsMetricsAsync();

            var metrics = new Dictionary<(string Namespace, string Name), ResourceUsage>();
            foreach (var podMetric in podMetricsList.Items)
            {
                var name = podMetric.Metadata?.Name;
                var ns = podMetric.Metadata?.NamespaceProperty;
                if (name == null || ns == null)
                    continue;

                var cpu = CpuQuantity.Zero;
                var memory = MemoryQuantity.Zero;
                if (podMetric.Containers != null)
                {
                    foreach (var container in podMetric.Containers)
                    {
                        if (container.Usage == null)
                            continue;
                        if (container.Usage.TryGetValue("cpu", out var cpuQty))
                            cpu += CpuQuantity.FromResourceQuantity(cpuQty);
                        if (container.Usage.TryGetValue("memory", out var memQty))
                            memory += MemoryQuantity.FromResourceQuantity(memQty);
                    }
                }

                metrics[(ns, name)] = new ResourceUsage(cpu, memory);
            }

            Log.PodMetricsFetched(_logger, metrics.Count, _contextName);
            return metrics;
        }
        catch (Exception ex)
        {
            Log.GetPodMetricsFailed(_logger, _contextName, ex);
            return null;
        }
    }

    public async Task<NodeMetrics?> GetNodeMetricsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.FetchingNodeMetrics(_logger, _contextName);
            var nodeMetricsListTask = _kubernetes!.GetKubernetesNodesMetricsAsync();
            var nodesTask = _kubernetes.CoreV1.ListNodeAsync(
                cancellationToken: cancellationToken
            );
            var podsTask = _kubernetes.CoreV1.ListPodForAllNamespacesAsync(
                cancellationToken: cancellationToken
            );
            await Task.WhenAll(nodeMetricsListTask, nodesTask, podsTask);

            var nodeMetricsList = nodeMetricsListTask.Result;
            var metrics = new Dictionary<string, ResourceUsage>();
            foreach (var nodeMetric in nodeMetricsList.Items)
            {
                var name = nodeMetric.Metadata?.Name;
                if (name == null)
                    continue;

                var cpu = CpuQuantity.Zero;
                var memory = MemoryQuantity.Zero;
                if (nodeMetric.Usage != null)
                {
                    if (nodeMetric.Usage.TryGetValue("cpu", out var cpuQty))
                        cpu = CpuQuantity.FromResourceQuantity(cpuQty);
                    if (nodeMetric.Usage.TryGetValue("memory", out var memQty))
                        memory = MemoryQuantity.FromResourceQuantity(memQty);
                }

                metrics[name] = new ResourceUsage(cpu, memory);
            }

            var nodes = nodesTask.Result;
            var totalNodes = nodes.Items.Count;
            var readyNodes = nodes.Items.Count(n =>
                n.Status?.Conditions?.Any(c => c.Type == "Ready" && c.Status == "True") == true
            );

            var totalCpu = CpuQuantity.Zero;
            var totalMemory = MemoryQuantity.Zero;
            foreach (var node in nodes.Items)
            {
                if (node.Status?.Capacity != null)
                {
                    if (node.Status.Capacity.TryGetValue("cpu", out var cpuQty))
                        totalCpu += CpuQuantity.FromResourceQuantity(cpuQty);
                    if (node.Status.Capacity.TryGetValue("memory", out var memQty))
                        totalMemory += MemoryQuantity.FromResourceQuantity(memQty);
                }
            }

            var pods = podsTask.Result;
            var totalPods = pods.Items.Count(p =>
                p.Status?.Phase is not "Failed"
            );
            var requestedPods = pods.Items.Count;

            Log.NodeMetricsFetched(_logger, metrics.Count, _contextName);
            return new NodeMetrics(
                metrics,
                totalNodes,
                readyNodes,
                totalPods,
                requestedPods,
                totalCpu,
                totalMemory
            );
        }
        catch (Exception ex)
        {
            Log.GetNodeMetricsFailed(_logger, _contextName, ex);
            return null;
        }
    }



    private string ExtractResourceNameFromYaml(string yaml)
    {
        try
        {
            var resource = KubernetesYaml.Deserialize<GenericKubernetesObject>(yaml);
            return resource.Name();
        }
        catch (Exception ex)
        {
            Log.ExtractResourceNameFailed(_logger, _contextName, ex);
            return "unknown";
        }
    }

    private string? ExtractResourceNamespaceFromYaml(string yaml)
    {
        try
        {
            var resource = KubernetesYaml.Deserialize<GenericKubernetesObject>(yaml);
            return resource.Namespace();
        }
        catch (Exception ex)
        {
            Log.ExtractResourceNamespaceFailed(_logger, _contextName, ex);
            return null;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Creating Kubernetes client for context {ContextName}"
        )]
        public static partial void CreatingKubernetesClient(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Information,
            Message = "Kubernetes client created for context {ContextName}"
        )]
        public static partial void KubernetesClientCreated(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Information,
            Message = "Testing connection to cluster {ContextName}"
        )]
        public static partial void TestingConnection(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Information,
            Message = "Connection to cluster {ContextName} successful"
        )]
        public static partial void ConnectionSuccessful(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Error,
            Message = "Connection to cluster {ContextName} failed"
        )]
        public static partial void ConnectionFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2006,
            Level = LogLevel.Information,
            Message = "Listing {ResourceType} resources in cluster {ContextName}"
        )]
        public static partial void ListingResources(
            ILogger logger,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2007,
            Level = LogLevel.Information,
            Message = "Listed {Count} {ResourceType} resources in cluster {ContextName}"
        )]
        public static partial void ResourcesListed(
            ILogger logger,
            int count,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2008,
            Level = LogLevel.Error,
            Message = "Failed to list {ResourceType} resources in cluster {ContextName}"
        )]
        public static partial void ListResourcesFailed(
            ILogger logger,
            string resourceType,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2009,
            Level = LogLevel.Information,
            Message = "Starting watcher for {ResourceType} resources in cluster {ContextName}"
        )]
        public static partial void StartingWatcher(
            ILogger logger,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2010,
            Level = LogLevel.Information,
            Message = "Deleting {Count} {ResourceType} resources in cluster {ContextName}"
        )]
        public static partial void DeletingResources(
            ILogger logger,
            int count,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2011,
            Level = LogLevel.Information,
            Message = "Deleting {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void DeletingResource(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2012,
            Level = LogLevel.Information,
            Message = "Deleted {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void ResourceDeleted(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2013,
            Level = LogLevel.Error,
            Message = "Failed to delete {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void DeleteResourceFailed(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2014,
            Level = LogLevel.Warning,
            Message = "Deleted {SuccessCount} of {TotalCount} {ResourceType} resources in cluster {ContextName} with errors"
        )]
        public static partial void DeleteResourcesCompletedWithErrors(
            ILogger logger,
            int successCount,
            int totalCount,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2015,
            Level = LogLevel.Information,
            Message = "Successfully deleted {Count} {ResourceType} resources in cluster {ContextName}"
        )]
        public static partial void DeleteResourcesCompleted(
            ILogger logger,
            int count,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2016,
            Level = LogLevel.Information,
            Message = "Opening log stream for pod {PodName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void OpeningPodLogStream(
            ILogger logger,
            string podName,
            string @namespace,
            string contextName
        );

        [LoggerMessage(
            EventId = 2017,
            Level = LogLevel.Information,
            Message = "Log stream opened for pod {PodName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void PodLogStreamOpened(
            ILogger logger,
            string podName,
            string @namespace,
            string contextName
        );

        [LoggerMessage(
            EventId = 2018,
            Level = LogLevel.Error,
            Message = "Failed to open log stream for pod {PodName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void OpenPodLogStreamFailed(
            ILogger logger,
            string podName,
            string @namespace,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2019,
            Level = LogLevel.Information,
            Message = "Opening exec session for pod {PodName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void OpeningPodExecSession(
            ILogger logger,
            string podName,
            string @namespace,
            string contextName
        );

        [LoggerMessage(
            EventId = 2020,
            Level = LogLevel.Information,
            Message = "Exec session opened for pod {PodName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void PodExecSessionOpened(
            ILogger logger,
            string podName,
            string @namespace,
            string contextName
        );

        [LoggerMessage(
            EventId = 2021,
            Level = LogLevel.Error,
            Message = "Failed to open exec session for pod {PodName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void OpenPodExecSessionFailed(
            ILogger logger,
            string podName,
            string @namespace,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2022,
            Level = LogLevel.Information,
            Message = "Opening port forward for pod {PodName} in namespace {Namespace} on port {Port} in cluster {ContextName}"
        )]
        public static partial void OpeningPodPortForward(
            ILogger logger,
            string podName,
            string @namespace,
            int port,
            string contextName
        );

        [LoggerMessage(
            EventId = 2023,
            Level = LogLevel.Information,
            Message = "Port forward opened for pod {PodName} in namespace {Namespace} on port {Port} in cluster {ContextName}"
        )]
        public static partial void PodPortForwardOpened(
            ILogger logger,
            string podName,
            string @namespace,
            int port,
            string contextName
        );

        [LoggerMessage(
            EventId = 2024,
            Level = LogLevel.Error,
            Message = "Failed to open port forward for pod {PodName} in namespace {Namespace} on port {Port} in cluster {ContextName}"
        )]
        public static partial void OpenPodPortForwardFailed(
            ILogger logger,
            string podName,
            string @namespace,
            int port,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2025,
            Level = LogLevel.Information,
            Message = "Getting YAML for {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void GettingResourceYaml(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2026,
            Level = LogLevel.Information,
            Message = "Retrieved YAML for {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void ResourceYamlRetrieved(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2027,
            Level = LogLevel.Error,
            Message = "Failed to get YAML for {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void GetResourceYamlFailed(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2028,
            Level = LogLevel.Information,
            Message = "Applying YAML for {ResourceType} resource in cluster {ContextName}"
        )]
        public static partial void ApplyingResourceYaml(
            ILogger logger,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2029,
            Level = LogLevel.Information,
            Message = "Applied YAML for {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void ResourceYamlApplied(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2030,
            Level = LogLevel.Error,
            Message = "Failed to apply YAML for {ResourceType} resource in cluster {ContextName}"
        )]
        public static partial void ApplyResourceYamlFailed(
            ILogger logger,
            string resourceType,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2031,
            Level = LogLevel.Information,
            Message = "Creating {ResourceType} resource from YAML in cluster {ContextName}"
        )]
        public static partial void CreatingResourceFromYaml(
            ILogger logger,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2032,
            Level = LogLevel.Information,
            Message = "Created {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}"
        )]
        public static partial void ResourceCreatedFromYaml(
            ILogger logger,
            string resourceName,
            string @namespace,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 2033,
            Level = LogLevel.Error,
            Message = "Failed to create {ResourceType} resource from YAML in cluster {ContextName}"
        )]
        public static partial void CreateResourceFromYamlFailed(
            ILogger logger,
            string resourceType,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2034,
            Level = LogLevel.Warning,
            Message = "Failed to extract resource name from YAML in cluster {ContextName}"
        )]
        public static partial void ExtractResourceNameFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2035,
            Level = LogLevel.Warning,
            Message = "Failed to extract resource namespace from YAML in cluster {ContextName}"
        )]
        public static partial void ExtractResourceNamespaceFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2036,
            Level = LogLevel.Debug,
            Message = "Fetching pod metrics in cluster {ContextName}"
        )]
        public static partial void FetchingPodMetrics(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2037,
            Level = LogLevel.Debug,
            Message = "Fetched metrics for {Count} pods in cluster {ContextName}"
        )]
        public static partial void PodMetricsFetched(ILogger logger, int count, string contextName);

        [LoggerMessage(
            EventId = 2039,
            Level = LogLevel.Warning,
            Message = "Failed to get pod metrics in cluster {ContextName}"
        )]
        public static partial void GetPodMetricsFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 2040,
            Level = LogLevel.Debug,
            Message = "Fetching node metrics in cluster {ContextName}"
        )]
        public static partial void FetchingNodeMetrics(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2041,
            Level = LogLevel.Debug,
            Message = "Fetched metrics for {Count} nodes in cluster {ContextName}"
        )]
        public static partial void NodeMetricsFetched(
            ILogger logger,
            int count,
            string contextName
        );

        [LoggerMessage(
            EventId = 2042,
            Level = LogLevel.Warning,
            Message = "Failed to get node metrics in cluster {ContextName}"
        )]
        public static partial void GetNodeMetricsFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );
    }
}
