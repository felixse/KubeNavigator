using System.Collections.Specialized;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Shelf;
using KubeNavigator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KubeNavigator.Pages;

public sealed partial class DetailsWindowRootPage : Page
{
    public DetailWindowViewModel? ViewModel { get; private set; }

    public DetailsWindowRootPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is DetailWindowViewModel viewModel)
        {
            ViewModel = viewModel;
            if (
                ViewModel.ContentDialogService
                is ContentDialogService contentDialogService
            )
            {
                contentDialogService.Page = this;
            }

            ViewModel.ShelfItems.CollectionChanged += OnShelfItemsChanged;
            UpdateColumnLayout();
        }
    }

    private void OnShelfItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateColumnLayout();
    }

    private void UpdateColumnLayout()
    {
        if (ViewModel is null) return;

        if (ViewModel.ShelfItems.Count > 0)
        {
            // Lock the panel to its current pixel width so it doesn't grow
            PanelColumn.Width = new GridLength(PanelColumn.ActualWidth, GridUnitType.Pixel);
            SplitterColumn.Width = GridLength.Auto;
            ShelfColumn.Width = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            // Return to stretch mode so the panel fills the window
            PanelColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitterColumn.Width = new GridLength(0);
            ShelfColumn.Width = new GridLength(0);
        }
    }

    public double PanelActualWidth => PanelColumn.ActualWidth;

    private async void Shelf_TabCloseRequested(
        TabView sender,
        TabViewTabCloseRequestedEventArgs args
    )
    {
        if (args.Item is IShelfItem shelfItem && ViewModel is not null)
        {
            await ViewModel.CloseShelfItemAsync(shelfItem);
        }
    }

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is AppBarButton button) // todo terrible workaround but I cannot bind to ViewModel from inside a DataTemplate
        {
            var command = button.CommandParameter as ItemCommand;
            command?.Command.Execute(ViewModel);
        }
    }
}
