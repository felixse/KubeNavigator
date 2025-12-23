using k8s;
using k8s.Models;
using KubeNavigator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.Model;

public class KubernetesResourceRepository<T> : IKubernetesResourceRepository where T : IKubernetesObject<V1ObjectMeta>
{
    private readonly HashSet<IKubernetesResourceEventSubscriber> _subscribers = [];
    private readonly KubernetesService _kubernetesService;
    private readonly List<T> _resources = [];

    private Watcher<T>? _watcher;

    public ResourceType ResourceType { get; }

    private int _instance;

    public KubernetesResourceRepository(ResourceType resourceType, KubernetesService kubernetesService)
    {
        _kubernetesService = kubernetesService;
        ResourceType = resourceType;
        _instance = Random.Shared.Next();
    }

    public IReadOnlyCollection<TItem> GetItems<TItem>() where TItem : IKubernetesObject<V1ObjectMeta>
    {
        return [.. _resources.Cast<TItem>()];
    }

    public async Task StartAsync()
    {
        var items = await _kubernetesService.ListResourcesAsync<T>(ResourceType);

        foreach (var item in items.Items)
        {
            _resources.Add(item);
        }
    }

    public void AddSubscriber(IKubernetesResourceEventSubscriber subscriber)
    {
        _subscribers.Add(subscriber);

        if (_subscribers.Any())
        {
            StartWatcher();
        }
    }

    public void RemoveSubscriber(IKubernetesResourceEventSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
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

        Debug.WriteLine($"Starting watcher for {ResourceType.Plural}");

        _watcher = _kubernetesService.WatchResources<T>(ResourceType, (watchEventType, resource) =>
        {
            if (watchEventType == WatchEventType.Added)
            {
                var index = _resources.FindIndex(r => r.Metadata.Name == resource.Metadata.Name);
                if (index != -1)
                {
                    var currentVersion = int.Parse(_resources[index].Metadata.ResourceVersion);
                    var receivedVersion = int.Parse(resource.Metadata.ResourceVersion);
                    if (receivedVersion > currentVersion)
                    {
                        _resources[index] = resource;

                        foreach (var subscriber in _subscribers)
                        {
                            subscriber.OnResourceEvent(KubernetesResourceEvent.Modified, ResourceType, resource);
                        }
                    }
                }
                else
                {
                    _resources.Add(resource);

                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.OnResourceEvent(KubernetesResourceEvent.Added, ResourceType, resource);
                    }
                }
            }
            else if (watchEventType == WatchEventType.Modified)
            {
                var index = _resources.FindIndex(r => r.Metadata.Name == resource.Metadata.Name);
                if (index != -1)
                {
                    var currentVersion = int.Parse(_resources[index].Metadata.ResourceVersion);
                    var receivedVersion = int.Parse(resource.Metadata.ResourceVersion);
                    if (receivedVersion > currentVersion)
                    {
                        _resources[index] = resource;

                        foreach (var subscriber in _subscribers)
                        {
                            subscriber.OnResourceEvent(KubernetesResourceEvent.Modified, ResourceType, resource);
                        }
                    }
                }
            }
            else if (watchEventType == WatchEventType.Deleted)
            {
                var existing = _resources.FirstOrDefault(r => r.Metadata.Name == resource.Metadata.Name);
                if (existing != null)
                {
                    _resources.Remove(existing);
                }

                foreach (var subscriber in _subscribers)
                {
                    subscriber.OnResourceEvent(KubernetesResourceEvent.Deleted, ResourceType, resource);
                }
            }
            else
            {
                Debug.WriteLine($"Unhandled watch event type: {watchEventType}");
            }
        }, 
        (ex) =>
        {
            Debug.WriteLine(ex);
        });
    }

    private void StopWatcher()
    {
        Debug.WriteLine($"Stopping watcher for {ResourceType.Plural}");

        _watcher?.Dispose();
        _watcher = null;
    }
}
