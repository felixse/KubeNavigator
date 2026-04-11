using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Helpers;
using KubeNavigator.Pages;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.UI;

namespace KubeNavigator.Windows;

public sealed partial class MainWindow : Window
{
    public WindowViewModel ViewModel { get; private set; }

    public MainWindow(WindowViewModel viewModel)
    {
        this.InitializeComponent();

        ViewModel = viewModel;
        ViewModel.FilePickerHandler = PickFileAsync;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/Square44x44Logo.targetsize-24.png");
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        AppTitleBar.ActualThemeChanged += AppTitleBar_ActualThemeChanged;

        RootFrame.Navigate(typeof(RootPage), viewModel);
    }

    private async Task<string?> PickFileAsync(IReadOnlyList<string> fileTypes)
    {
        var picker = new FileOpenPicker();
        foreach (var fileType in fileTypes)
        {
            picker.FileTypeFilter.Add(fileType);
        }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
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
