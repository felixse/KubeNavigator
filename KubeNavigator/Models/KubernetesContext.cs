using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

public class KubernetesContext
{
    private ClusterStatus _status = new() { Status = ConnectionStatus.Disconnected };

    private readonly AsyncLock _lock = new();
    private readonly KubernetesService _kubernetesService;
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

    public KubernetesContext(string name, ILoggerFactory loggerFactory)
    {
        Name = name;
        var logger = loggerFactory.CreateLogger<KubernetesService>();
        _kubernetesService = new KubernetesService(name, logger);
    }

    public async Task ConnectAsync()
    {
        try
        {
            if (Status is { Status: ConnectionStatus.Connected or ConnectionStatus.Connecting })
            {
                return;
            }

            Status = new ClusterStatus { Status = ConnectionStatus.Connecting };

            await _kubernetesService.InitializeAsync();
            var connected = await _kubernetesService.TestConnectionAsync();

            if (connected)
            {
                Status = new ClusterStatus { Status = ConnectionStatus.Connected };
            }
            else
            {
                Status = new ClusterStatus
                {
                    Status = ConnectionStatus.Error,
                    ErrorMessage = "Connection test failed",
                };
            }
        }
        catch (Exception e)
        {
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
            repository = (resourceType.Group, resourceType.Version, resourceType.Plural) switch
            {
                (V1Pod.KubeGroup, V1Pod.KubeApiVersion, V1Pod.KubePluralName) =>
                    new KubernetesResourceRepository<V1Pod>(resourceType, _kubernetesService),
                (V1Service.KubeGroup, V1Service.KubeApiVersion, V1Service.KubePluralName) =>
                    new KubernetesResourceRepository<V1Service>(resourceType, _kubernetesService),
                (V1Secret.KubeGroup, V1Secret.KubeApiVersion, V1Secret.KubePluralName) =>
                    new KubernetesResourceRepository<V1Secret>(resourceType, _kubernetesService),
                (V1Namespace.KubeGroup, V1Namespace.KubeApiVersion, V1Namespace.KubePluralName) =>
                    new KubernetesResourceRepository<V1Namespace>(resourceType, _kubernetesService),
                (V1ConfigMap.KubeGroup, V1ConfigMap.KubeApiVersion, V1ConfigMap.KubePluralName) =>
                    new KubernetesResourceRepository<V1ConfigMap>(resourceType, _kubernetesService),
                (
                    Eventsv1Event.KubeGroup,
                    Eventsv1Event.KubeApiVersion,
                    Eventsv1Event.KubePluralName
                ) => new KubernetesResourceRepository<Eventsv1Event>(
                    resourceType,
                    _kubernetesService
                ),
                (
                    V1CustomResourceDefinition.KubeGroup,
                    V1CustomResourceDefinition.KubeApiVersion,
                    V1CustomResourceDefinition.KubePluralName
                ) => new KubernetesResourceRepository<V1CustomResourceDefinition>(
                    resourceType,
                    _kubernetesService
                ),
                _ => new KubernetesResourceRepository<GenericKubernetesObject>(
                    resourceType,
                    _kubernetesService
                ),
            };
            await repository.StartAsync();
            _repositories[resourceType] = repository;
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
        listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await listener.AcceptSocketAsync(cancellationToken);
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

        Debug.WriteLine("Starting socket");
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
                        Debug.WriteLine($"readerTask Exception: {e.Message}");
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
                        Debug.WriteLine($"writerTask Exception: {e.Message}");
                    }
                }
            },
            cancellationToken
        );

        await Task.WhenAll(readerTask, writerTask);

        socket.Close();
    }
}
