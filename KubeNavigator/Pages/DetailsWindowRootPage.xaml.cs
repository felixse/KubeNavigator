using KubeNavigator.ViewModels;
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
            if (ViewModel.UserConfirmationService is ConfirmationDialogService confirmationDialogService)
            {
                confirmationDialogService.Page = this;
            }
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
