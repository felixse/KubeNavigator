using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CliWrap;
using CommunityToolkit.WinUI.Helpers;
using KubeNavigator.Pages;
using KubeNavigator.Services;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        AppTitleBar.ActualThemeChanged += AppTitleBar_ActualThemeChanged;

        RootFrame.Navigate(typeof(RootPage), viewModel);
        RootFrame.Loaded += OnRootFrameLoaded;
    }

    private async void OnRootFrameLoaded(object sender, RoutedEventArgs e)
    {
        RootFrame.Loaded -= OnRootFrameLoaded;
        await CheckCliToolsAsync();
    }

    private async Task CheckCliToolsAsync()
    {
        var settings = ViewModel.App.Settings;
        var kubectlAvailable = await IsToolAvailableAsync(settings.KubectlPath, "kubectl");
        var helmAvailable = await IsToolAvailableAsync(settings.HelmPath, "helm");

        if (kubectlAvailable && helmAvailable)
        {
            return;
        }

        var missing = (kubectlAvailable, helmAvailable) switch
        {
            (false, false) => "kubectl and helm",
            (false, true) => "kubectl",
            _ => "helm",
        };

        var dialog = new ContentDialog
        {
            Title = "CLI tools not found",
            Content =
                $"Could not find {missing} on this system. Some functionality may not work correctly.\n\nYou can configure custom paths in Settings.",
            PrimaryButtonText = "Go to Settings",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
            RequestedTheme =
                ViewModel.App.ThemeManager.GetEffectiveTheme() == ThemeManager.EffectiveTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.NavigateToSettings();
        }
    }

    private static async Task<bool> IsToolAvailableAsync(string customPath, string defaultName)
    {
        var tool = string.IsNullOrWhiteSpace(customPath) ? defaultName : customPath;

        try
        {
            await Cli.Wrap(tool)
                .WithArguments("version")
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
            return true;
        }
        catch
        {
            return false;
        }
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
