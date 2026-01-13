using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DetailsTypes = KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels;

public partial class DetailsViewModel : ObservableObject
{
    private readonly Action? onClose;

    [ObservableProperty]
    private List<DetailsTypes.IDetailsSection> details = [];

    public DetailsViewModel(
        KubernetesResourceViewModel selectedResource,
        IShelfHost shelfHost,
        Action? onClose = null
    )
    {
        NavigationStack.Push(selectedResource);
        Cluster = selectedResource.Cluster;
        ShelfHost = shelfHost;
        this.onClose = onClose;
        SubscribeToDetailsRefresh(selectedResource);
        UpdateDetails();
    }

    public KubernetesResourceViewModel SelectedResource => NavigationStack.Peek();

    public bool CanGoBack => NavigationStack.Count > 1;

    public Stack<KubernetesResourceViewModel> NavigationStack { get; } =
        new Stack<KubernetesResourceViewModel>();
    public ClusterViewModel Cluster { get; }
    public IShelfHost ShelfHost { get; }

    [RelayCommand]
    public async Task NavigateAsync(DetailsTypes.DetailsLinkItem link)
    {
        if (link.ResourceName == null)
        {
            // todo show error
            return;
        }

        var resource = await Cluster.GetResourceAsync(link.ResourceType, link.ResourceName);

        if (resource != null)
        {
            UnsubscribeFromDetailsRefresh(SelectedResource);
            NavigationStack.Push(resource);
            SubscribeToDetailsRefresh(resource);
            OnPropertyChanged(nameof(SelectedResource));
            OnPropertyChanged(nameof(CanGoBack));
            UpdateDetails();
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
            UnsubscribeFromDetailsRefresh(SelectedResource);
            NavigationStack.Pop();
            SubscribeToDetailsRefresh(SelectedResource);
            OnPropertyChanged(nameof(SelectedResource));
            OnPropertyChanged(nameof(CanGoBack));
            UpdateDetails();
        }
    }

    private void SubscribeToDetailsRefresh(KubernetesResourceViewModel resource)
    {
        resource.DetailsRefreshRequested += OnDetailsRefreshRequested;
    }

    private void UnsubscribeFromDetailsRefresh(KubernetesResourceViewModel resource)
    {
        resource.DetailsRefreshRequested -= OnDetailsRefreshRequested;
    }

    private void OnDetailsRefreshRequested(object? sender, EventArgs e)
    {
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        Details = SelectedResource.CreateDetails();
    }

    public void Close()
    {
        onClose?.Invoke();
    }
}
