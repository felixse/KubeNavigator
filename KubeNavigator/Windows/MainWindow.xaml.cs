using KubeNavigator.Pages;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;

namespace KubeNavigator.Windows;

public sealed partial class MainWindow : Window
{
    public WindowViewModel ViewModel { get; private set; }

    public MainWindow(WindowViewModel viewModel)
    {
        this.InitializeComponent();

        ViewModel = viewModel;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        RootFrame.Navigate(typeof(RootPage), viewModel);
    }
}
