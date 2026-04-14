using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;
using DetailsTypes = KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels;

public partial class DetailsViewModel : ObservableObject
{
    private readonly Action? onClose;

    [ObservableProperty]
    public partial List<DetailsTypes.IDetailsSection> Details { get; private set; } = [];

    public DetailsViewModel(IDetailsSource source, IShelfHost shelfHost, Action? onClose = null)
    {
        NavigationStack.Push(source);
        Cluster = source.Cluster;
        ShelfHost = shelfHost;
        this.onClose = onClose;
        SubscribeToDetailsRefresh(source);
        UpdateDetailsAsync();
    }

    public IDetailsSource SelectedItem => NavigationStack.Peek();

    public bool CanGoBack => NavigationStack.Count > 1;

    public Stack<IDetailsSource> NavigationStack { get; } = new Stack<IDetailsSource>();
    public ClusterViewModel Cluster { get; }
    public IShelfHost ShelfHost { get; }

    [RelayCommand]
    public async Task NavigateAsync(DetailsTypes.LinkContent link)
    {
        if (link.ResourceName == null)
        {
            // todo show error
            return;
        }

        var resource = await Cluster.GetResourceAsync(link.ResourceType, link.ResourceName);

        if (resource != null)
        {
            UnsubscribeFromDetailsRefresh(SelectedItem);
            NavigationStack.Push(resource);
            SubscribeToDetailsRefresh(resource);
            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(CanGoBack));
            await UpdateDetailsAsync();
        }
        else
        {
            // todo show error
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        if (NavigationStack.Count > 1)
        {
            UnsubscribeFromDetailsRefresh(SelectedItem);
            NavigationStack.Pop();
            SubscribeToDetailsRefresh(SelectedItem);
            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(CanGoBack));
            await UpdateDetailsAsync();
        }
    }

    private void SubscribeToDetailsRefresh(IDetailsSource resource)
    {
        resource.DetailsRefreshRequested += OnDetailsRefreshRequested;
    }

    private void UnsubscribeFromDetailsRefresh(IDetailsSource resource)
    {
        resource.DetailsRefreshRequested -= OnDetailsRefreshRequested;
    }

    private async void OnDetailsRefreshRequested(object? sender, EventArgs e)
    {
        await UpdateDetailsAsync();
    }

    private async Task UpdateDetailsAsync()
    {
        Details = await SelectedItem.CreateDetailsAsync();
    }

    public void Close()
    {
        onClose?.Invoke();
    }
}
