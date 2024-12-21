using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Model.Details;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class DetailsViewModel : ObservableObject
{
    private readonly Action? onClose;

    public DetailsViewModel(KubernetesResourceViewModel selectedResource, IShelfHost shelfHost, Action? onClose = null)
    {
        NavigationStack.Push(selectedResource);
        Cluster = selectedResource.Cluster;
        ShelfHost = shelfHost;
        this.onClose = onClose;
    }

    public KubernetesResourceViewModel SelectedResource => NavigationStack.Peek();

    public bool CanGoBack => NavigationStack.Count > 1;

    public Stack<KubernetesResourceViewModel> NavigationStack { get; } = new Stack<KubernetesResourceViewModel>();
    public ClusterViewModel Cluster { get; }
    public IShelfHost ShelfHost { get; }

    [RelayCommand]
    public async Task NavigateAsync(DetailsLinkItem link)
    {
        if (link.ResourceName == null)
        {
            // todo show error
            return;
        }

        var resource = await Cluster.GetResourceAsync(link.ResourceType, link.ResourceName);

        if (resource != null)
        {
            NavigationStack.Push(resource);
            OnPropertyChanged(nameof(SelectedResource));
            OnPropertyChanged(nameof(CanGoBack));
        }
        else
        {
            // todo show error
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        if (NavigationStack.Count > 1)
        {
            NavigationStack.Pop();
            OnPropertyChanged(nameof(SelectedResource));
            OnPropertyChanged(nameof(CanGoBack));
        }
    }

    public void Close()
    {
        onClose?.Invoke();
    }
}
