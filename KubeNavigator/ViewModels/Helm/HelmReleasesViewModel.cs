using CommunityToolkit.WinUI;
using KubeNavigator.ViewModels.Resources;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Helm;
public partial class HelmReleasesViewModel : ListViewModel
{
    public HelmReleasesViewModel(WorkspaceViewModel workspace, ClusterViewModel cluster)
        : base(workspace, title: "Helm Releases", isNamespaceScoped: true, cluster.NamespaceFilters)
    {
    }

    public async Task ActivateAsync()
    {
        await Workspace.Window.App.DispatcherQueue.EnqueueAsync(async () =>
        {
            Items = Workspace.HelmReleasesViews;
            Items.Filter = x =>
            {
                var resource = (HelmReleaseViewModel)x;
                //if (resource.DeletionPending)
                //{
                //    return false;
                //}

                if (Workspace.SelectedNamespaceFilter is NamespaceFilter filter && resource.Namespace != filter.Name)
                {
                    return false;
                }

                return string.IsNullOrEmpty(SearchText) || resource.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            };

            foreach (var item in Items.Cast<HelmReleaseViewModel>())
            {
                item.PropertyChanged += ResourceViewModel_PropertyChanged;
            }

            ((INotifyCollectionChanged)Items.SourceCollection).CollectionChanged += (s, e) =>
            {
                foreach (var oldItem in e.OldItems?.Cast<HelmReleaseViewModel>() ?? [])
                {
                    oldItem.PropertyChanged -= ResourceViewModel_PropertyChanged;
                }

                foreach (var newItem in e.NewItems?.Cast<HelmReleaseViewModel>() ?? [])
                {
                    newItem.PropertyChanged += ResourceViewModel_PropertyChanged;
                }
            };
        });
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

    protected override Task DeleteItemsAsync(IReadOnlyCollection<ISelectable> items)
    {
        throw new NotImplementedException();
    }

    public override Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public override Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
