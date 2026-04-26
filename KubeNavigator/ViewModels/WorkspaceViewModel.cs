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
using KubeNavigator.Helpers;
using KubeNavigator.Models;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.AppCommands;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Filters;
using KubeNavigator.ViewModels.Helm;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels;

public partial class WorkspaceViewModel : ObservableObject, IShelfHost
{
    private ObservableCollection<KubernetesResourceViewModel>? _customResourceDefinitions;
    private readonly ILogger<WorkspaceViewModel> _logger;
    private (string Title, string Message, Func<Task>? FollowUp)? _pendingInfoDialog;
    private string? _pendingNamespaceFilter;

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
        _logger = window.App.LoggingService.LoggerFactory.CreateLogger<WorkspaceViewModel>();

        Pinned = new NavigationGroupViewModel(
            "Pinned",
            new SymbolNavigationGroupIcon("\uE840"),
            []
        );
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
#if DEBUG
        ShelfItems.Add(new ApplicationLogViewModel(App.LoggingService, App.ThemeManager));
        SelectedShelfItem = ShelfItems.First();
#endif
        AppCommands = new ObservableCollection<IAppCommand>(
            FooterItems.Select(x => new NavigateToViewAppCommand(x, this))
        );
        AppCommands.Add(new OpenApplicationLogCommand(this));
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

        // Check for a saved namespace filter before setting the default,
        // so the initial assignment doesn't overwrite the persisted value.
        var viewState = App.ViewStateService.State;
        if (
            viewState.ClusterStates.TryGetValue(cluster.Name, out var clusterState)
            && clusterState.LastNamespaceFilter != null
        )
        {
            _pendingNamespaceFilter = clusterState.LastNamespaceFilter;
        }

        SelectedNamespaceFilter = cluster.NamespaceFilters.First(x => x is AllNamespacesFilter);

        var clusterOverview = new ClusterOverviewViewModel(
            cluster,
            this,
            new KubernetesResourceTypeListViewModel(
                this,
                cluster,
                ResourceType.Event,
                EventViewModel.EventColumns
            )
        );

        var clusterGroup = new NavigationGroupViewModel(
            "Cluster",
            new SymbolNavigationGroupIcon("\uE968"),
            [
                clusterOverview,
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Node,
                    NodeViewModel.NodeColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Namespace,
                    NamespaceViewModel.NamespaceColumns
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
                    PodViewModel.PodColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Deployment,
                    DeploymentViewModel.DeploymentColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.DaemonSet,
                    DaemonSetViewModel.DaemonSetColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.StatefulSet,
                    StatefulSetViewModel.StatefulSetColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ReplicaSet,
                    ReplicaSetViewModel.ReplicaSetColumns
                ),
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
                    ConfigMapViewModel.ConfigMapColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Secret,
                    SecretViewModel.SecretColumns
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
                    ServiceViewModel.ServiceColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.EndpointSlice,
                    EndpointSliceViewModel.EndpointSliceColumns
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
                    ResourceType.PersistentVolumeClaim,
                    PersistentVolumeClaimViewModel.PersistentVolumeClaimColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.PersistentVolume,
                    PersistentVolumeViewModel.PersistentVolumeColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.StorageClass,
                    StorageClassViewModel.StorageClassColumns
                ),
            ]
        );
        var helm = new NavigationGroupViewModel(
            "Helm",
            new PathNavigationGroupIcon(
                "M1.5,8 A6.5,6.5,0,1,1,14.5,8 A6.5,6.5,0,1,1,1.5,8 Z M2.7,8 A5.3,5.3,0,1,0,13.3,8 A5.3,5.3,0,1,0,2.7,8 Z M6.2,8 A1.8,1.8,0,1,1,9.8,8 A1.8,1.8,0,1,1,6.2,8 Z M7.45,5.8 L8.55,5.8 L8.55,3.1 L7.45,3.1 Z M7.45,1.2 L8.55,1.2 L8.55,0.2 L7.45,0.2 Z M9.63,6.42 L10.18,7.38 L12.52,6.03 L11.97,5.07 Z M13.61,4.12 L14.16,5.08 L15.03,4.58 L14.48,3.62 Z M10.18,8.62 L9.63,9.58 L11.97,10.93 L12.52,9.97 Z M14.16,10.92 L13.61,11.88 L14.48,12.38 L15.03,11.42 Z M8.55,10.2 L7.45,10.2 L7.45,12.9 L8.55,12.9 Z M8.55,14.8 L7.45,14.8 L7.45,15.8 L8.55,15.8 Z M6.37,9.58 L5.82,8.62 L3.48,9.97 L4.03,10.93 Z M2.39,11.88 L1.84,10.92 L0.97,11.42 L1.52,12.38 Z M5.82,7.38 L6.37,6.42 L4.03,5.07 L3.48,6.03 Z M1.84,5.08 L2.39,4.12 L1.52,3.62 L0.97,4.58 Z"
            ),
            [new HelmReleasesViewModel(this, cluster)]
        );
        var accessControl = new NavigationGroupViewModel(
            "Access Control",
            new SymbolNavigationGroupIcon("\uE72E"),
            [
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ServiceAccount,
                    ServiceAccountViewModel.ServiceAccountColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ClusterRole,
                    ClusterRoleViewModel.ClusterRoleColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.Role,
                    RoleViewModel.RoleColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.ClusterRoleBinding,
                    ClusterRoleBindingViewModel.ClusterRoleBindingColumns
                ),
                new KubernetesResourceTypeListViewModel(
                    this,
                    cluster,
                    ResourceType.RoleBinding,
                    RoleBindingViewModel.RoleBindingColumns
                ),
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

        // Restore expanded groups from view state
        foreach (var group in NavigationGroups)
        {
            group.IsExpanded = viewState.ExpandedGroups.Contains(group.Name);
            group.PropertyChanged += OnNavigationGroupPropertyChanged;
        }

        // Restore pinned items from view state
        var allNavigationTargets = NavigationGroups
            .SelectMany(g => g.Items)
            .SelectMany(item =>
                item is CustomResourceGroupViewModel crg
                    ? crg.Resources.Cast<INavigationTarget>()
                    : [item]
            )
            .ToList();

        foreach (var pin in viewState.PinnedItems)
        {
            INavigationTarget? target = pin switch
            {
                PinnedClusterOverviewState => allNavigationTargets
                    .OfType<ClusterOverviewViewModel>()
                    .FirstOrDefault(),
                PinnedHelmReleasesState => allNavigationTargets
                    .OfType<Helm.HelmReleasesViewModel>()
                    .FirstOrDefault(),
                PinnedResourceTypeState rt => allNavigationTargets
                    .OfType<KubernetesResourceTypeListViewModel>()
                    .FirstOrDefault(r =>
                        r.ResourceType.Kind == rt.Kind
                        && r.ResourceType.Group == rt.Group
                        && r.ResourceType.Version == rt.Version
                        && r.ResourceType.Plural == rt.Plural
                    ),
                _ => null,
            };
            if (target != null)
            {
                Pinned.Items.Add(new PinnedNavigationTargetViewModel(target, this));
            }
        }
        if (Pinned.Items.Any())
        {
            Pinned.IsExpanded = true;
        }

        // Restore namespace filter from cluster view state
        if (_pendingNamespaceFilter != null)
        {
            var nsFilter = cluster.NamespaceFilters.FirstOrDefault(f =>
                f is NamespaceFilter nf && nf.Name == _pendingNamespaceFilter
            );
            if (nsFilter != null)
            {
                _pendingNamespaceFilter = null;
                SelectedNamespaceFilter = nsFilter;
            }
            else
            {
                cluster.NamespaceFilters.CollectionChanged += OnNamespaceFiltersCollectionChanged;
            }
        }

        AppCommands.Clear();
        foreach (
            var command in NavigationGroups
                .SelectMany(x => x.Items)
                .SelectMany(item =>
                    item is CustomResourceGroupViewModel crg
                        ? crg.Resources.Cast<INavigationTarget>()
                        : [item]
                )
                .OfType<KubernetesResourceTypeListViewModel>()
                .Select(x => new ViewResourceAppCommand(x.ResourceType, this))
        )
        {
            AppCommands.Add(command);
        }
        foreach (var footerItem in FooterItems)
        {
            AppCommands.Add(new NavigateToViewAppCommand(footerItem, this));
        }
        AppCommands.Add(new OpenApplicationLogCommand(this));

        SelectedItem = clusterOverview;

        // Load CRDs in the background so the overview appears immediately.
        _ = LoadCustomResourceDefinitionsAsync(cluster);
    }

    private async Task LoadCustomResourceDefinitionsAsync(ClusterViewModel cluster)
    {
        try
        {
            _customResourceDefinitions = await cluster.GetResourcesAsync(
                ResourceType.CustomResourceDefinition
            );

            // Start watching CRDs immediately so new definitions added to the
            // cluster are picked up without the user navigating to the CRD list first.
            await cluster.WatchResource(ResourceType.CustomResourceDefinition);

            foreach (var item in _customResourceDefinitions)
            {
                if (item.Resource is V1CustomResourceDefinition crd)
                {
                    AddCustomResourceDefinitionToNavigation(crd);
                }
            }

            _customResourceDefinitions.CollectionChanged +=
                OnCustomResourceDefinitionsCollectionChanged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load custom resource definitions");
        }
    }

    private void OnCustomResourceDefinitionsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e
    )
    {
        if (CustomResourcesNavigationGroup == null)
        {
            Log.CustomResourcesNavigationGroupNull(_logger);
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
                        r.ResourceType.Kind == crd.Spec.Names.Kind
                        && r.ResourceType.Version == crd.Spec.Versions.First().Name
                    );
                    if (viewModel != null)
                    {
                        group.Resources.Remove(viewModel);

                        var command = AppCommands
                            .OfType<ViewResourceAppCommand>()
                            .FirstOrDefault(c => c.ResourceType == viewModel.ResourceType);
                        if (command != null)
                        {
                            AppCommands.Remove(command);
                        }
                    }

                    if (group.Resources.Count == 0)
                    {
                        CustomResourcesNavigationGroup.Items.Remove(group);
                    }
                }
            }
        }
    }

    public void DisconnectCluster()
    {
        _customResourceDefinitions?.CollectionChanged -=
            OnCustomResourceDefinitionsCollectionChanged;
        _customResourceDefinitions = null;

        NavigationGroups.Clear();
        Cluster = null;
        Details = null;
        HelmReleasesViews = null;

        SelectedItem = FooterItems.First(f => f is ClusterListViewModel);
    }

    public async Task HandleClusterRemovedAsync(string contextName)
    {
        Cluster?.Context.Disconnect();
        DisconnectCluster();

        var title = "Context Removed";
        var message =
            $"The context \"{contextName}\" has been removed from your kubeconfig. The workspace has been disconnected.";

        if (Window.SelectedWorkspace == this)
        {
            await Window.ContentDialogService.ShowInfoDialogAsync(title, message);
        }
        else
        {
            _pendingInfoDialog = (title, message, null);
        }
    }

    public async Task ShowPendingDialogAsync()
    {
        if (_pendingInfoDialog is var (title, message, followUp))
        {
            _pendingInfoDialog = null;
            await Window.ContentDialogService.ShowInfoDialogAsync(title, message);

            if (followUp is not null)
            {
                await followUp();
            }
        }
    }

    public async Task HandleClusterChangedAsync(ClusterViewModel cluster)
    {
        cluster.Context.Disconnect();

        var title = "Context Changed";
        var message =
            $"The kubeconfig for \"{cluster.Name}\" has been modified. Reconnecting\u2026";

        if (Window.SelectedWorkspace == this)
        {
            await Window.ContentDialogService.ShowInfoDialogAsync(title, message);
            await ReconnectClusterAsync(cluster);
        }
        else
        {
            _pendingInfoDialog = (title, message, () => ReconnectClusterAsync(cluster));
        }
    }

    private async Task ReconnectClusterAsync(ClusterViewModel cluster)
    {
        try
        {
            var reconnected = await Window.ContentDialogService.ShowConnectingDialogAsync(
                cluster.Name,
                ct => cluster.Context.ConnectAsync(ct)
            );

            if (reconnected)
            {
                await SetContextAsync(cluster);
            }
        }
        catch (Exception e)
        {
            Window.ShowMessage(
                "Reconnection Failed",
                $"Failed to reconnect to cluster \"{cluster.Name}\": {e.Message}",
                NotificationSeverity.Error
            );
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
            Log.ClusterNullInAddCrd(_logger);
            return;
        }

        if (CustomResourcesNavigationGroup == null)
        {
            Log.CustomResourcesNavigationGroupNull(_logger);
            return;
        }

        if (
            !CustomResourcesNavigationGroup.Items.Any(c =>
                c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group
            )
        )
        {
            var newGroup = new CustomResourceGroupViewModel(crd.Spec.Group);

            // Restore expanded state and listen for changes
            var viewState = App.ViewStateService.State;
            newGroup.IsExpanded = viewState.ExpandedGroups.Contains(newGroup.GroupName);
            newGroup.PropertyChanged += OnCustomResourceGroupPropertyChanged;

            CustomResourcesNavigationGroup.Items.Add(newGroup);
        }

        var group =
            CustomResourcesNavigationGroup.Items.First(c =>
                c is CustomResourceGroupViewModel group && group.GroupName == crd.Spec.Group
            ) as CustomResourceGroupViewModel;

        if (group != null)
        {
            // todo what if multiple versions?
            var printerColumns = CrdColumnHelper.GetPrinterColumns(crd);
            var resourceType = new ResourceType(
                crd.Spec.Names.Kind,
                crd.Spec.Group,
                crd.Spec.Versions.First().Name,
                crd.Spec.Names.Plural,
                crd.Spec.Scope == "Namespaced",
                crd.Spec.Names.Plural,
                crd.Spec.Names.Singular,
                printerColumns
            );
            var columns = CrdColumnHelper.BuildColumns(resourceType);
            group.Resources.Add(
                new KubernetesResourceTypeListViewModel(this, Cluster, resourceType, columns)
            );
            AppCommands.Add(new ViewResourceAppCommand(resourceType, this, crd.Spec.Group));
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

    public void PinNavigationTarget(INavigationTarget navigationTarget)
    {
        if (Pinned != null)
        {
            Pinned.Items.Add(new PinnedNavigationTargetViewModel(navigationTarget, this));
            Pinned.IsExpanded = true;
            SavePinnedState();
        }
    }

    public void UnPinNavigationTarget(INavigationTarget navigationTarget)
    {
        if (Pinned != null)
        {
            var pinnedItem = Pinned
                .Items.Cast<PinnedNavigationTargetViewModel>()
                .FirstOrDefault(i => i.NavigationTarget == navigationTarget);
            if (pinnedItem != null)
            {
                Pinned.Items.Remove(pinnedItem);
                SavePinnedState();
            }
        }
    }

    public void MovePinnedItem(PinnedNavigationTargetViewModel item, int newIndex)
    {
        if (Pinned == null)
            return;

        var oldIndex = Pinned.Items.IndexOf(item);
        if (oldIndex < 0 || oldIndex == newIndex)
            return;

        // Use Remove+Insert instead of Move to avoid NavigationView
        // container tracking issues that corrupt selection state.
        var wasSelected = SelectedItem == item;
        if (wasSelected)
        {
            SelectedItem = null;
        }

        Pinned.Items.RemoveAt(oldIndex);
        if (newIndex > oldIndex)
        {
            newIndex--;
        }
        Pinned.Items.Insert(newIndex, item);
        SavePinnedState();

        foreach (var pinnedItem in Pinned.Items.OfType<PinnedNavigationTargetViewModel>())
        {
            pinnedItem.NotifyMoveCommandsChanged();
        }

        if (wasSelected)
        {
            App.DispatcherQueue.TryEnqueue(() => SelectedItem = item);
        }
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
            var category = NavigationGroups.FirstOrDefault(c => c.Items.Contains(resourceType));
            if (category != null)
            {
                category.IsExpanded = true;
            }
            else
            {
                // Custom resources are nested inside CustomResourceGroupViewModel,
                // so search one level deeper and expand both the group and subgroup.
                foreach (var group in NavigationGroups)
                {
                    var crGroup = group
                        .Items.OfType<CustomResourceGroupViewModel>()
                        .FirstOrDefault(cr => cr.Resources.Contains(resourceType));
                    if (crGroup != null)
                    {
                        group.IsExpanded = true;
                        crGroup.IsExpanded = true;
                        break;
                    }
                }
            }
        }
    }

    partial void OnSelectedNamespaceFilterChanged(
        INamespaceFilter? oldValue,
        INamespaceFilter? newValue
    )
    {
        HelmReleasesViews?.RefreshFilter(); // todo this should be done in the viewmodel
        if (_pendingNamespaceFilter == null)
        {
            SaveNamespaceFilterState();
        }
    }

    private void OnNamespaceFiltersCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e
    )
    {
        if (_pendingNamespaceFilter == null || Cluster == null)
            return;

        foreach (var item in e.NewItems?.Cast<INamespaceFilter>() ?? [])
        {
            if (item is NamespaceFilter nf && nf.Name == _pendingNamespaceFilter)
            {
                SelectedNamespaceFilter = item;
                _pendingNamespaceFilter = null;
                Cluster.NamespaceFilters.CollectionChanged -= OnNamespaceFiltersCollectionChanged;
                return;
            }
        }
    }

    private void OnNavigationGroupPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(NavigationGroupViewModel.IsExpanded))
        {
            SaveExpandedGroupsState();
        }
    }

    private void OnCustomResourceGroupPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(CustomResourceGroupViewModel.IsExpanded))
        {
            SaveExpandedGroupsState();
        }
    }

    private void SavePinnedState()
    {
        var viewState = App.ViewStateService.State;
        viewState.PinnedItems = Pinned
            .Items.OfType<PinnedNavigationTargetViewModel>()
            .Select<PinnedNavigationTargetViewModel, PinnedItemState?>(p =>
                p.NavigationTarget switch
                {
                    KubernetesResourceTypeListViewModel r => new PinnedResourceTypeState
                    {
                        Kind = r.ResourceType.Kind,
                        Group = r.ResourceType.Group,
                        Version = r.ResourceType.Version,
                        Plural = r.ResourceType.Plural,
                    },
                    ClusterOverviewViewModel => new PinnedClusterOverviewState(),
                    Helm.HelmReleasesViewModel => new PinnedHelmReleasesState(),
                    _ => null,
                }
            )
            .Where(p => p != null)
            .Cast<PinnedItemState>()
            .ToList();
        _ = App.ViewStateService.SaveAsync();
    }

    private void SaveExpandedGroupsState()
    {
        var viewState = App.ViewStateService.State;
        var expanded = NavigationGroups.Where(g => g.IsExpanded).Select(g => g.Name).ToList();

        if (CustomResourcesNavigationGroup != null)
        {
            foreach (var item in CustomResourcesNavigationGroup.Items)
            {
                if (item is CustomResourceGroupViewModel crg && crg.IsExpanded)
                {
                    expanded.Add(crg.GroupName);
                }
            }
        }

        viewState.ExpandedGroups = expanded;
        _ = App.ViewStateService.SaveAsync();
    }

    private void SaveNamespaceFilterState()
    {
        if (Cluster == null || SelectedNamespaceFilter == null)
            return;

        var viewState = App.ViewStateService.State;
        string? filterName = SelectedNamespaceFilter is NamespaceFilter nf ? nf.Name : null;
        if (!viewState.ClusterStates.TryGetValue(Cluster.Name, out var clusterState))
        {
            clusterState = new ClusterViewState();
            viewState.ClusterStates[Cluster.Name] = clusterState;
        }
        clusterState.LastNamespaceFilter = filterName;
        _ = App.ViewStateService.SaveAsync();
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 11001,
            Level = LogLevel.Error,
            Message = "CustomResourcesNavigationGroup is null, cannot process CRD changes"
        )]
        public static partial void CustomResourcesNavigationGroupNull(ILogger logger);

        [LoggerMessage(
            EventId = 11002,
            Level = LogLevel.Error,
            Message = "Cluster is null in AddCustomResourceDefinitionToNavigation"
        )]
        public static partial void ClusterNullInAddCrd(ILogger logger);
    }
}
