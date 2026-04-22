using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Filters;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels;

public partial class KubernetesResourceTypeListViewModel
    : ListViewModel
{
    public ClusterViewModel Cluster { get; }

    [ObservableProperty]
    public partial bool IsPinned { get; private set; }

    public ResourceType ResourceType { get; }

    public ImmutableArray<ResourceColumn> Columns { get; }

    private INotifyCollectionChanged? _sourceCollection;
    private NotifyCollectionChangedEventHandler? _collectionChangedHandler;

    public KubernetesResourceTypeListViewModel(
        WorkspaceViewModel workspace,
        ClusterViewModel cluster,
        ResourceType resourceType,
        ImmutableArray<ResourceColumn>? columns = null
    )
        : base(
            workspace,
            title: resourceType.PluralDisplayName,
            isNamespaceScoped: resourceType.IsNamespaceScoped,
            namespaceFilters: cluster.NamespaceFilters,
            additionalFilters: cluster.AdditionalFilters.GetValueOrDefault(
                resourceType,
                Enumerable.Empty<ToggleFilter>()
            )
        )
    {
        Cluster = cluster;
        ResourceType = resourceType;
        Columns = columns ?? KubernetesResourceViewModel.GetDefaultColumns();
    }

    public async Task ActivateAsync()
    {
        await Workspace.Window.App.DispatcherQueue.EnqueueAsync(async () =>
        {
            DetachFromCollection();

            bool filter(object x)
            {
                foreach (var toggleFilter in AdditionalFilters.Where(f => f.IsChecked))
                {
                    if (!toggleFilter.Expression.Invoke(x))
                    {
                        return false;
                    }
                }

                var resource = (KubernetesResourceViewModel)x;
                if (
                    ResourceType.IsNamespaceScoped
                    && Workspace.SelectedNamespaceFilter is NamespaceFilter filter
                    && resource.Namespace != filter.Name
                )
                {
                    return false;
                }

                return string.IsNullOrEmpty(SearchText)
                    || resource.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }

            var collection = await Cluster.GetResourcesAsync(ResourceType);
            Items = new AdvancedCollectionView(collection) { Filter = filter };

            foreach (var item in Items.Cast<KubernetesResourceViewModel>())
            {
                item.PropertyChanged += ResourceViewModel_PropertyChanged;
            }

            _sourceCollection = (INotifyCollectionChanged)Items.SourceCollection;
            _collectionChangedHandler = (s, e) =>
            {
                foreach (var oldItem in e.OldItems?.Cast<KubernetesResourceViewModel>() ?? [])
                {
                    oldItem.PropertyChanged -= ResourceViewModel_PropertyChanged;
                }

                foreach (var newItem in e.NewItems?.Cast<KubernetesResourceViewModel>() ?? [])
                {
                    newItem.PropertyChanged += ResourceViewModel_PropertyChanged;
                }
            };
            _sourceCollection.CollectionChanged += _collectionChangedHandler;
        });
    }

    public override async Task OnNavigatedTo()
    {
        await ActivateAsync();
        await Cluster.WatchResource(ResourceType);
    }

    public override async Task OnNavigatedFrom()
    {
        await Cluster.StopWatchResource(ResourceType);
        DetachFromCollection();
    }

    private void DetachFromCollection()
    {
        if (_sourceCollection != null && _collectionChangedHandler != null)
        {
            _sourceCollection.CollectionChanged -= _collectionChangedHandler;
        }
        _sourceCollection = null;
        _collectionChangedHandler = null;

        if (Items != null)
        {
            foreach (var item in Items.Cast<KubernetesResourceViewModel>())
            {
                item.PropertyChanged -= ResourceViewModel_PropertyChanged;
            }
            Items = null;
        }
    }

    private void ResourceViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(KubernetesResourceViewModel.IsSelected))
        {
            DeleteSelectedItemsCommand.NotifyCanExecuteChanged();
            if (!_selectingAll)
            {
                OnPropertyChanged(nameof(IsAllSelected));
            }
        }
    }

    [RelayCommand]
    public void Pin()
    {
        IsPinned = true;
        Workspace.PinResourceType(this);
    }

    [RelayCommand]
    public void UnPin()
    {
        IsPinned = false;
        Workspace.UnPinResourceType(this);
    }

    public override void AddNewItem()
    {
        string? targetNamespace = null;

        if (
            ResourceType.IsNamespaceScoped
            && Workspace.SelectedNamespaceFilter is NamespaceFilter nsFilter
        )
        {
            targetNamespace = nsFilter.Name;
        }

        Workspace.OpenShelfItem(
            new Shelf.CreateKubernetesResourceViewModel(Cluster, ResourceType, targetNamespace)
        );
    }

    protected override async Task DeleteItemsAsync(IReadOnlyCollection<ISelectable> items)
    {
        await Cluster.DeleteResourcesAsync(
            ResourceType,
            [.. items.Cast<KubernetesResourceViewModel>()]
        );
    }
}
