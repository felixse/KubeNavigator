using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Helpers;
using KubeNavigator.Pages;
using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.UI;

namespace KubeNavigator.Windows;

public sealed partial class DetailWindow : WinUIEx.WindowEx
{
    private const int ExpandedWidth = 1440;

    public DetailWindowViewModel ViewModel { get; }

    public DetailWindow(DetailWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.FilePickerHandler = PickFileAsync;

        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("WindowIcon.ico");
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        AppTitleBar.ActualThemeChanged += AppTitleBar_ActualThemeChanged;

        ViewModel.ShelfItems.CollectionChanged += OnShelfItemsChanged;

        RootFrame.Navigate(typeof(DetailsWindowRootPage), viewModel);
    }

    private void OnShelfItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var size = AppWindow.Size;

        if (ViewModel.ShelfItems.Count > 0 && size.Width < ExpandedWidth)
        {
            Width = ExpandedWidth;
        }
        else if (ViewModel.ShelfItems.Count == 0)
        {
            var page = (RootFrame.Content as DetailsWindowRootPage);
            var panelWidth = (int)(page?.PanelActualWidth ?? size.Width);

            Width = panelWidth;
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

    private async void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        await ViewModel.Details.GoBackAsync();
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
