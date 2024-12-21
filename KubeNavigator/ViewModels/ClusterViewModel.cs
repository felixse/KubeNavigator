using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using k8s;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.Model.Helm;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class ClusterViewModel : ObservableObject, IKubernetesResourceEventSubscriber
{
    private readonly Dictionary<ResourceType, ObservableCollection<KubernetesResourceViewModel>> _resources = [];

    [ObservableProperty]
    public partial ClusterStatus Status { get; private set; } = new ClusterStatus { Status = ConnectionStatus.Disconnected };

    public ObservableCollection<INamespaceFilter> NamespaceFilters { get; } = [];

    public ObservableCollection<HelmReleaseViewModel> HelmReleases { get; } = [];

    public string Name { get; }
    public AppViewModel App { get; }

    public KubernetesContext Context { get; }

    public ClusterViewModel(string name, AppViewModel app, KubernetesContext context)
    {
        Name = name;
        App = app;
        Context = context;
        NamespaceFilters.Add(new AllNamespacesFilter());

        context.StatusChanged += Context_StatusChanged;
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

            foreach (var secret in secrets.GetItems<V1Secret>().Where(s => s.Type == "helm.sh/release.v1"))
            {
                var helmRelease = HelmRelease.FromSecret(secret);
                var existing = HelmReleases.FirstOrDefault(h => h.HelmRelease.Name == helmRelease.Name && h.HelmRelease.Namespace == helmRelease.Namespace);
                if (existing == null)
                {
                    HelmReleases.Add(new HelmReleaseViewModel(helmRelease));
                }
                else
                {
                    existing.Revisions.Add(helmRelease);
                }
            }
        }
    }

    public ForwardedPortViewModel CreateForwardedPort(PodViewModel pod, V1ContainerPort port, int localPort)
    {
        var forwardedPort = new ForwardedPortViewModel(this, pod, port, localPort);
        App.ForwardedPorts.Add(forwardedPort);

        return forwardedPort;
    }

    public void DeleteForwardedPort(ForwardedPortViewModel forwardedPort, PodViewModel pod)
    {
        forwardedPort.Stop();
        App.ForwardedPorts.Remove(forwardedPort);

        // update details to remove the forwarded port
        pod.UpdateDetails();
    }

    public CancellationTokenSource ForwardContainerPort(V1Pod pod, V1ContainerPort port, int localPort)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () => await Context.StartListenAsync(pod, port, localPort, cancellationTokenSource.Token), cancellationTokenSource.Token);

        return cancellationTokenSource;
    }

    public async Task<ObservableCollection<KubernetesResourceViewModel>> GetResourcesAsync(ResourceType resourceType)
    {
        if (!_resources.TryGetValue(resourceType, out ObservableCollection<KubernetesResourceViewModel>? value))
        {
            var repository = await Context.GetResourceRepositoryAsync(resourceType);
            var collection = new ObservableCollection<KubernetesResourceViewModel>(CreateResourceViewModelCollection(repository, resourceType));
            value = collection;
            _resources.Add(resourceType, value);
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

    private IEnumerable<KubernetesResourceViewModel> CreateResourceViewModelCollection(IKubernetesResourceRepository resourceRepository, ResourceType resourceType)
    {
        return resourceRepository.GetItems<IKubernetesObject<V1ObjectMeta>>().Select(r => CreateResourceViewModel(r, resourceType));
    }

    private KubernetesResourceViewModel CreateResourceViewModel(IKubernetesObject<V1ObjectMeta> resource, ResourceType resourceType)
    {
        return (resourceType.Group, resourceType.Version, resourceType.Plural) switch
        {
            (V1Pod.KubeGroup, V1Pod.KubeApiVersion, V1Pod.KubePluralName) => new PodViewModel((V1Pod)resource, this),
            _ => new KubernetesResourceViewModel(resource, resourceType, this),
        };
    }

    public async Task<KubernetesResourceViewModel?> GetResourceAsync(ResourceType resourceType, string name)
    {
        var resources = await GetResourcesAsync(resourceType);

        return resources.FirstOrDefault(r => r.Name == name);
    }

    public async Task DeleteResourcesAsync(ResourceType resourceType, IReadOnlyCollection<KubernetesResourceViewModel> resources)
    {
        var confirmed = await App.WindowManager.ActiveWindow.UserConfirmationService.ConfirmResourceDeletionAsync(resourceType, resources.Select(r => r.Name), Name);
        if (!confirmed)
        {
            return;
        }

        await Context.DeleteResourcesAsync(resourceType, resources.Select(r => r.Resource));
    }

    public async Task OnResourceEvent(KubernetesResourceEvent resourceEvent, ResourceType resourceType, IKubernetesObject<V1ObjectMeta> resource)
    {
        var collection = await GetResourcesAsync(resourceType);

        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            if (resourceEvent == KubernetesResourceEvent.Added)
            {
                collection.Add(CreateResourceViewModel(resource, resourceType));
            }
            else if (resourceEvent == KubernetesResourceEvent.Modified)
            {
                var existing = collection.FirstOrDefault(r => r.Name == resource.Name());
                if (existing != null)
                {
                    existing.Resource = resource;
                }
            }
            else if (resourceEvent == KubernetesResourceEvent.Deleted)
            {
                var existing = collection.FirstOrDefault(r => r.Name == resource.Name());
                if (existing != null)
                {
                    collection.Remove(existing);
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
                    var existing = NamespaceFilters.FirstOrDefault(n => n is NamespaceFilter nf && nf.Name == @namespace.Metadata.Name);
                    if (existing != null)
                    {
                        NamespaceFilters.Remove(existing);
                    }
                }
                else
                {
                    Debug.WriteLine($"Unhandled namespace event: {resourceEvent}");
                }
            }
            else if (resourceType == ResourceType.Secret && resource is V1Secret secret && secret.Type == "helm.sh/release.v1")
            {
                if (resourceEvent == KubernetesResourceEvent.Added)
                {
                    var helmRelease = HelmRelease.FromSecret(secret);
                    var existing = HelmReleases.FirstOrDefault(h => h.HelmRelease.Name == helmRelease.Name && h.HelmRelease.Namespace == helmRelease.Namespace);
                    if (existing == null)
                    {
                        HelmReleases.Add(new HelmReleaseViewModel(helmRelease));
                    }
                    else
                    {
                        existing.Revisions.Add(helmRelease);
                    }
                }
                else if (resourceEvent == KubernetesResourceEvent.Deleted)
                {
                    var helmRelease = HelmRelease.FromSecret(secret);

                    var existingHelmRelease = HelmReleases.FirstOrDefault(h => h.HelmRelease.Name == helmRelease.Name && h.HelmRelease.Namespace == helmRelease.Namespace);

                    if (existingHelmRelease != null)
                    {
                        var existingRevision = existingHelmRelease.Revisions.FirstOrDefault(r => r.Version == helmRelease.Version);
                        if (existingRevision != null)
                        {
                            if (existingHelmRelease.Revisions.Count == 1)
                            {
                                HelmReleases.Remove(existingHelmRelease);
                            }
                            else
                            {
                                existingHelmRelease.Revisions.Remove(existingRevision);
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Unhandled secret event: {resourceEvent}");
                }
            }
        });

    }
}
