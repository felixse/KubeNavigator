using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using k8s;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class KubernetesResourceTypeListViewModel : ListViewModel, IKubernetesResourceEventSubscriber
{
    public ClusterViewModel Cluster { get; }

    public bool Loaded { get; set; } // todo this is not set if this is a pinned tab, use the activated method instead?

    private readonly DispatcherQueueTimer _timer;

    [ObservableProperty]
    public partial bool IsPinned { get; private set; }

    public ResourceType ResourceType { get; }

    private readonly Func<IKubernetesObject<V1ObjectMeta>, KubernetesResourceViewModel> _itemViewModelFactory;

    public KubernetesResourceTypeListViewModel(WorkspaceViewModel workspace, ClusterViewModel cluster, ResourceType resourceType, Func<IKubernetesObject<V1ObjectMeta>, KubernetesResourceViewModel>? itemViewModelFactory = null)
        : base(workspace, title: resourceType.PluralDisplayName, isNamespaceScoped: resourceType.IsNamespaceScoped, namespaceFilters: cluster.NamespaceFilters)
    {
        Cluster = cluster;
        ResourceType = resourceType;

        _itemViewModelFactory = itemViewModelFactory ??= (x) => new KubernetesResourceViewModel(x, ResourceType, Cluster);

        _timer = Workspace.Window.App.DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();
        Timer_Tick(_timer, this);
    }

    public async Task ActivateAsync()
    {
        // todo: keep obs collections in clustervm, create collection view in here, use navto/navfrom to let the cluster sub/unsub

        await Workspace.Window.App.DispatcherQueue.EnqueueAsync(async () =>
        {
            bool filter(object x)
            {
                var resource = (KubernetesResourceViewModel)x;
                //if (resource.DeletionPending)
                //{
                //    return false;
                //}

                if (ResourceType.IsNamespaceScoped && Workspace.SelectedNamespaceFilter is NamespaceFilter filter && resource.Namespace != filter.Name)
                {
                    return false;
                }

                return string.IsNullOrEmpty(SearchText) || resource.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }

            var collection = await Cluster.GetResourcesAsync(ResourceType);
            Items = new AdvancedCollectionView(collection)
            {
                Filter = filter
            };

            foreach (var item in Items.Cast<KubernetesResourceViewModel>())
            {
                item.PropertyChanged += ResourceViewModel_PropertyChanged;
            }

            ((INotifyCollectionChanged)Items.SourceCollection).CollectionChanged += (s, e) =>
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
        });
    }

    public override async Task OnNavigatedTo()
    {
        //var resources = await Cluster.Context.GetResourceRepositoryAsync(ResourceType);
        //resources.AddSubscriber(this);
        await Cluster.WatchResource(ResourceType);
    }

    public override async Task OnNavigatedFrom()
    {
        //var resources = await Cluster.Context.GetResourceRepositoryAsync(ResourceType);
        //resources.RemoveSubscriber(this);
        await Cluster.StopWatchResource(ResourceType);
    }

    private void ResourceViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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

    private void Timer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (!Loaded) return;

        foreach (var item in Items?.Cast<KubernetesResourceViewModel>() ?? [])
        {
            item.Age = item.CalculateAge();
        }
    }

    [RelayCommand]
    public void Pin()
    {
        this.IsPinned = true;
        Workspace.PinResourceType(this);
    }

    [RelayCommand]
    public void UnPin()
    {
        this.IsPinned = false;
        Workspace.UnPinResourceType(this);
    }

    protected override async Task DeleteItemsAsync(IReadOnlyCollection<ISelectable> items)
    {
        await Cluster.DeleteResourcesAsync(ResourceType, [.. items.Cast<KubernetesResourceViewModel>()]);
    }

    public Task OnResourceEvent(KubernetesResourceEvent resourceEvent, ResourceType resourceType, IKubernetesObject<V1ObjectMeta> resource)
    {
        Workspace.App.DispatcherQueue.EnqueueAsync(() =>
        {
            var vm = _itemViewModelFactory(resource);
            Items.Add(vm);
        });

        return Task.CompletedTask;
    }
}
