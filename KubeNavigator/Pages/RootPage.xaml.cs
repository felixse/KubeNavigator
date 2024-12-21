using KubeNavigator.ViewModels;
using KubeNavigator.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KubeNavigator.Pages;

public sealed partial class RootPage : Page
{
    public WindowViewModel? ViewModel { get; private set; }

    public RootPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is WindowViewModel windowViewModel)
        {
            ViewModel = windowViewModel;
            if (ViewModel.UserConfirmationService is ConfirmationDialogService confirmationDialogService) // todo: avoid this hack
            {
                confirmationDialogService.Page = this;
            }
        }
    }

    private void TabViewItem_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (sender.DataContext is WorkspaceViewModel workspace)
        {
            workspace.Close();
        }
    }
}
