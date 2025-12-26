using CommunityToolkit.WinUI.Helpers;
using KubeNavigator.Pages;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

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
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        AppTitleBar.ActualThemeChanged += AppTitleBar_ActualThemeChanged;

        RootFrame.Navigate(typeof(DetailsWindowRootPage), viewModel);
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        ViewModel.Details.GoBack();
    }

    private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySystemThemeToCaptionButtons(this);
    }

    private void AppTitleBar_ActualThemeChanged(FrameworkElement sender, object args)
    {
        ApplySystemThemeToCaptionButtons(this);
    }

    public void ApplySystemThemeToCaptionButtons(Window window)
    {
        var res = Application.Current.Resources;
        Color buttonForegroundColor;
        Color buttonHoverForegroundColor;

        Color buttonHoverBackgroundColor;
        if (ViewModel.App.ThemeManager.GetEffectiveTheme() == ThemeManager.EffectiveTheme.Dark)
        {
            buttonForegroundColor = "#FFFFFF".ToColor();
            buttonHoverForegroundColor = "#FFFFFF".ToColor();

            buttonHoverBackgroundColor = "#0FFFFFFF".ToColor();
        }
        else
        {
            buttonForegroundColor = "#191919".ToColor();
            buttonHoverForegroundColor = "#191919".ToColor();

            buttonHoverBackgroundColor = "#09000000".ToColor();
        }
        res["WindowCaptionForeground"] = buttonForegroundColor;

        window.AppWindow.TitleBar.ButtonForegroundColor = buttonForegroundColor;
        window.AppWindow.TitleBar.ButtonHoverForegroundColor = buttonHoverForegroundColor;

        window.AppWindow.TitleBar.ButtonHoverBackgroundColor = buttonHoverBackgroundColor;
    }
}
