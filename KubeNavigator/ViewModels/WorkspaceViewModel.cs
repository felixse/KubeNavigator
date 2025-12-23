using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class WorkspaceViewModel : ObservableRecipient, IShelfHost
{
    private ObservableCollection<KubernetesResourceViewModel> _customResourceDefinitions;

    public event EventHandler? Closed;

    [ObservableProperty]
    public partial DetailsViewModel? Details { get; private set; }

    public ObservableCollection<IShelfItem> ShelfItems { get; } = [];

    public int ShelfItemsCount => ShelfItems.Count;

    [ObservableProperty]
    public partial IShelfItem? SelectedShelfItem { get; set; }


    [ObservableProperty]
    public partial ClusterViewModel? Cluster { get; private set; }
    public WindowViewModel Window { get; }

    public AppViewModel App => Window.App;

    public ObservableCollection<NavigationGroupViewModel> NavigationGroups { get; } = [];

    public IReadOnlyCollection<Navigation.INavigationTarget> FooterItems { get; private set; }

    public NavigationGroupViewModel Pinned { get; }

    [ObservableProperty]
    public partial Navigation.INavigationTarget? SelectedItem { get; set; }

    [ObservableProperty]
    public partial INamespaceFilter? SelectedNamespaceFilter { get; set; }
    public NavigationGroupViewModel CustomResourcesNavigationGroup { get; private set; }

    [ObservableProperty]
    public partial bool IsShelfMaximized { get; set; }

    public AdvancedCollectionView HelmReleasesViews { get; private set; }

    public WorkspaceViewModel(WindowViewModel window)
    {
        Window = window;

        Pinned = new NavigationGroupViewModel("Pinned", "\uE840", []);
        // todo load pinned from settings

        var portForwards = new PortForwardsViewModel(this, window.App.ForwardedPorts);

        FooterItems =
        [
            portForwards,
            new ClusterListViewModel(this, portForwards),
            window.App.Settings,
        ];

        ShelfItems.CollectionChanged += OnShelfItemsCollectionChanged;

        SelectedItem = FooterItems.First(f => f is ClusterListViewModel);
        ShelfItems.Add(new ApplicationLogViewModel(App.LoggingService, App.ThemeManager));
        SelectedShelfItem = ShelfItems.First();
    }

    public async Task SetContextAsync(ClusterViewModel cluster)
    {
        Cluster = cluster;

        HelmReleasesViews = new AdvancedCollectionView(Cluster.HelmReleases);
        HelmReleasesViews.SortDescriptions.Add(new SortDescription(nameof(HelmReleaseViewModel.Name), SortDirection.Ascending));

        NavigationGroups.Clear();

        SelectedNamespaceFilter = cluster.NamespaceFilters.First(x => x is AllNamespacesFilter);

        var clusterGroup = new NavigationGroupViewModel("Cluster", "\uE968", [
            new TodoViewModel("Overview"),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Node),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Namespace),
            new TodoViewModel("Events"),
        ]);
        var workloads = new NavigationGroupViewModel("Workloads", "\uEE40", [
            new TodoViewModel("Overview"),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Pod, (x) => new PodViewModel((V1Pod)x, Cluster)),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Deployment),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.DaemonSet),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.StatefulSet),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ReplicaSet),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ReplicationController),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Job),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.CronJob),
        ]);
        var config = new NavigationGroupViewModel("Config", "\uF259", [
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ConfigMap),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Secret),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ResourceQuota),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.LimitRange),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.HorizontalPodAutoscaler),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.PodDisruptionBudget),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.PriorityClass),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.RuntimeClass),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Lease),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.MutatingWebhookConfiguration),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ValidatingWebhookConfiguration),
        ]);
        var network = new NavigationGroupViewModel("Network", "\uED5D", [
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Service),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Endpoint),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Ingress),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.IngressClass),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.NetworkPolicy),
        ]);
        var storage = new NavigationGroupViewModel("Storage", "\uEDA2", [
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.PersistentVolumeClaim),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.PersistentVolume),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.StorageClass),
        ]);
        var helm = new NavigationGroupViewModel("Helm", "\uEE94", [
            new TodoViewModel("Charts"),
            new HelmReleasesViewModel(this, cluster),
        ]);
        var accessControl = new NavigationGroupViewModel("Access Control", "\uE72E", [
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ServiceAccount),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ClusterRole),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Role),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ClusterRoleBinding),
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.RoleBinding),
        ]);
        CustomResourcesNavigationGroup = new NavigationGroupViewModel("Custom Resources", "\uEA86", [
            new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.CustomResourceDefinition),
        ]);

        NavigationGroups.Add(Pinned);
        NavigationGroups.Add(clusterGroup);
        NavigationGroups.Add(workloads);
        NavigationGroups.Add(config);
        NavigationGroups.Add(network);
        NavigationGroups.Add(storage);
        NavigationGroups.Add(helm);
        NavigationGroups.Add(accessControl);
        NavigationGroups.Add(CustomResourcesNavigationGroup);

        _customResourceDefinitions = await cluster.GetResourcesAsync(ResourceType.CustomResourceDefinition);

        foreach (var item in _customResourceDefinitions)
        {
            if (item.Resource is V1CustomResourceDefinition crd)
            {
                AddCustomResourceDefinitionToNavigation(crd);
            }
        }


        _customResourceDefinitions.CollectionChanged += OnCustomResourceDefinitionsCollectionChanged;

        SelectedItem = null;
    }

    private void OnCustomResourceDefinitionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.NewItems?.Cast<KubernetesResourceViewModel>() ?? [])
        {
            if (item.Resource is V1CustomResourceDefinition crd)
            {
                AddCustomResourceDefinitionToNavigation(crd);
            }
        }

        foreach (var item in e.OldItems?.Cast<KubernetesResourceViewModel>() ?? [])
        {
            if (item.Resource is V1CustomResourceDefinition crd)
            {
                var group = CustomResourcesNavigationGroup.Items.FirstOrDefault(c => c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group) as CustomResourceGroupViewModel;
                if (group != null)
                {
                    var viewModel = group.Resources.FirstOrDefault(r => r.ResourceType.Version == crd.Spec.Versions.First().Name);
                    if (viewModel != null)
                    {
                        group.Resources.Remove(viewModel);
                    }
                }
            }

        }
    }

    [RelayCommand]
    public void Close()
    {
        _customResourceDefinitions.CollectionChanged -= OnCustomResourceDefinitionsCollectionChanged;
        ShelfItems.CollectionChanged -= OnShelfItemsCollectionChanged;

        Window.Workspaces.Remove(this);
        Closed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void CloseOthers()
    {
        foreach (var workspace in Window.Workspaces.Where(t => t != this).ToList())
        {
            workspace.Close();
        }
    }

    private void AddCustomResourceDefinitionToNavigation(V1CustomResourceDefinition crd)
    {
        if (!CustomResourcesNavigationGroup.Items.Any(c => c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group))
        {
            CustomResourcesNavigationGroup.Items.Add(new CustomResourceGroupViewModel(crd.Spec.Group));
        }

        var group = CustomResourcesNavigationGroup.Items.First(c => c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group) as CustomResourceGroupViewModel;

        if (group != null)
        {
            // todo what if multiple versions?
            group.Resources.Add(new KubernetesResourceTypeListViewModel(this, Cluster, new ResourceType(crd.Spec.Group, crd.Spec.Versions.First().Name, crd.Spec.Names.Plural, crd.Spec.Scope == "Namespaced", crd.Spec.Names.Plural, crd.Spec.Names.Singular)));

        }
    }

    private void OnShelfItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ShelfItems.Count == 0)
        {
            IsShelfMaximized = false;
        }
        OnPropertyChanged(nameof(ShelfItemsCount));
    }

    public async Task CloseShelfItemAsync(IShelfItem item)
    {
        if (ShelfItems.Remove(item))
        {
            await item.OnCloseAsync();
        }
    }


    [RelayCommand]
    public void MaximizeShelf()
    {
        IsShelfMaximized = true;
    }

    [RelayCommand]
    public void RestoreShelf()
    {
        IsShelfMaximized = false;
    }

    [RelayCommand]
    public void ClosePanel()
    {
        Details?.Close();
        Details = null;
    }

    public void OpenShelfItem(IShelfItem item)
    {
        ClosePanel();

        var existing = ShelfItems.FirstOrDefault(t => t.Resource == item.Resource && item.GetType() == t.GetType());
        if (existing != null)
        {
            SelectedShelfItem = existing;
            return;
        }

        ShelfItems.Add(item);
        SelectedShelfItem = item;
    }

    public void OpenDetails(ISelectable item, ListViewModel source)
    {
        if (item is KubernetesResourceViewModel resource) // todo support other types, introduce IDetails?
        {
            Details = new DetailsViewModel(resource, this, () =>
            {
                source.SelectedItem = null;
            });
        }
    }

    public void PinResourceType(KubernetesResourceTypeListViewModel resourceType)
    {
        if (Pinned != null)
        {
            Pinned.Items.Add(new PinnedNavigationTargetViewModel(resourceType, this));
            Pinned.IsExpanded = true;
        }
        // todo persist
    }

    public void UnPinResourceType(INavigationTarget navigationTarget)
    {
        if (Pinned != null)
        {
            var pinnedItem = Pinned.Items.Cast<PinnedNavigationTargetViewModel>().FirstOrDefault(i => i.NavigationTarget == navigationTarget);
            if (pinnedItem != null)
            {
                Pinned.Items.Remove(pinnedItem);
            }
        }
        // todo persist
    }

    partial void OnSelectedItemChanged(Navigation.INavigationTarget? oldValue, Navigation.INavigationTarget? newValue)
    {
        if (oldValue is KubernetesResourceTypeListViewModel oldResourceType)
        {
            oldResourceType.SelectedItem = null;
        }

        Details = null;

        if (newValue is KubernetesResourceTypeListViewModel resourceType)
        {
            resourceType.Loaded = true;
            var category = NavigationGroups.FirstOrDefault(c => c.Items.Contains(resourceType));
            if (category != null)
            {
                category.IsExpanded = true;
            }
        }
    }

    partial void OnSelectedNamespaceFilterChanged(INamespaceFilter oldValue, INamespaceFilter newValue)
    {
        HelmReleasesViews.RefreshFilter(); // todo this should be done in the viewmodel
    }
}
