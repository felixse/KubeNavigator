using KubeNavigator.ViewModels;
using KubeNavigator.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace KubeNavigator.Pages;

public sealed partial class RootPage : Page
{
    public WindowViewModel? ViewModel { get; private set; }

    public RootPage()
    {
        this.InitializeComponent();
    }

    private void OnNextTabAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel is { Workspaces.Count: > 1 } vm)
        {
            var index = vm.Workspaces.IndexOf(vm.SelectedWorkspace);
            vm.SelectedWorkspace = vm.Workspaces[(index + 1) % vm.Workspaces.Count];
        }
        args.Handled = true;
    }

    private void OnPreviousTabAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel is { Workspaces.Count: > 1 } vm)
        {
            var index = vm.Workspaces.IndexOf(vm.SelectedWorkspace);
            vm.SelectedWorkspace = vm.Workspaces[(index - 1 + vm.Workspaces.Count) % vm.Workspaces.Count];
        }
        args.Handled = true;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is WindowViewModel windowViewModel)
        {
            ViewModel = windowViewModel;
            if (
                ViewModel.ContentDialogService
                is ContentDialogService contentDialogService
            ) // todo: avoid this hack
            {
                contentDialogService.Page = this;
            }
        }
    }

    private void TabViewItem_CloseRequested(
        TabViewItem sender,
        TabViewTabCloseRequestedEventArgs args
    )
    {
        if (sender.DataContext is WorkspaceViewModel workspace)
        {
            workspace.Close();
        }
    }
}
