using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class WorkspaceView : UserControl
{
    private double? _previousShelfHeight;

    public WorkspaceViewModel ViewModel { get; }

    public WorkspaceView(WorkspaceViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ViewModel.ShelfItemsCount) or nameof(ViewModel.IsShelfMaximized) or nameof(ViewModel.SelectedItem))
            {
                SetRowSizes(ViewModel.ShelfItemsCount > 0, ViewModel.IsShelfMaximized, ViewModel.SelectedItem);
            }
        };
        this.InitializeComponent();
        SetRowSizes(ViewModel.ShelfItemsCount > 0, ViewModel.IsShelfMaximized, ViewModel.SelectedItem);
    }

    private void SetRowSizes(bool shelfHasItems, bool shelfIsMaximized, INavigationTarget? navigationTarget)
    {
        if (ShelfRow.Height.IsAbsolute && ShelfRow.Height.Value != 0)
        {
            _previousShelfHeight = ShelfRow.Height.Value;
        }

        if (navigationTarget is SettingsViewModel)
        {
            ShelfResizer.Visibility = Visibility.Collapsed;
            ShelfRow.Height = new GridLength(0);
            ContentRow.Height = new GridLength(1, GridUnitType.Star);
        }
        else if (shelfHasItems && shelfIsMaximized)
        {
            ShelfResizer.Visibility = Visibility.Collapsed;
            ShelfRow.Height = new GridLength(1, GridUnitType.Star);
            ContentRow.Height = new GridLength(1, GridUnitType.Auto);
        }
        else if (shelfHasItems && !shelfIsMaximized)
        {
            ShelfResizer.Visibility = Visibility.Visible;
            ContentRow.Height = new GridLength(1, GridUnitType.Star);
            if (!ShelfRow.Height.IsAbsolute || ShelfRow.Height == new GridLength(0))
            {
                ShelfRow.Height = new GridLength(_previousShelfHeight ?? 300);
            }
        }
        else if (!shelfHasItems)
        {
            ShelfResizer.Visibility = Visibility.Collapsed;
            ShelfRow.Height = new GridLength(1, GridUnitType.Auto);
            ContentRow.Height = new GridLength(1, GridUnitType.Star);
        }
    }

    private async void Shelf_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is IShelfItem shelfItem)
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
