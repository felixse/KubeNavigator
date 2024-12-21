using KubeNavigator.Pages;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Windows;

public sealed partial class DetailWindow : Window
{
    public DetailWindowViewModel ViewModel { get; }

    public DetailWindow(DetailWindowViewModel viewModel)
    {
        ViewModel = viewModel;

        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        RootFrame.Navigate(typeof(DetailsWindowRootPage), viewModel);
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        ViewModel.Details.GoBack();
    }
}
