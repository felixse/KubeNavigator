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

    public event EventHandler<ClusterStatus>? StatusChanged; // todo change to something that supports async handlers

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

    public KubernetesContext(string name, ILoggerFactory loggerFactory, ISettingsService settingsService)
    {
        Name = name;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<KubernetesContext>();
        var logger = loggerFactory.CreateLogger<KubernetesService>();
        _kubernetesService = new KubernetesService(name, logger, settingsService);
    }

    public async Task ConnectAsync()
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
            var connected = await _kubernetesService.TestConnectionAsync();

            if (connected)
            {
                Log.Connected(_logger, Name);
                Status = new ClusterStatus { Status = ConnectionStatus.Connected };
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
                    new KubernetesResourceRepository<V1Pod>(resourceType, _kubernetesService, _loggerFactory),
                (V1Service.KubeGroup, V1Service.KubeApiVersion, V1Service.KubePluralName) =>
                    new KubernetesResourceRepository<V1Service>(resourceType, _kubernetesService, _loggerFactory),
                (V1Secret.KubeGroup, V1Secret.KubeApiVersion, V1Secret.KubePluralName) =>
                    new KubernetesResourceRepository<V1Secret>(resourceType, _kubernetesService, _loggerFactory),
                (V1Namespace.KubeGroup, V1Namespace.KubeApiVersion, V1Namespace.KubePluralName) =>
                    new KubernetesResourceRepository<V1Namespace>(resourceType, _kubernetesService, _loggerFactory),
                (V1ConfigMap.KubeGroup, V1ConfigMap.KubeApiVersion, V1ConfigMap.KubePluralName) =>
                    new KubernetesResourceRepository<V1ConfigMap>(resourceType, _kubernetesService, _loggerFactory),
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

    public Task ApplyResourceFromYamlAsync(
        string yaml,
        ResourceType resourceType,
        string? resourceNamespace = null,
        CancellationToken cancellationToken = default
    )
    {
        return _kubernetesService.ApplyResourceFromYamlAsync(
            yaml,
            resourceType,
            resourceNamespace,
            cancellationToken
        );
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
                    try
                    {
                        var buffer = arrayPool.Rent(4096);
                        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, bytesRead),
                            SocketFlags.None
                        );
                        arrayPool.Return(buffer);
                    }
                    catch (Exception e)
                    {
                        Log.PortForwardReadError(_logger, resource.Name(), targetPort, Name, e);
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
                    try
                    {
                        var buffer = arrayPool.Rent(4096);
                        var bytesRead = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            SocketFlags.None
                        );
                        stream.Write(buffer, 0, bytesRead);
                        arrayPool.Return(buffer);
                    }
                    catch (Exception e)
                    {
                        Log.PortForwardWriteError(_logger, resource.Name(), targetPort, Name, e);
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
        [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Connecting to cluster {ContextName}")]
        public static partial void Connecting(ILogger logger, string contextName);

        [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Connected to cluster {ContextName}")]
        public static partial void Connected(ILogger logger, string contextName);

        [LoggerMessage(EventId = 5003, Level = LogLevel.Warning, Message = "Connection test failed for cluster {ContextName}")]
        public static partial void ConnectionTestFailed(ILogger logger, string contextName);

        [LoggerMessage(EventId = 5004, Level = LogLevel.Error, Message = "Failed to connect to cluster {ContextName}")]
        public static partial void ConnectFailed(ILogger logger, string contextName, Exception exception);

        [LoggerMessage(EventId = 5005, Level = LogLevel.Debug, Message = "Connect skipped for cluster {ContextName}, already in state {CurrentStatus}")]
        public static partial void ConnectSkippedAlreadyConnecting(ILogger logger, string contextName, string currentStatus);

        [LoggerMessage(EventId = 5006, Level = LogLevel.Debug, Message = "Creating resource repository for {ResourceType} in cluster {ContextName}")]
        public static partial void CreatingResourceRepository(ILogger logger, string resourceType, string contextName);

        [LoggerMessage(EventId = 5007, Level = LogLevel.Debug, Message = "Resource repository created for {ResourceType} in cluster {ContextName}")]
        public static partial void ResourceRepositoryCreated(ILogger logger, string resourceType, string contextName);

        [LoggerMessage(EventId = 5008, Level = LogLevel.Warning, Message = "Event repository unavailable for cluster {ContextName}")]
        public static partial void EventRepositoryUnavailable(ILogger logger, string contextName);

        [LoggerMessage(EventId = 5009, Level = LogLevel.Information, Message = "Port forward listener starting for {ResourceName} on local port {LocalPort} to target port {TargetPort} in cluster {ContextName}")]
        public static partial void PortForwardListenerStarting(ILogger logger, string resourceName, int localPort, int targetPort, string contextName);

        [LoggerMessage(EventId = 5010, Level = LogLevel.Debug, Message = "Port forward socket accepted for {ResourceName} on local port {LocalPort} in cluster {ContextName}")]
        public static partial void PortForwardSocketAccepted(ILogger logger, string resourceName, int localPort, string contextName);

        [LoggerMessage(EventId = 5011, Level = LogLevel.Debug, Message = "Port forward socket running for {ResourceName} on target port {TargetPort} in cluster {ContextName}")]
        public static partial void PortForwardSocketRunning(ILogger logger, string resourceName, int targetPort, string contextName);

        [LoggerMessage(EventId = 5012, Level = LogLevel.Error, Message = "Port forward read error for {ResourceName} on target port {TargetPort} in cluster {ContextName}")]
        public static partial void PortForwardReadError(ILogger logger, string resourceName, int targetPort, string contextName, Exception exception);

        [LoggerMessage(EventId = 5013, Level = LogLevel.Error, Message = "Port forward write error for {ResourceName} on target port {TargetPort} in cluster {ContextName}")]
        public static partial void PortForwardWriteError(ILogger logger, string resourceName, int targetPort, string contextName, Exception exception);

        [LoggerMessage(EventId = 5014, Level = LogLevel.Information, Message = "Port forward socket closed for {ResourceName} on target port {TargetPort} in cluster {ContextName}")]
        public static partial void PortForwardSocketClosed(ILogger logger, string resourceName, int targetPort, string contextName);
    }
}
