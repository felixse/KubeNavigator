using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Filters;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Resources;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels;

public partial class ClusterViewModel : ObservableObject, IKubernetesResourceEventSubscriber
{
    private readonly Dictionary<
        ResourceType,
        ObservableCollection<KubernetesResourceViewModel>
    > _resources = [];

    private readonly Dictionary<
        ResourceType,
        Dictionary<(string Name, string? Namespace), KubernetesResourceViewModel>
    > _resourceIndex = [];

    private readonly ILogger<ClusterViewModel> _logger;

    [ObservableProperty]
    public partial ClusterStatus Status { get; private set; } =
        new ClusterStatus { Status = ConnectionStatus.Disconnected };

    public ObservableCollection<INamespaceFilter> NamespaceFilters { get; } = [];

    public ObservableCollection<HelmReleaseViewModel> HelmReleases { get; } = [];

    private readonly Dictionary<(string Name, string Namespace), HelmReleaseViewModel> _helmIndex = [];

    public Dictionary<ResourceType, IEnumerable<ToggleFilter>> AdditionalFilters { get; } = new();

    public string Name { get; }
    public AppViewModel App { get; }

    public KubernetesContext Context { get; }

    public ClusterViewModel(string name, AppViewModel app, KubernetesContext context)
    {
        Name = name;
        App = app;
        Context = context;
        _logger = app.LoggingService.LoggerFactory.CreateLogger<ClusterViewModel>();
        NamespaceFilters.Add(new AllNamespacesFilter());
        AdditionalFilters.Add(
            ResourceType.Secret,
            [
                new ToggleFilter(
                    "Hide Helm Secrets",
                    defaultValue: true,
                    r =>
                    {
                        if (r is SecretViewModel secret)
                        {
                            return secret.Type != "helm.sh/release.v1";
                        }
                        return true;
                    }
                ),
            ]
        );
        AdditionalFilters.Add(
            ResourceType.Event,
            [
                new ToggleFilter(
                    "Warnings only",
                    defaultValue: true,
                    r =>
                    {
                        if (r is EventViewModel @event)
                        {
                            return @event.Event.Type == "Warning";
                        }
                        return true;
                    }
                ),
            ]
        );

        context.StatusChanged += Context_StatusChanged;
        context.PodMetricsUpdated += OnPodMetricsUpdated;
    }

    private async void Context_StatusChanged(object? sender, ClusterStatus e)
    {
        Status = e;

        if (e.Status == ConnectionStatus.Connected)
        {
            var namespaces = await Context.GetResourceRepositoryAsync(ResourceType.Namespace);
            namespaces.AddSubscriber(this);

            foreach (var ns in namespaces.GetItems<V1Namespace>())
            {
                NamespaceFilters.Add(new NamespaceFilter { Name = ns.Name() });
            }

            var secrets = await Context.GetResourceRepositoryAsync(ResourceType.Secret);
            secrets.AddSubscriber(this);

            foreach (
                var secret in secrets
                    .GetItems<V1Secret>()
                    .Where(s => s.Type == "helm.sh/release.v1")
            )
            {
                var helmRelease = App.HelmService.ParseReleaseFromSecret(secret);
                if (helmRelease == null)
                    continue;

                var helmKey = (helmRelease.Name, helmRelease.Namespace);
                if (_helmIndex.TryGetValue(helmKey, out var existing))
                {
                    existing.Revisions.Add(helmRelease);
                }
                else
                {
                    var vm = new HelmReleaseViewModel(helmRelease, this);
                    HelmReleases.Add(vm);
                    _helmIndex[helmKey] = vm;
                }
            }
        }
    }

    public ForwardedPortViewModel CreateForwardedPort(
        KubernetesResourceViewModel resource,
        IKubernetesObject<V1ObjectMeta> targetResource,
        int targetPort,
        int localPort,
        string? protocol
    )
    {
        var forwardedPort = new ForwardedPortViewModel(
            this,
            resource,
            targetResource,
            localPort,
            targetPort,
            protocol
        );
        App.ForwardedPorts.Add(forwardedPort);

        return forwardedPort;
    }

    public void DeleteForwardedPort(
        ForwardedPortViewModel forwardedPort,
        KubernetesResourceViewModel resource
    )
    {
        forwardedPort.Stop();
        App.ForwardedPorts.Remove(forwardedPort);
        resource.RequestDetailsRefresh();
    }

    public CancellationTokenSource ForwardContainerPort(
        IKubernetesObject<V1ObjectMeta> resource,
        int targetPort,
        int localPort
    )
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Task.Run(
            async () =>
                await Context.StartListenAsync(
                    resource,
                    targetPort,
                    localPort,
                    cancellationTokenSource.Token
                ),
            cancellationTokenSource.Token
        );

        return cancellationTokenSource;
    }

    public async Task<ObservableCollection<KubernetesResourceViewModel>> GetResourcesAsync(
        ResourceType resourceType
    )
    {
        if (
            !_resources.TryGetValue(
                resourceType,
                out ObservableCollection<KubernetesResourceViewModel>? value
            )
        )
        {
            var repository = await Context.GetResourceRepositoryAsync(resourceType);
            var collection = new ObservableCollection<KubernetesResourceViewModel>(
                CreateResourceViewModelCollection(repository, resourceType)
            );
            value = collection;
            _resources.Add(resourceType, value);

            var index = new Dictionary<(string Name, string? Namespace), KubernetesResourceViewModel>();
            foreach (var vm in value)
            {
                index[(vm.Name, vm.Resource.Namespace())] = vm;
            }
            _resourceIndex[resourceType] = index;
        }
        return value;
    }

    public async Task WatchResource(ResourceType resourceType)
    {
        if (resourceType == ResourceType.Namespace || resourceType == ResourceType.Secret)
        {
            return;
        }

        var repository = await Context.GetResourceRepositoryAsync(resourceType);
        repository.AddSubscriber(this);
    }

    public async Task StopWatchResource(ResourceType resourceType)
    {
        if (resourceType == ResourceType.Namespace || resourceType == ResourceType.Secret)
        {
            return;
        }

        var repository = await Context.GetResourceRepositoryAsync(resourceType);
        repository.RemoveSubscriber(this);
    }

    private IEnumerable<KubernetesResourceViewModel> CreateResourceViewModelCollection(
        IKubernetesResourceRepository resourceRepository,
        ResourceType resourceType
    )
    {
        return resourceRepository
            .GetItems<IKubernetesObject<V1ObjectMeta>>()
            .Select(r => CreateResourceViewModel(r, resourceType));
    }

    private KubernetesResourceViewModel CreateResourceViewModel(
        IKubernetesObject<V1ObjectMeta> resource,
        ResourceType resourceType
    )
    {
        return (resourceType.Group, resourceType.Version, resourceType.Plural) switch
        {
            (V1Pod.KubeGroup, V1Pod.KubeApiVersion, V1Pod.KubePluralName) => new PodViewModel(
                (V1Pod)resource,
                this
            ),
            (V1Service.KubeGroup, V1Service.KubeApiVersion, V1Service.KubePluralName) =>
                new ServiceViewModel((V1Service)resource, this),
            (V1Deployment.KubeGroup, V1Deployment.KubeApiVersion, V1Deployment.KubePluralName) =>
                new DeploymentViewModel((V1Deployment)resource, this),
            (V1ConfigMap.KubeGroup, V1ConfigMap.KubeApiVersion, V1ConfigMap.KubePluralName) =>
                new ConfigMapViewModel((V1ConfigMap)resource, this),
            (V1Secret.KubeGroup, V1Secret.KubeApiVersion, V1Secret.KubePluralName) =>
                new SecretViewModel((V1Secret)resource, this),
            (Eventsv1Event.KubeGroup, Eventsv1Event.KubeApiVersion, Eventsv1Event.KubePluralName) =>
                new EventViewModel((Eventsv1Event)resource, this),
            (V1Namespace.KubeGroup, V1Namespace.KubeApiVersion, V1Namespace.KubePluralName) =>
                new NamespaceViewModel((V1Namespace)resource, this),
            _ => new KubernetesResourceViewModel(resource, resourceType, this),
        };
    }

    public async Task<KubernetesResourceViewModel?> GetResourceAsync(
        ResourceType resourceType,
        string name
    )
    {
        await GetResourcesAsync(resourceType);

        if (_resourceIndex.TryGetValue(resourceType, out var index))
        {
            foreach (var kvp in index)
            {
                if (kvp.Key.Name == name)
                    return kvp.Value;
            }
        }

        return null;
    }

    public async Task DeleteResourcesAsync(
        ResourceType resourceType,
        IReadOnlyCollection<KubernetesResourceViewModel> resources
    )
    {
        var confirmed =
            await App.WindowManager.ActiveWindow.ContentDialogService.ConfirmResourceDeletionAsync(
                resourceType,
                resources.Select(r => r.Name),
                Name
            );
        if (!confirmed)
        {
            return;
        }

        await Context.DeleteResourcesAsync(resourceType, resources.Select(r => r.Resource));
    }

    public async Task OnResourceEvent(
        KubernetesResourceEvent resourceEvent,
        ResourceType resourceType,
        IKubernetesObject<V1ObjectMeta> resource
    )
    {
        var collection = await GetResourcesAsync(resourceType);

        if (!_resourceIndex.TryGetValue(resourceType, out var index))
        {
            index = [];
            _resourceIndex[resourceType] = index;
        }

        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            var key = (resource.Name(), resource.Namespace());

            if (resourceEvent == KubernetesResourceEvent.Added)
            {
                var vm = CreateResourceViewModel(resource, resourceType);
                collection.Add(vm);
                index[key] = vm;
            }
            else if (resourceEvent == KubernetesResourceEvent.Modified)
            {
                if (index.TryGetValue(key, out var existing))
                {
                    existing.Resource = resource;
                }
            }
            else if (resourceEvent == KubernetesResourceEvent.Deleted)
            {
                if (index.TryGetValue(key, out var existing))
                {
                    collection.Remove(existing);
                    index.Remove(key);
                }
            }

            if (resourceType == ResourceType.Namespace && resource is V1Namespace @namespace)
            {
                if (resourceEvent == KubernetesResourceEvent.Added)
                {
                    NamespaceFilters.Add(new NamespaceFilter { Name = @namespace.Metadata.Name });
                }
                else if (resourceEvent == KubernetesResourceEvent.Deleted)
                {
                    var existing = NamespaceFilters.FirstOrDefault(n =>
                        n is NamespaceFilter nf && nf.Name == @namespace.Metadata.Name
                    );
                    if (existing != null)
                    {
                        NamespaceFilters.Remove(existing);
                    }
                }
                else
                {
                    Log.UnhandledNamespaceEvent(_logger, Name, resourceEvent.ToString());
                }
            }
            else if (
                resourceType == ResourceType.Secret
                && resource is V1Secret secret
                && secret.Type == "helm.sh/release.v1"
            )
            {
                if (resourceEvent == KubernetesResourceEvent.Added)
                {
                    var helmRelease = App.HelmService.ParseReleaseFromSecret(secret);
                    if (helmRelease == null)
                        return;

                    var helmKey = (helmRelease.Name, helmRelease.Namespace);
                    if (_helmIndex.TryGetValue(helmKey, out var existing))
                    {
                        existing.Revisions.Add(helmRelease);
                    }
                    else
                    {
                        var vm = new HelmReleaseViewModel(helmRelease, this);
                        HelmReleases.Add(vm);
                        _helmIndex[helmKey] = vm;
                    }
                }
                else if (resourceEvent == KubernetesResourceEvent.Deleted)
                {
                    var helmRelease = App.HelmService.ParseReleaseFromSecret(secret);
                    if (helmRelease == null)
                        return;

                    var helmKey = (helmRelease.Name, helmRelease.Namespace);
                    if (_helmIndex.TryGetValue(helmKey, out var existingHelmRelease))
                    {
                        var existingRevision = existingHelmRelease.Revisions.FirstOrDefault(r =>
                            r.Version == helmRelease.Version
                        );
                        if (existingRevision != null)
                        {
                            if (existingHelmRelease.Revisions.Count == 1)
                            {
                                HelmReleases.Remove(existingHelmRelease);
                                _helmIndex.Remove(helmKey);
                            }
                            else
                            {
                                existingHelmRelease.Revisions.Remove(existingRevision);
                            }
                        }
                    }
                }
                else if (resourceEvent == KubernetesResourceEvent.Modified)
                {
                    var helmRelease = App.HelmService.ParseReleaseFromSecret(secret);
                    if (helmRelease == null)
                        return;

                    var helmKey = (helmRelease.Name, helmRelease.Namespace);
                    if (_helmIndex.TryGetValue(helmKey, out var existing))
                    {
                        var existingRevision = existing.Revisions.FirstOrDefault(r =>
                            r.Version == helmRelease.Version
                        );
                        if (existingRevision != null)
                        {
                            var revIndex = existing.Revisions.IndexOf(existingRevision);
                            existing.Revisions[revIndex] = helmRelease;
                        }
                        else
                        {
                            existing.Revisions.Add(helmRelease);
                        }
                    }
                    else
                    {
                        var vm = new HelmReleaseViewModel(helmRelease, this);
                        HelmReleases.Add(vm);
                        _helmIndex[helmKey] = vm;
                    }
                }
                else
                {
                    Log.UnhandledSecretEvent(_logger, Name, resourceEvent.ToString());
                }
            }
        });
    }

    private void OnPodMetricsUpdated(object? sender, EventArgs e)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            if (_resources.TryGetValue(ResourceType.Pod, out var pods))
            {
                foreach (var pod in pods)
                {
                    if (pod is PodViewModel podVm)
                    {
                        podVm.RefreshMetrics();
                    }
                }
            }
        });
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 10001,
            Level = LogLevel.Warning,
            Message = "Unhandled namespace event {EventType} for cluster {ClusterName}"
        )]
        public static partial void UnhandledNamespaceEvent(
            ILogger logger,
            string clusterName,
            string eventType
        );

        [LoggerMessage(
            EventId = 10002,
            Level = LogLevel.Warning,
            Message = "Unhandled secret event {EventType} for cluster {ClusterName}"
        )]
        public static partial void UnhandledSecretEvent(
            ILogger logger,
            string clusterName,
            string eventType
        );
    }
}
