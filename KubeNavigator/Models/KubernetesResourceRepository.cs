using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Services;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.Models;

public partial class KubernetesResourceRepository<T> : IKubernetesResourceRepository
    where T : IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger _logger;
    private readonly HashSet<IKubernetesResourceEventSubscriber> _subscribers = [];
    private readonly KubernetesService _kubernetesService;
    private readonly List<T> _resources = [];

    private Watcher<T>? _watcher;

    public ResourceType ResourceType { get; }

    private int _instance;

    public KubernetesResourceRepository(
        ResourceType resourceType,
        KubernetesService kubernetesService,
        ILoggerFactory loggerFactory
    )
    {
        _kubernetesService = kubernetesService;
        ResourceType = resourceType;
        _logger = loggerFactory.CreateLogger($"KubeNavigator.Models.KubernetesResourceRepository<{typeof(T).Name}>");
        _instance = Random.Shared.Next();
    }

    public IReadOnlyCollection<TItem> GetItems<TItem>()
        where TItem : IKubernetesObject<V1ObjectMeta>
    {
        return [.. _resources.Cast<TItem>()];
    }

    public async Task StartAsync()
    {
        Log.InitialListStarting(_logger, ResourceType.Plural);
        var items = await _kubernetesService.ListResourcesAsync<T>(ResourceType);

        foreach (var item in items.Items)
        {
            _resources.Add(item);
        }
        Log.InitialListCompleted(_logger, ResourceType.Plural, items.Items.Count);
    }

    public void AddSubscriber(IKubernetesResourceEventSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
        Log.SubscriberAdded(_logger, ResourceType.Plural, _subscribers.Count);

        if (_subscribers.Any())
        {
            StartWatcher();
        }
    }

    public void RemoveSubscriber(IKubernetesResourceEventSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
        Log.SubscriberRemoved(_logger, ResourceType.Plural, _subscribers.Count);
        if (!_subscribers.Any())
        {
            StopWatcher();
        }
    }

    private void StartWatcher()
    {
        if (_watcher != null)
        {
            return;
        }

        Log.WatcherStarting(_logger, ResourceType.Plural);

        _watcher = _kubernetesService.WatchResources<T>(
            ResourceType,
            (watchEventType, resource) =>
            {
                if (watchEventType == WatchEventType.Added)
                {
                    var index = _resources.FindIndex(r =>
                        r.Metadata.Name == resource.Metadata.Name
                    );
                    if (index != -1)
                    {
                        var currentVersion = int.Parse(_resources[index].Metadata.ResourceVersion);
                        var receivedVersion = int.Parse(resource.Metadata.ResourceVersion);
                        if (receivedVersion > currentVersion)
                        {
                            _resources[index] = resource;

                            foreach (var subscriber in _subscribers)
                            {
                                subscriber.OnResourceEvent(
                                    KubernetesResourceEvent.Modified,
                                    ResourceType,
                                    resource
                                );
                            }
                        }
                    }
                    else
                    {
                        _resources.Add(resource);

                        foreach (var subscriber in _subscribers)
                        {
                            subscriber.OnResourceEvent(
                                KubernetesResourceEvent.Added,
                                ResourceType,
                                resource
                            );
                        }
                    }
                }
                else if (watchEventType == WatchEventType.Modified)
                {
                    var index = _resources.FindIndex(r =>
                        r.Metadata.Name == resource.Metadata.Name
                    );
                    if (index != -1)
                    {
                        var currentVersion = int.Parse(_resources[index].Metadata.ResourceVersion);
                        var receivedVersion = int.Parse(resource.Metadata.ResourceVersion);
                        if (receivedVersion > currentVersion)
                        {
                            _resources[index] = resource;

                            foreach (var subscriber in _subscribers)
                            {
                                subscriber.OnResourceEvent(
                                    KubernetesResourceEvent.Modified,
                                    ResourceType,
                                    resource
                                );
                            }
                        }
                    }
                }
                else if (watchEventType == WatchEventType.Deleted)
                {
                    var existing = _resources.FirstOrDefault(r =>
                        r.Metadata.Name == resource.Metadata.Name
                    );
                    if (existing != null)
                    {
                        _resources.Remove(existing);
                    }

                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OnResourceEvent(
                            KubernetesResourceEvent.Deleted,
                            ResourceType,
                            resource
                        );
                    }
                }
                else
                {
                    Log.UnhandledWatchEventType(_logger, ResourceType.Plural, watchEventType.ToString());
                }
            },
            (ex) =>
            {
                Log.WatcherError(_logger, ResourceType.Plural, ex);
            }
        );
    }

    private void StopWatcher()
    {
        Log.WatcherStopping(_logger, ResourceType.Plural);

        _watcher?.Dispose();
        _watcher = null;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 6001, Level = LogLevel.Debug, Message = "Starting initial list for {ResourceType}")]
        public static partial void InitialListStarting(ILogger logger, string resourceType);

        [LoggerMessage(EventId = 6002, Level = LogLevel.Debug, Message = "Initial list completed for {ResourceType} with {Count} items")]
        public static partial void InitialListCompleted(ILogger logger, string resourceType, int count);

        [LoggerMessage(EventId = 6003, Level = LogLevel.Debug, Message = "Subscriber added for {ResourceType}, total subscribers: {Count}")]
        public static partial void SubscriberAdded(ILogger logger, string resourceType, int count);

        [LoggerMessage(EventId = 6004, Level = LogLevel.Debug, Message = "Subscriber removed for {ResourceType}, total subscribers: {Count}")]
        public static partial void SubscriberRemoved(ILogger logger, string resourceType, int count);

        [LoggerMessage(EventId = 6005, Level = LogLevel.Information, Message = "Starting watcher for {ResourceType}")]
        public static partial void WatcherStarting(ILogger logger, string resourceType);

        [LoggerMessage(EventId = 6006, Level = LogLevel.Information, Message = "Stopping watcher for {ResourceType}")]
        public static partial void WatcherStopping(ILogger logger, string resourceType);

        [LoggerMessage(EventId = 6007, Level = LogLevel.Warning, Message = "Unhandled watch event type {EventType} for {ResourceType}")]
        public static partial void UnhandledWatchEventType(ILogger logger, string resourceType, string eventType);

        [LoggerMessage(EventId = 6008, Level = LogLevel.Error, Message = "Watcher error for {ResourceType}")]
        public static partial void WatcherError(ILogger logger, string resourceType, Exception exception);
    }
}
