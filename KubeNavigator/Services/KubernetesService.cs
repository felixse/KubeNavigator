using k8s;
using k8s.Models;
using KubeNavigator.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KubeNavigator.Services;

public partial class KubernetesService
{
    private readonly ILogger<KubernetesService> _logger;
    private readonly IKubernetes _kubernetes;
    private readonly string _contextName;

    public KubernetesService(string contextName, ILogger<KubernetesService> logger)
    {
        _contextName = contextName;
        _logger = logger;
        
        Log.CreatingKubernetesClient(_logger, contextName);
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(currentContext: contextName);
        _kubernetes = new Kubernetes(config);
        Log.KubernetesClientCreated(_logger, contextName);
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

    public async Task<GenericKubernetesItems<T>> ListResourcesAsync<T>(ResourceType resourceType, CancellationToken cancellationToken = default)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        try
        {
            Log.ListingResources(_logger, resourceType.Plural, _contextName);
            var client = new GenericClient(_kubernetes, resourceType.Group, resourceType.Version, resourceType.Plural);
            var items = await client.ListAsync<GenericKubernetesItems<T>>();
            Log.ResourcesListed(_logger, items.Items.Count, resourceType.Plural, _contextName);
            return items;
        }
        catch (Exception ex)
        {
            Log.ListResourcesFailed(_logger, resourceType.Plural, _contextName, ex);
            throw;
        }
    }

    public Watcher<T> WatchResources<T>(ResourceType resourceType, Action<WatchEventType, T> onEvent, Action<Exception>? onError = null, Action? onClosed = null)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        Log.StartingWatcher(_logger, resourceType.Plural, _contextName);
        var client = new GenericClient(_kubernetes, resourceType.Group, resourceType.Version, resourceType.Plural);
        return client.Watch(onEvent, onError, onClosed);
    }

    public async Task<IEnumerable<(string ResourceName, string Error)>> DeleteResourcesAsync(
        ResourceType resourceType, 
        IEnumerable<IKubernetesObject<V1ObjectMeta>> resources,
        CancellationToken cancellationToken = default)
    {
        var client = new GenericClient(_kubernetes, resourceType.Group, resourceType.Version, resourceType.Plural);
        var errors = new List<(string ResourceName, string Error)>();
        var resourcesList = resources.ToList();

        Log.DeletingResources(_logger, resourcesList.Count, resourceType.Plural, _contextName);

        foreach (var resource in resourcesList)
        {
            try
            {
                Log.DeletingResource(_logger, resource.Name(), resource.Namespace(), resourceType.Plural, _contextName);
                
                if (resourceType.IsNamespaceScoped)
                {
                    await client.DeleteNamespacedAsync<GenericKubernetesObject>(resource.Namespace(), resource.Name());
                }
                else
                {
                    await client.DeleteAsync<GenericKubernetesObject>(resource.Name());
                }
                
                Log.ResourceDeleted(_logger, resource.Name(), resource.Namespace(), resourceType.Plural, _contextName);
            }
            catch (Exception ex)
            {
                Log.DeleteResourceFailed(_logger, resource.Name(), resource.Namespace(), resourceType.Plural, _contextName, ex);
                errors.Add((resource.Name(), ex.Message));
            }
        }

        if (errors.Any())
        {
            Log.DeleteResourcesCompletedWithErrors(_logger, errors.Count, resourcesList.Count, resourceType.Plural, _contextName);
        }
        else
        {
            Log.DeleteResourcesCompleted(_logger, resourcesList.Count, resourceType.Plural, _contextName);
        }

        return errors;
    }

    public async Task<Stream> OpenPodLogStreamAsync(V1Pod pod, CancellationToken cancellationToken = default)
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
                cancellationToken: cancellationToken);
            Log.PodLogStreamOpened(_logger, pod.Name(), pod.Namespace(), _contextName);
            return stream;
        }
        catch (Exception ex)
        {
            Log.OpenPodLogStreamFailed(_logger, pod.Name(), pod.Namespace(), _contextName, ex);
            throw;
        }
    }

    public async Task<PodExecSession> OpenPodExecSessionAsync(V1Pod pod, CancellationToken cancellationToken = default)
    {
            try
        {
            Log.OpeningPodExecSession(_logger, pod.Name(), pod.Namespace(), _contextName);
            var webSocket = await _kubernetes.WebSocketNamespacedPodExecAsync(
                pod.Name(), 
                pod.Namespace(), 
                ["sh", "-c", "clear; (bash || sh || echo 'no shell found')"],
                cancellationToken: cancellationToken);
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
        V1Pod pod, 
        int containerPort, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            Log.OpeningPodPortForward(_logger, pod.Name(), pod.Namespace(), containerPort, _contextName);
            var webSocket = await _kubernetes.WebSocketNamespacedPodPortForwardAsync(
                pod.Name(), 
                pod.Namespace(), 
                [containerPort], 
                cancellationToken: cancellationToken);
            Log.PodPortForwardOpened(_logger, pod.Name(), pod.Namespace(), containerPort, _contextName);
            return webSocket;
        }
        catch (Exception ex)
        {
            Log.OpenPodPortForwardFailed(_logger, pod.Name(), pod.Namespace(), containerPort, _contextName, ex);
            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Creating Kubernetes client for context {ContextName}")]
        public static partial void CreatingKubernetesClient(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Information,
            Message = "Kubernetes client created for context {ContextName}")]
        public static partial void KubernetesClientCreated(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Information,
            Message = "Testing connection to cluster {ContextName}")]
        public static partial void TestingConnection(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Information,
            Message = "Connection to cluster {ContextName} successful")]
        public static partial void ConnectionSuccessful(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Error,
            Message = "Connection to cluster {ContextName} failed")]
        public static partial void ConnectionFailed(ILogger logger, string contextName, Exception exception);

        [LoggerMessage(
            EventId = 2006,
            Level = LogLevel.Information,
            Message = "Listing {ResourceType} resources in cluster {ContextName}")]
        public static partial void ListingResources(ILogger logger, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2007,
            Level = LogLevel.Information,
            Message = "Listed {Count} {ResourceType} resources in cluster {ContextName}")]
        public static partial void ResourcesListed(ILogger logger, int count, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2008,
            Level = LogLevel.Error,
            Message = "Failed to list {ResourceType} resources in cluster {ContextName}")]
        public static partial void ListResourcesFailed(ILogger logger, string resourceType, string contextName, Exception exception);

        [LoggerMessage(
            EventId = 2009,
            Level = LogLevel.Information,
            Message = "Starting watcher for {ResourceType} resources in cluster {ContextName}")]
        public static partial void StartingWatcher(ILogger logger, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2010,
            Level = LogLevel.Information,
            Message = "Deleting {Count} {ResourceType} resources in cluster {ContextName}")]
        public static partial void DeletingResources(ILogger logger, int count, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2011,
            Level = LogLevel.Information,
            Message = "Deleting {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void DeletingResource(ILogger logger, string resourceName, string @namespace, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2012,
            Level = LogLevel.Information,
            Message = "Deleted {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void ResourceDeleted(ILogger logger, string resourceName, string @namespace, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2013,
            Level = LogLevel.Error,
            Message = "Failed to delete {ResourceType} resource {ResourceName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void DeleteResourceFailed(ILogger logger, string resourceName, string @namespace, string resourceType, string contextName, Exception exception);

        [LoggerMessage(
            EventId = 2014,
            Level = LogLevel.Warning,
            Message = "Deleted {SuccessCount} of {TotalCount} {ResourceType} resources in cluster {ContextName} with errors")]
        public static partial void DeleteResourcesCompletedWithErrors(ILogger logger, int successCount, int totalCount, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2015,
            Level = LogLevel.Information,
            Message = "Successfully deleted {Count} {ResourceType} resources in cluster {ContextName}")]
        public static partial void DeleteResourcesCompleted(ILogger logger, int count, string resourceType, string contextName);

        [LoggerMessage(
            EventId = 2016,
            Level = LogLevel.Information,
            Message = "Opening log stream for pod {PodName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void OpeningPodLogStream(ILogger logger, string podName, string @namespace, string contextName);

        [LoggerMessage(
            EventId = 2017,
            Level = LogLevel.Information,
            Message = "Log stream opened for pod {PodName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void PodLogStreamOpened(ILogger logger, string podName, string @namespace, string contextName);

        [LoggerMessage(
            EventId = 2018,
            Level = LogLevel.Error,
            Message = "Failed to open log stream for pod {PodName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void OpenPodLogStreamFailed(ILogger logger, string podName, string @namespace, string contextName, Exception exception);

        [LoggerMessage(
            EventId = 2019,
            Level = LogLevel.Information,
            Message = "Opening exec session for pod {PodName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void OpeningPodExecSession(ILogger logger, string podName, string @namespace, string contextName);

        [LoggerMessage(
            EventId = 2020,
            Level = LogLevel.Information,
            Message = "Exec session opened for pod {PodName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void PodExecSessionOpened(ILogger logger, string podName, string @namespace, string contextName);

        [LoggerMessage(
            EventId = 2021,
            Level = LogLevel.Error,
            Message = "Failed to open exec session for pod {PodName} in namespace {Namespace} in cluster {ContextName}")]
        public static partial void OpenPodExecSessionFailed(ILogger logger, string podName, string @namespace, string contextName, Exception exception);

        [LoggerMessage(
            EventId = 2022,
            Level = LogLevel.Information,
            Message = "Opening port forward for pod {PodName} in namespace {Namespace} on port {Port} in cluster {ContextName}")]
        public static partial void OpeningPodPortForward(ILogger logger, string podName, string @namespace, int port, string contextName);

        [LoggerMessage(
            EventId = 2023,
            Level = LogLevel.Information,
            Message = "Port forward opened for pod {PodName} in namespace {Namespace} on port {Port} in cluster {ContextName}")]
        public static partial void PodPortForwardOpened(ILogger logger, string podName, string @namespace, int port, string contextName);

        [LoggerMessage(
            EventId = 2024,
            Level = LogLevel.Error,
            Message = "Failed to open port forward for pod {PodName} in namespace {Namespace} on port {Port} in cluster {ContextName}")]
        public static partial void OpenPodPortForwardFailed(ILogger logger, string podName, string @namespace, int port, string contextName, Exception exception);
    }
}
