using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Shelf;
using DetailsTypes = KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels;

public partial class DetailsViewModel : ObservableObject
{
    private readonly Action? onClose;

    [ObservableProperty]
    public partial List<DetailsTypes.IDetailsSection> Details { get; private set; } = [];

    public event EventHandler? Navigating;

    public event EventHandler? Navigated;

    public event EventHandler<NavigationEntry>? NavigatedBack;

    public DetailsViewModel(IDetailsSource source, IShelfHost shelfHost, Action? onClose = null)
    {
        NavigationStack.Push(new NavigationEntry(source));
        Cluster = source.Cluster;
        ShelfHost = shelfHost;
        this.onClose = onClose;
        SubscribeToDetailsRefresh(source);
        UpdateDetailsAsync();
    }

    public IDetailsSource SelectedItem => NavigationStack.Peek().Source;

    public bool CanGoBack => NavigationStack.Count > 1;

    public Stack<NavigationEntry> NavigationStack { get; } = new Stack<NavigationEntry>();
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
            Navigating?.Invoke(this, EventArgs.Empty);
            UnsubscribeFromDetailsRefresh(SelectedItem);
            NavigationStack.Push(new NavigationEntry(resource));
            SubscribeToDetailsRefresh(resource);
            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(CanGoBack));
            await UpdateDetailsAsync();
            Navigated?.Invoke(this, EventArgs.Empty);
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
            Navigating?.Invoke(this, EventArgs.Empty);
            UnsubscribeFromDetailsRefresh(SelectedItem);
            NavigationStack.Pop();
            var entry = NavigationStack.Peek();
            SubscribeToDetailsRefresh(entry.Source);
            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(CanGoBack));
            await UpdateDetailsAsync();
            NavigatedBack?.Invoke(this, entry);
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

public class NavigationEntry(IDetailsSource source)
{
    public IDetailsSource Source { get; } = source;

    public double ScrollOffset { get; set; }
}
