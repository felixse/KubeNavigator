using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using FuseSharp;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.AppCommands;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;
using Windows.UI.WebUI;

namespace KubeNavigator.ViewModels;

public partial class WorkspaceViewModel : ObservableObject, IShelfHost
{
    private ObservableCollection<KubernetesResourceViewModel>? _customResourceDefinitions;

    public event EventHandler? Closed;

    [ObservableProperty]
    public partial DetailsViewModel? Details { get; private set; }

    [ObservableProperty]
    public partial string? CommandText { get; set; }

    [ObservableProperty]
    public partial AdvancedCollectionView FilteredAppCommands { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<IAppCommand> AppCommands { get; set; }

    [ObservableProperty]
    public partial IAppCommand? SelectedCommand { get; set; }

    public ObservableCollection<IShelfItem> ShelfItems { get; } = [];

    public int ShelfItemsCount => ShelfItems.Count;

    [ObservableProperty]
    public partial IShelfItem? SelectedShelfItem { get; set; }

    private readonly Fuse _fuse;

    [ObservableProperty]
    public partial ClusterViewModel? Cluster { get; private set; }
    public WindowViewModel Window { get; }

    public AppViewModel App => Window.App;

    public ObservableCollection<NavigationGroupViewModel> NavigationGroups { get; } = [];

    public ObservableCollection<INavigationTarget> FooterItems { get; private set; }

    public NavigationGroupViewModel Pinned { get; }

    [ObservableProperty]
    public partial Navigation.INavigationTarget? SelectedItem { get; set; }

    [ObservableProperty]
    public partial INamespaceFilter? SelectedNamespaceFilter { get; set; }
    public NavigationGroupViewModel? CustomResourcesNavigationGroup { get; private set; }

    [ObservableProperty]
    public partial bool IsShelfMaximized { get; set; }

    public AdvancedCollectionView? HelmReleasesViews { get; private set; }

    public WorkspaceViewModel(WindowViewModel window)
    {
        Window = window;

        Pinned = new NavigationGroupViewModel("Pinned", new SymbolNavigationGroupIcon("\uE840"), []);
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
        AppCommands = new ObservableCollection<IAppCommand>(
            FooterItems.Select(x => new NavigateToViewAppCommand(x, this))
        );
        FilteredAppCommands = new AdvancedCollectionView(AppCommands);
        FilteredAppCommands.Filter = (obj) =>
        {
            if (string.IsNullOrWhiteSpace(CommandText))
            {
                return true;
            }
            else if (obj is IAppCommand command)
            {
                var result = _fuse.Search(CommandText, command.Name);

                return result?.Score < 0.4;
            }
            return false;
        };

        _fuse = new Fuse(threshold: 0.2);
    }

    public async Task SetContextAsync(ClusterViewModel cluster)
    {
        Cluster = cluster;

        HelmReleasesViews = new AdvancedCollectionView(Cluster.HelmReleases);
        HelmReleasesViews.SortDescriptions.Add(
            new SortDescription(nameof(HelmReleaseViewModel.Name), SortDirection.Ascending)
        );

        NavigationGroups.Clear();

        SelectedNamespaceFilter = cluster.NamespaceFilters.First(x => x is AllNamespacesFilter);

        var clusterGroup = new NavigationGroupViewModel(
            "Cluster",
            new SymbolNavigationGroupIcon("\uE968"),
            [
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Node),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Namespace,
                    (x) => new NamespaceViewModel((V1Namespace)x, Cluster)
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Event,
                    (x) => new SecretViewModel((Eventsv1Event)x, Cluster)
                ),
            ]
        );
        var workloads = new NavigationGroupViewModel(
            "Workloads",
            new SymbolNavigationGroupIcon("\uEE40"),
            [
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Pod,
                    (x) => new PodViewModel((V1Pod)x, Cluster)
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Deployment),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.DaemonSet),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.StatefulSet),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ReplicaSet),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ReplicationController
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Job),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.CronJob),
            ]
        );
        var config = new NavigationGroupViewModel(
            "Config",
            new SymbolNavigationGroupIcon("\uF259"),
            [
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ConfigMap,
                    (x) => new ConfigMapViewModel((V1ConfigMap)x, Cluster)
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Secret,
                    (x) => new EventViewModel((Eventsv1Event)x, Cluster)
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ResourceQuota),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.LimitRange),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.HorizontalPodAutoscaler
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.PodDisruptionBudget
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.PriorityClass),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.RuntimeClass),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Lease),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.MutatingWebhookConfiguration
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ValidatingWebhookConfiguration
                ),
            ]
        );
        var network = new NavigationGroupViewModel(
            "Network",
            new SymbolNavigationGroupIcon("\uED5D"),
            [
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Service,
                    (x) => new ServiceViewModel((V1Service)x, Cluster)
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Endpoint),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Ingress),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.IngressClass),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.NetworkPolicy),
            ]
        );
        var storage = new NavigationGroupViewModel(
            "Storage",
            new SymbolNavigationGroupIcon("\uEDA2"),
            [
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.PersistentVolumeClaim
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.PersistentVolume
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.StorageClass),
            ]
        );
        var helm = new NavigationGroupViewModel(
            "Helm",
            new PathNavigationGroupIcon("M1.5,8 A6.5,6.5,0,1,1,14.5,8 A6.5,6.5,0,1,1,1.5,8 Z M2.7,8 A5.3,5.3,0,1,0,13.3,8 A5.3,5.3,0,1,0,2.7,8 Z M6.2,8 A1.8,1.8,0,1,1,9.8,8 A1.8,1.8,0,1,1,6.2,8 Z M7.45,5.8 L8.55,5.8 L8.55,3.1 L7.45,3.1 Z M7.45,1.2 L8.55,1.2 L8.55,0.2 L7.45,0.2 Z M9.63,6.42 L10.18,7.38 L12.52,6.03 L11.97,5.07 Z M13.61,4.12 L14.16,5.08 L15.03,4.58 L14.48,3.62 Z M10.18,8.62 L9.63,9.58 L11.97,10.93 L12.52,9.97 Z M14.16,10.92 L13.61,11.88 L14.48,12.38 L15.03,11.42 Z M8.55,10.2 L7.45,10.2 L7.45,12.9 L8.55,12.9 Z M8.55,14.8 L7.45,14.8 L7.45,15.8 L8.55,15.8 Z M6.37,9.58 L5.82,8.62 L3.48,9.97 L4.03,10.93 Z M2.39,11.88 L1.84,10.92 L0.97,11.42 L1.52,12.38 Z M5.82,7.38 L6.37,6.42 L4.03,5.07 L3.48,6.03 Z M1.84,5.08 L2.39,4.12 L1.52,3.62 L0.97,4.58 Z"),
            [new HelmReleasesViewModel(this, cluster)]
        );
        var accessControl = new NavigationGroupViewModel(
            "Access Control",
            new SymbolNavigationGroupIcon("\uE72E"),
            [
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ServiceAccount),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.ClusterRole),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.Role),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ClusterRoleBinding
                ),
                new KubernetesResourceTypeListViewModel(this, cluster, ResourceType.RoleBinding),
            ]
        );
        CustomResourcesNavigationGroup = new NavigationGroupViewModel(
            "Custom Resources",
            new SymbolNavigationGroupIcon("\uEA86"),
            [
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.CustomResourceDefinition
                ),
            ]
        );

        NavigationGroups.Add(Pinned);
        NavigationGroups.Add(clusterGroup);
        NavigationGroups.Add(workloads);
        NavigationGroups.Add(config);
        NavigationGroups.Add(network);
        NavigationGroups.Add(storage);
        NavigationGroups.Add(helm);
        NavigationGroups.Add(accessControl);
        NavigationGroups.Add(CustomResourcesNavigationGroup);

        AppCommands = new ObservableCollection<IAppCommand>(
            NavigationGroups
                .SelectMany(x => x.Items)
                .Where(x => x is KubernetesResourceTypeListViewModel)
                .Cast<KubernetesResourceTypeListViewModel>()
                .Select(x => new ViewResourceAppCommand(x.ResourceType, this))
        );
        foreach (var footerItem in FooterItems)
        {
            AppCommands.Add(new NavigateToViewAppCommand(footerItem, this));
        }

        _customResourceDefinitions = await cluster.GetResourcesAsync(
            ResourceType.CustomResourceDefinition
        );

        foreach (var item in _customResourceDefinitions)
        {
            if (item.Resource is V1CustomResourceDefinition crd)
            {
                AddCustomResourceDefinitionToNavigation(crd);
            }
        }

        _customResourceDefinitions.CollectionChanged +=
            OnCustomResourceDefinitionsCollectionChanged;

        SelectedItem = null;
    }

    private void OnCustomResourceDefinitionsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e
    )
    {
        if (CustomResourcesNavigationGroup == null)
        {
            // todo log error
            return;
        }

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
                var group =
                    CustomResourcesNavigationGroup.Items.FirstOrDefault(c =>
                        c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group
                    ) as CustomResourceGroupViewModel;
                if (group != null)
                {
                    var viewModel = group.Resources.FirstOrDefault(r =>
                        r.ResourceType.Version == crd.Spec.Versions.First().Name
                    );
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
        _customResourceDefinitions?.CollectionChanged -=
            OnCustomResourceDefinitionsCollectionChanged;
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

    [RelayCommand]
    public void SelectNextCommand()
    {
        if (SelectedCommand == null)
        {
            return;
        }

        var index = FilteredAppCommands.IndexOf(SelectedCommand);
        if (index < FilteredAppCommands.Count - 1)
        {
            var next = FilteredAppCommands.ElementAt(index + 1);
            SelectedCommand = (IAppCommand)next;
        }
    }

    [RelayCommand]
    public void SelectPreviousCommand()
    {
        if (SelectedCommand == null)
        {
            return;
        }

        var index = FilteredAppCommands.IndexOf(SelectedCommand);
        if (index > 0)
        {
            var next = FilteredAppCommands.ElementAt(index - 1);
            SelectedCommand = (IAppCommand)next;
        }
    }

    [RelayCommand]
    public async Task ExecuteSelectedCommand()
    {
        Window.IsCommandPanelOpen = false;
        if (SelectedCommand != null)
        {
            await SelectedCommand.ExecuteAsync();
        }
    }

    private void AddCustomResourceDefinitionToNavigation(V1CustomResourceDefinition crd)
    {
        if (Cluster == null)
        {
            // todo log error
            return;
        }

        if (CustomResourcesNavigationGroup == null)
        {
            // todo log error
            return;
        }

        if (
            !CustomResourcesNavigationGroup.Items.Any(c =>
                c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group
            )
        )
        {
            CustomResourcesNavigationGroup.Items.Add(
                new CustomResourceGroupViewModel(crd.Spec.Group)
            );
        }

        var group =
            CustomResourcesNavigationGroup.Items.First(c =>
                c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group
            ) as CustomResourceGroupViewModel;

        if (group != null)
        {
            // todo what if multiple versions?
            group.Resources.Add(
                new KubernetesResourceTypeListViewModel(
                    this,
                    Cluster,
                    new ResourceType(
                        crd.Spec.Names.Kind,
                        crd.Spec.Group,
                        crd.Spec.Versions.First().Name,
                        crd.Spec.Names.Plural,
                        crd.Spec.Scope == "Namespaced",
                        crd.Spec.Names.Plural,
                        crd.Spec.Names.Singular
                    )
                )
            );
        }
    }

    partial void OnCommandTextChanged(string? oldValue, string? newValue)
    {
        FilteredAppCommands.RefreshFilter();
        SelectedCommand = FilteredAppCommands.Cast<IAppCommand>().FirstOrDefault();
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

        var existing = ShelfItems.FirstOrDefault(t =>
            t.Resource == item.Resource && item.GetType() == t.GetType()
        );
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
        if (item is IDetailsSource detailsSource)
        {
            Details = new DetailsViewModel(
                detailsSource,
                this,
                () =>
                {
                    source.SelectedItem = null;
                }
            );
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
            var pinnedItem = Pinned
                .Items.Cast<PinnedNavigationTargetViewModel>()
                .FirstOrDefault(i => i.NavigationTarget == navigationTarget);
            if (pinnedItem != null)
            {
                Pinned.Items.Remove(pinnedItem);
            }
        }
        // todo persist
    }

    partial void OnSelectedItemChanged(
        Navigation.INavigationTarget? oldValue,
        Navigation.INavigationTarget? newValue
    )
    {
        if (oldValue is ListViewModel oldList)
        {
            oldList.SelectedItem = null;
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

    partial void OnSelectedNamespaceFilterChanged(
        INamespaceFilter? oldValue,
        INamespaceFilter? newValue
    )
    {
        HelmReleasesViews?.RefreshFilter(); // todo this should be done in the viewmodel
    }
}
