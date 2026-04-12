using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using KubeNavigator.Models;
using KubeNavigator.Services;
using KubeNavigator.ViewModels;
using KubeNavigator.Views;
using KubeNavigator.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator;

public partial class App : Application, IWindowManager
{
    private readonly List<Window> _windows = [];
    private Window? _activeWindow;
    private readonly ISettingsService _settingsService;
    private ThemeManager? _themeManager;
    private LoggingService? _loggingService;
    private ILogger<App>? _logger;

    public IWindow ActiveWindow =>
        _activeWindow switch
        {
            MainWindow mainWindow => mainWindow.ViewModel,
            DetailWindow detailsWindow => detailsWindow.ViewModel,
            _ => throw new NotImplementedException(),
        };

    public App()
    {
        this.InitializeComponent();
        UnhandledException += App_UnhandledException;

        _loggingService = new LoggingService();
        _settingsService = new SettingsService(_loggingService.LoggerFactory.CreateLogger<SettingsService>());
    }

    private void App_UnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
    )
    {
        if (_logger != null)
        {
            Log.UnhandledExceptionOccurred(_logger, e.Exception);
        }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        await _settingsService.LoadAsync();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _themeManager = new ThemeManager(_settingsService, dispatcherQueue);
        _logger = _loggingService!.LoggerFactory.CreateLogger<App>();

        Log.ApplicationStarting(_logger);

        var settings = new SettingsViewModel(_settingsService, this, _loggingService!);
        var app = new AppViewModel(
            () => new ConfirmationDialogService(_themeManager),
            this,
            settings,
            dispatcherQueue,
            _themeManager,
            _loggingService!,
            _settingsService
        );
        app.DetailWindowViewModels.CollectionChanged += OnDetailWindowsCollectionchanged;
        var mainWindow = new MainWindow(app.MainWindow);
        var bar = DispatcherQueue.GetForCurrentThread();
        mainWindow.Closed += OnWindowClosed;
        mainWindow.Activated += OnWindowActivated;
        _windows.Add(mainWindow);

        RegisterWindowForTheming(mainWindow);
        mainWindow.Activate();

        await CheckCliToolsAsync(mainWindow, app.MainWindow);

        Log.ApplicationStartedSuccessfully(_logger);
    }

    private async Task CheckCliToolsAsync(MainWindow mainWindow, WindowViewModel viewModel)
    {
        var settings = viewModel.App.Settings;
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
            XamlRoot = mainWindow.Content.XamlRoot,
            RequestedTheme =
                _themeManager!.GetEffectiveTheme() == ThemeManager.EffectiveTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            viewModel.NavigateToSettings();
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

    private void RegisterWindowForTheming(Window window)
    {
        if (window.Content is FrameworkElement content)
        {
            _themeManager?.RegisterThemeTarget(content);
        }
    }

    private void OnDetailWindowsCollectionchanged(
        object? sender,
        NotifyCollectionChangedEventArgs e
    )
    {
        foreach (var detail in e.NewItems?.Cast<DetailWindowViewModel>() ?? [])
        {
            var window = new DetailWindow(detail);
            window.Closed += OnWindowClosed;
            window.Activated += OnWindowActivated;
            _windows.Add(window);

            RegisterWindowForTheming(window);
            window.Activate();
        }

        // todo do we need this?
        //foreach (var detail in e.OldItems?.Cast<DetailWindowViewModel>() ?? [])
        //{
        //    var window = _windows.FirstOrDefault(w => w is DetailWindow detailWindow && detailWindow.ViewModel == detail);
        //    if (window != null)
        //    {
        //        window.Closed -= OnWindowClosed;
        //        window.Activated -= OnWindowActivated;
        //        _windows.Remove(window);
        //    }
        //}
    }

    private void OnWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if (sender is Window window)
        {
            _activeWindow = window;
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is Window window)
        {
            window.Closed -= OnWindowClosed;
            window.Activated -= OnWindowActivated;
            _windows.Remove(window);

            if (window.Content is FrameworkElement content)
            {
                _themeManager?.UnregisterThemeTarget(content);
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Critical,
            Message = "Unhandled exception occurred"
        )]
        public static partial void UnhandledExceptionOccurred(ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Information,
            Message = "KubeNavigator starting"
        )]
        public static partial void ApplicationStarting(ILogger logger);

        [LoggerMessage(
            EventId = 1003,
            Level = LogLevel.Information,
            Message = "KubeNavigator started successfully"
        )]
        public static partial void ApplicationStartedSuccessfully(ILogger logger);
    }
}
