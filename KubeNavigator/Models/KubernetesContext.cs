using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.Services;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace KubeNavigator.Models;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error,
}

public class ClusterStatus
{
    public ConnectionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public partial class KubernetesContext
{
    private ClusterStatus _status = new() { Status = ConnectionStatus.Disconnected };

    private readonly AsyncLock _lock = new();
    private readonly KubernetesService _kubernetesService;
    private readonly ILogger<KubernetesContext> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<ResourceType, IKubernetesResourceRepository> _repositories = [];

    private CancellationTokenSource? _metricsPollingCts;
    private volatile IReadOnlyDictionary<(string Namespace, string Name), ResourceUsage> _podMetrics =
        new Dictionary<(string, string), ResourceUsage>();
    private Services.NodeMetrics? _nodeMetrics;

    public event EventHandler<ClusterStatus>? StatusChanged; // todo change to something that supports async handlers
    public event EventHandler? PodMetricsUpdated;
    public event EventHandler<Services.NodeMetrics>? NodeMetricsUpdated;
    public event EventHandler? MetricsNotAvailable;

    public string Name { get; }

    public ClusterStatus Status
    {
        get => _status;
        private set
        {
            _status = value;
            StatusChanged?.Invoke(this, value);
        }
    }

    public KubernetesContext(
        string name,
        ILoggerFactory loggerFactory,
        ISettingsService settingsService
    )
    {
        Name = name;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<KubernetesContext>();
        var logger = loggerFactory.CreateLogger<KubernetesService>();
        _kubernetesService = new KubernetesService(name, logger, settingsService);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Status is { Status: ConnectionStatus.Connected or ConnectionStatus.Connecting })
            {
                Log.ConnectSkippedAlreadyConnecting(_logger, Name, Status.Status.ToString());
                return;
            }

            Log.Connecting(_logger, Name);
            Status = new ClusterStatus { Status = ConnectionStatus.Connecting };

            await _kubernetesService.InitializeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var connected = await _kubernetesService.TestConnectionAsync(cancellationToken);

            if (connected)
            {
                Log.Connected(_logger, Name);
                Status = new ClusterStatus { Status = ConnectionStatus.Connected };
                StartMetricsPolling();
            }
            else
            {
                Log.ConnectionTestFailed(_logger, Name);
                Status = new ClusterStatus
                {
                    Status = ConnectionStatus.Error,
                    ErrorMessage = "Connection test failed",
                };
            }
        }
        catch (OperationCanceledException)
        {
            Status = new ClusterStatus { Status = ConnectionStatus.Disconnected };
            throw;
        }
        catch (Exception e)
        {
            Log.ConnectFailed(_logger, Name, e);
            Status = new ClusterStatus
            {
                Status = ConnectionStatus.Error,
                ErrorMessage = e.Message,
            };
            throw; // todo catch and handle
        }
    }

    public void Disconnect()
    {
        Log.Disconnecting(_logger, Name);
        StopMetricsPolling();
        _repositories.Clear();
        Status = new ClusterStatus { Status = ConnectionStatus.Disconnected };
    }

    public async Task<IKubernetesResourceRepository> GetResourceRepositoryAsync(
        ResourceType resourceType
    )
    {
        using var @lock = await _lock.LockAsync();
        if (!_repositories.TryGetValue(resourceType, out IKubernetesResourceRepository? repository))
        {
            Log.CreatingResourceRepository(_logger, resourceType.Plural, Name);
            repository = (resourceType.Group, resourceType.Version, resourceType.Plural) switch
            {
                (V1Pod.KubeGroup, V1Pod.KubeApiVersion, V1Pod.KubePluralName) =>
                    new KubernetesResourceRepository<V1Pod>(
                        resourceType,
                        _kubernetesService,
                        _loggerFactory
                    ),
                (V1Service.KubeGroup, V1Service.KubeApiVersion, V1Service.KubePluralName) =>
                    new KubernetesResourceRepository<V1Service>(
                        resourceType,
                        _kubernetesService,
                        _loggerFactory
                    ),
                (V1Secret.KubeGroup, V1Secret.KubeApiVersion, V1Secret.KubePluralName) =>
                    new KubernetesResourceRepository<V1Secret>(
                        resourceType,
                        _kubernetesService,
                        _loggerFactory
                    ),
                (V1Namespace.KubeGroup, V1Namespace.KubeApiVersion, V1Namespace.KubePluralName) =>
                    new KubernetesResourceRepository<V1Namespace>(
                        resourceType,
                        _kubernetesService,
                        _loggerFactory
                    ),
                (V1Node.KubeGroup, V1Node.KubeApiVersion, V1Node.KubePluralName) =>
                    new KubernetesResourceRepository<V1Node>(
                        resourceType,
                        _kubernetesService,
                        _loggerFactory
                    ),
                (
                    V1Deployment.KubeGroup,
                    V1Deployment.KubeApiVersion,
                    V1Deployment.KubePluralName
                ) => new KubernetesResourceRepository<V1Deployment>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1DaemonSet.KubeGroup,
                    V1DaemonSet.KubeApiVersion,
                    V1DaemonSet.KubePluralName
                ) => new KubernetesResourceRepository<V1DaemonSet>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1ReplicaSet.KubeGroup,
                    V1ReplicaSet.KubeApiVersion,
                    V1ReplicaSet.KubePluralName
                ) => new KubernetesResourceRepository<V1ReplicaSet>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1StorageClass.KubeGroup,
                    V1StorageClass.KubeApiVersion,
                    V1StorageClass.KubePluralName
                ) => new KubernetesResourceRepository<V1StorageClass>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1PersistentVolumeClaim.KubeGroup,
                    V1PersistentVolumeClaim.KubeApiVersion,
                    V1PersistentVolumeClaim.KubePluralName
                ) => new KubernetesResourceRepository<V1PersistentVolumeClaim>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1PersistentVolume.KubeGroup,
                    V1PersistentVolume.KubeApiVersion,
                    V1PersistentVolume.KubePluralName
                ) => new KubernetesResourceRepository<V1PersistentVolume>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1StatefulSet.KubeGroup,
                    V1StatefulSet.KubeApiVersion,
                    V1StatefulSet.KubePluralName
                ) => new KubernetesResourceRepository<V1StatefulSet>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (V1ConfigMap.KubeGroup, V1ConfigMap.KubeApiVersion, V1ConfigMap.KubePluralName) =>
                    new KubernetesResourceRepository<V1ConfigMap>(
                        resourceType,
                        _kubernetesService,
                        _loggerFactory
                    ),
                (
                    V1ServiceAccount.KubeGroup,
                    V1ServiceAccount.KubeApiVersion,
                    V1ServiceAccount.KubePluralName
                ) => new KubernetesResourceRepository<V1ServiceAccount>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1ClusterRole.KubeGroup,
                    V1ClusterRole.KubeApiVersion,
                    V1ClusterRole.KubePluralName
                ) => new KubernetesResourceRepository<V1ClusterRole>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1Role.KubeGroup,
                    V1Role.KubeApiVersion,
                    V1Role.KubePluralName
                ) => new KubernetesResourceRepository<V1Role>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1ClusterRoleBinding.KubeGroup,
                    V1ClusterRoleBinding.KubeApiVersion,
                    V1ClusterRoleBinding.KubePluralName
                ) => new KubernetesResourceRepository<V1ClusterRoleBinding>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1RoleBinding.KubeGroup,
                    V1RoleBinding.KubeApiVersion,
                    V1RoleBinding.KubePluralName
                ) => new KubernetesResourceRepository<V1RoleBinding>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1EndpointSlice.KubeGroup,
                    V1EndpointSlice.KubeApiVersion,
                    V1EndpointSlice.KubePluralName
                ) => new KubernetesResourceRepository<V1EndpointSlice>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1Endpoints.KubeGroup,
                    V1Endpoints.KubeApiVersion,
                    V1Endpoints.KubePluralName
                ) => new KubernetesResourceRepository<V1Endpoints>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1NetworkPolicy.KubeGroup,
                    V1NetworkPolicy.KubeApiVersion,
                    V1NetworkPolicy.KubePluralName
                ) => new KubernetesResourceRepository<V1NetworkPolicy>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1PodDisruptionBudget.KubeGroup,
                    V1PodDisruptionBudget.KubeApiVersion,
                    V1PodDisruptionBudget.KubePluralName
                ) => new KubernetesResourceRepository<V1PodDisruptionBudget>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1PriorityClass.KubeGroup,
                    V1PriorityClass.KubeApiVersion,
                    V1PriorityClass.KubePluralName
                ) => new KubernetesResourceRepository<V1PriorityClass>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1Lease.KubeGroup,
                    V1Lease.KubeApiVersion,
                    V1Lease.KubePluralName
                ) => new KubernetesResourceRepository<V1Lease>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1Ingress.KubeGroup,
                    V1Ingress.KubeApiVersion,
                    V1Ingress.KubePluralName
                ) => new KubernetesResourceRepository<V1Ingress>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    Eventsv1Event.KubeGroup,
                    Eventsv1Event.KubeApiVersion,
                    Eventsv1Event.KubePluralName
                ) => new KubernetesResourceRepository<Eventsv1Event>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1CronJob.KubeGroup,
                    V1CronJob.KubeApiVersion,
                    V1CronJob.KubePluralName
                ) => new KubernetesResourceRepository<V1CronJob>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1RuntimeClass.KubeGroup,
                    V1RuntimeClass.KubeApiVersion,
                    V1RuntimeClass.KubePluralName
                ) => new KubernetesResourceRepository<V1RuntimeClass>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1MutatingWebhookConfiguration.KubeGroup,
                    V1MutatingWebhookConfiguration.KubeApiVersion,
                    V1MutatingWebhookConfiguration.KubePluralName
                ) => new KubernetesResourceRepository<V1MutatingWebhookConfiguration>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1ValidatingWebhookConfiguration.KubeGroup,
                    V1ValidatingWebhookConfiguration.KubeApiVersion,
                    V1ValidatingWebhookConfiguration.KubePluralName
                ) => new KubernetesResourceRepository<V1ValidatingWebhookConfiguration>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                (
                    V1CustomResourceDefinition.KubeGroup,
                    V1CustomResourceDefinition.KubeApiVersion,
                    V1CustomResourceDefinition.KubePluralName
                ) => new KubernetesResourceRepository<V1CustomResourceDefinition>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
                _ => new KubernetesResourceRepository<GenericKubernetesObject>(
                    resourceType,
                    _kubernetesService,
                    _loggerFactory
                ),
            };
            await repository.StartAsync();
            _repositories[resourceType] = repository;
            Log.ResourceRepositoryCreated(_logger, resourceType.Plural, Name);
        }
        return repository;
    }

    public Task<IEnumerable<(string ResourceName, string Error)>> DeleteResourcesAsync(
        ResourceType resourceType,
        IEnumerable<IKubernetesObject<V1ObjectMeta>> resources
    )
    {
        return _kubernetesService.DeleteResourcesAsync(resourceType, resources);
    }

    public Task<Stream> OpenLogStreamAsync(V1Pod pod, CancellationToken cancellationToken)
    {
        return _kubernetesService.OpenPodLogStreamAsync(pod, cancellationToken);
    }

    public Task<string> ReadPodLogsAsync(V1Pod pod, CancellationToken cancellationToken)
    {
        return _kubernetesService.ReadPodLogsAsync(pod, cancellationToken);
    }

    public Task<PodExecSession> ExecAsync(V1Pod pod, CancellationToken cancellationToken)
    {
        return _kubernetesService.OpenPodExecSessionAsync(pod, cancellationToken);
    }

    public async Task<IEnumerable<Eventsv1Event>> GetEventsForResourceAsync(
        IKubernetesObject<V1ObjectMeta> resource
    )
    {
        var eventRepository = await GetResourceRepositoryAsync(ResourceType.Event);

        if (eventRepository == null)
        {
            Log.EventRepositoryUnavailable(_logger, Name);
            return [];
        }

        return eventRepository
            .GetItems<Eventsv1Event>()
            .Where(e =>
                e.Regarding?.Kind == resource.Kind
                && e.Regarding?.Name == resource.Metadata.Name
                && e.Regarding?.NamespaceProperty == resource.Metadata.NamespaceProperty
            );
    }

    public Task<string> GetResourceAsYamlAsync(
        ResourceType resourceType,
        string resourceName,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        return _kubernetesService.GetResourceAsYamlAsync(
            resourceType,
            resourceName,
            resourceNamespace,
            cancellationToken
        );
    }

    public Task PatchResourceFromYamlAsync(
        string originalYaml,
        string modifiedYaml,
        ResourceType resourceType,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        return _kubernetesService.PatchResourceFromYamlAsync(
            originalYaml,
            modifiedYaml,
            resourceType,
            resourceNamespace,
            cancellationToken
        );
    }

    public Task CreateResourceFromYamlAsync(
        string yaml,
        ResourceType resourceType,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        return _kubernetesService.CreateResourceFromYamlAsync(
            yaml,
            resourceType,
            resourceNamespace,
            cancellationToken
        );
    }

    public ResourceUsage? GetPodMetrics(string podNamespace, string podName)
    {
        return _podMetrics.TryGetValue((podNamespace, podName), out var metrics)
            ? metrics
            : null;
    }

    public ResourceUsage? GetNodeMetrics(string nodeName)
    {
        return _nodeMetrics?.NodeUsage.TryGetValue(nodeName, out var metrics) == true
            ? metrics
            : null;
    }

    private void StartMetricsPolling()
    {
        StopMetricsPolling();
        _metricsPollingCts = new CancellationTokenSource();
        _ = PollMetricsAsync(_metricsPollingCts.Token);
    }

    private void StopMetricsPolling()
    {
        _metricsPollingCts?.Cancel();
        _metricsPollingCts?.Dispose();
        _metricsPollingCts = null;
        _podMetrics = new Dictionary<(string, string), ResourceUsage>();
    }

    private async Task PollMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var podMetricsTask = _kubernetesService.GetPodMetricsAsync(cancellationToken);
            var nodeMetricsTask = _kubernetesService.GetNodeMetricsAsync(cancellationToken);
            await Task.WhenAll(podMetricsTask, nodeMetricsTask);
            var podMetrics = podMetricsTask.Result;
            var nodeMetrics = nodeMetricsTask.Result;
            if (podMetrics == null)
            {
                Log.MetricsServerNotAvailable(_logger, Name);
                MetricsNotAvailable?.Invoke(this, EventArgs.Empty);
                return;
            }

            Log.MetricsPollingStarted(_logger, Name);
            _podMetrics = podMetrics;
            PodMetricsUpdated?.Invoke(this, EventArgs.Empty);

            if (nodeMetrics != null)
            {
                _nodeMetrics = nodeMetrics;
                NodeMetricsUpdated?.Invoke(this, nodeMetrics);
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var podMetricsUpdate = _kubernetesService.GetPodMetricsAsync(cancellationToken);
                var nodeMetricsUpdate = _kubernetesService.GetNodeMetricsAsync(cancellationToken);
                await Task.WhenAll(podMetricsUpdate, nodeMetricsUpdate);

                podMetrics = podMetricsUpdate.Result;
                if (podMetrics != null)
                {
                    _podMetrics = podMetrics;
                    PodMetricsUpdated?.Invoke(this, EventArgs.Empty);
                }

                nodeMetrics = nodeMetricsUpdate.Result;
                if (nodeMetrics != null)
                {
                    _nodeMetrics = nodeMetrics;
                    NodeMetricsUpdated?.Invoke(this, nodeMetrics);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
        catch (Exception ex)
        {
            Log.MetricsPollingFailed(_logger, Name, ex);
        }
    }

    public async Task StartListenAsync(
        IKubernetesObject<V1ObjectMeta> resource,
        int targetPort,
        int localPort,
        CancellationToken cancellationToken
    )
    {
        var ipAddress = IPAddress.Loopback;
        var localEndPoint = new IPEndPoint(ipAddress, localPort);
        var listener = new TcpListener(localEndPoint);
        Log.PortForwardListenerStarting(_logger, resource.Name(), localPort, targetPort, Name);
        listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var socket = await listener.AcceptSocketAsync(cancellationToken);
                Log.PortForwardSocketAccepted(_logger, resource.Name(), localPort, Name);
                Task.Run(
                    async () => await RunSocketAsync(socket, resource, targetPort, cancellationToken),
                    cancellationToken
                );
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    public async Task SaveConfigMapAsync(V1ConfigMap configMap)
    {
        await _kubernetesService.SaveConfigMapAsync(configMap);
    }

    public async Task SaveSecretAsync(V1Secret secret)
    {
        await _kubernetesService.SaveSecretAsync(secret);
    }

    private async Task RunSocketAsync(
        Socket socket,
        IKubernetesObject<V1ObjectMeta> resource,
        int targetPort,
        CancellationToken cancellationToken
    )
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var webSocket = await _kubernetesService.OpenPodPortForwardAsync(
            resource,
            targetPort,
            cancellationToken
        );
        var demux = new StreamDemuxer(webSocket, StreamType.PortForward);
        demux.Start();

        using var stream = demux.GetStream((byte?)0, (byte?)0);

        Log.PortForwardSocketRunning(_logger, resource.Name(), targetPort, Name);
        var readerTask = Task.Run(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested && socket.Connected)
                {
                    var buffer = arrayPool.Rent(4096);
                    try
                    {
                        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, bytesRead),
                            SocketFlags.None
                        );
                    }
                    catch (Exception e)
                    {
                        Log.PortForwardReadError(_logger, resource.Name(), targetPort, Name, e);
                    }
                    finally
                    {
                        arrayPool.Return(buffer);
                    }
                }
            },
            cancellationToken
        );

        var writerTask = Task.Run(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested && socket.Connected)
                {
                    var buffer = arrayPool.Rent(4096);
                    try
                    {
                        var bytesRead = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None
                        );
                        stream.Write(buffer, 0, bytesRead);
                    }
                    catch (Exception e)
                    {
                        Log.PortForwardWriteError(_logger, resource.Name(), targetPort, Name, e);
                    }
                    finally
                    {
                        arrayPool.Return(buffer);
                    }
                }
            },
            cancellationToken
        );

        await Task.WhenAll(readerTask, writerTask);

        Log.PortForwardSocketClosed(_logger, resource.Name(), targetPort, Name);
        socket.Close();
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 5001,
            Level = LogLevel.Information,
            Message = "Connecting to cluster {ContextName}"
        )]
        public static partial void Connecting(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5002,
            Level = LogLevel.Information,
            Message = "Connected to cluster {ContextName}"
        )]
        public static partial void Connected(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5015,
            Level = LogLevel.Information,
            Message = "Disconnecting from cluster {ContextName}"
        )]
        public static partial void Disconnecting(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5003,
            Level = LogLevel.Warning,
            Message = "Connection test failed for cluster {ContextName}"
        )]
        public static partial void ConnectionTestFailed(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5004,
            Level = LogLevel.Error,
            Message = "Failed to connect to cluster {ContextName}"
        )]
        public static partial void ConnectFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 5005,
            Level = LogLevel.Debug,
            Message = "Connect skipped for cluster {ContextName}, already in state {CurrentStatus}"
        )]
        public static partial void ConnectSkippedAlreadyConnecting(
            ILogger logger,
            string contextName,
            string currentStatus
        );

        [LoggerMessage(
            EventId = 5006,
            Level = LogLevel.Debug,
            Message = "Creating resource repository for {ResourceType} in cluster {ContextName}"
        )]
        public static partial void CreatingResourceRepository(
            ILogger logger,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 5007,
            Level = LogLevel.Debug,
            Message = "Resource repository created for {ResourceType} in cluster {ContextName}"
        )]
        public static partial void ResourceRepositoryCreated(
            ILogger logger,
            string resourceType,
            string contextName
        );

        [LoggerMessage(
            EventId = 5008,
            Level = LogLevel.Warning,
            Message = "Event repository unavailable for cluster {ContextName}"
        )]
        public static partial void EventRepositoryUnavailable(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5009,
            Level = LogLevel.Information,
            Message = "Port forward listener starting for {ResourceName} on local port {LocalPort} to target port {TargetPort} in cluster {ContextName}"
        )]
        public static partial void PortForwardListenerStarting(
            ILogger logger,
            string resourceName,
            int localPort,
            int targetPort,
            string contextName
        );

        [LoggerMessage(
            EventId = 5010,
            Level = LogLevel.Debug,
            Message = "Port forward socket accepted for {ResourceName} on local port {LocalPort} in cluster {ContextName}"
        )]
        public static partial void PortForwardSocketAccepted(
            ILogger logger,
            string resourceName,
            int localPort,
            string contextName
        );

        [LoggerMessage(
            EventId = 5011,
            Level = LogLevel.Debug,
            Message = "Port forward socket running for {ResourceName} on target port {TargetPort} in cluster {ContextName}"
        )]
        public static partial void PortForwardSocketRunning(
            ILogger logger,
            string resourceName,
            int targetPort,
            string contextName
        );

        [LoggerMessage(
            EventId = 5012,
            Level = LogLevel.Error,
            Message = "Port forward read error for {ResourceName} on target port {TargetPort} in cluster {ContextName}"
        )]
        public static partial void PortForwardReadError(
            ILogger logger,
            string resourceName,
            int targetPort,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 5013,
            Level = LogLevel.Error,
            Message = "Port forward write error for {ResourceName} on target port {TargetPort} in cluster {ContextName}"
        )]
        public static partial void PortForwardWriteError(
            ILogger logger,
            string resourceName,
            int targetPort,
            string contextName,
            Exception exception
        );

        [LoggerMessage(
            EventId = 5014,
            Level = LogLevel.Information,
            Message = "Port forward socket closed for {ResourceName} on target port {TargetPort} in cluster {ContextName}"
        )]
        public static partial void PortForwardSocketClosed(
            ILogger logger,
            string resourceName,
            int targetPort,
            string contextName
        );

        [LoggerMessage(
            EventId = 5016,
            Level = LogLevel.Information,
            Message = "Metrics server not available in cluster {ContextName}, metrics polling disabled"
        )]
        public static partial void MetricsServerNotAvailable(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5017,
            Level = LogLevel.Information,
            Message = "Metrics polling started for cluster {ContextName}"
        )]
        public static partial void MetricsPollingStarted(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 5018,
            Level = LogLevel.Error,
            Message = "Metrics polling failed for cluster {ContextName}"
        )]
        public static partial void MetricsPollingFailed(
            ILogger logger,
            string contextName,
            Exception exception
        );
    }
}
