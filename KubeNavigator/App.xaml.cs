using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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
        var startupTimer = Stopwatch.StartNew();

        var sw = Stopwatch.StartNew();
        await _settingsService.LoadAsync();
        sw.Stop();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _themeManager = new ThemeManager(_settingsService, dispatcherQueue);
        _logger = _loggingService!.LoggerFactory.CreateLogger<App>();

        Log.ApplicationStarting(_logger);
        Log.StartupPhaseCompleted(_logger, "LoadSettings", sw.ElapsedMilliseconds);

        sw.Restart();
        var contextNames = await KubernetesService.LoadContextNamesAsync();
        sw.Stop();
        Log.StartupPhaseCompleted(_logger, "LoadKubeConfig", sw.ElapsedMilliseconds);

        sw.Restart();
        var settings = new SettingsViewModel(_settingsService, this, _loggingService!);
        var app = new AppViewModel(
            () => new ContentDialogService(_themeManager),
            this,
            settings,
            dispatcherQueue,
            _themeManager,
            _loggingService!,
            _settingsService,
            contextNames
        );
        app.DetailWindowViewModels.CollectionChanged += OnDetailWindowsCollectionchanged;
        var mainWindow = new MainWindow(app.MainWindow);
        var bar = DispatcherQueue.GetForCurrentThread();
        mainWindow.Closed += OnWindowClosed;
        mainWindow.Activated += OnWindowActivated;
        _windows.Add(mainWindow);
        sw.Stop();
        Log.StartupPhaseCompleted(_logger, "CreateViewModelsAndWindow", sw.ElapsedMilliseconds);

        sw.Restart();
        RegisterWindowForTheming(mainWindow);
        mainWindow.Activate();
        sw.Stop();
        Log.StartupPhaseCompleted(_logger, "ActivateWindow", sw.ElapsedMilliseconds);

        var kubeConfigWatcher = new KubeConfigWatcher(
            contextNames,
            _loggingService!.LoggerFactory,
            dispatcherQueue
        );
        app.StartWatchingKubeConfig(kubeConfigWatcher);

        //sw.Restart();
        //await CheckCliToolsAsync(mainWindow, app.MainWindow);
        //sw.Stop();
        //Log.StartupPhaseCompleted(_logger, "CheckCliTools", sw.ElapsedMilliseconds);

        startupTimer.Stop();
        Log.StartupTotalTime(_logger, startupTimer.ElapsedMilliseconds);
        Log.ApplicationStartedSuccessfully(_logger);
    }

    private async Task CheckCliToolsAsync(MainWindow mainWindow, WindowViewModel viewModel)
    {
        var settings = viewModel.App.Settings;
        var helmAvailable = await IsToolAvailableAsync(settings.HelmPath, "helm");

        if (helmAvailable)
        {
            return;
        }

        var goToSettings = await viewModel.ContentDialogService.ShowToolsNotFoundDialogAsync(
            "Could not find helm on this system. Some functionality may not work correctly.\n\nYou can configure a custom path in Settings."
        );

        if (goToSettings)
        {
            viewModel.NavigateToSettings();
        }
    }

    private async Task<bool> IsToolAvailableAsync(string customPath, string defaultName)
    {
        var tool = string.IsNullOrWhiteSpace(customPath) ? defaultName : customPath;

        var sw = Stopwatch.StartNew();
        try
        {
            await Cli.Wrap(tool)
                .WithArguments(["version", "--client"])
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();
            sw.Stop();
            Log.CliToolCheckCompleted(_logger!, tool, sw.ElapsedMilliseconds, true);
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.CliToolCheckFailed(_logger!, tool, sw.ElapsedMilliseconds, ex);
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

        [LoggerMessage(
            EventId = 1004,
            Level = LogLevel.Debug,
            Message = "Startup phase '{Phase}' completed in {ElapsedMs}ms"
        )]
        public static partial void StartupPhaseCompleted(ILogger logger, string phase, long elapsedMs);

        [LoggerMessage(
            EventId = 1005,
            Level = LogLevel.Debug,
            Message = "Total startup time: {ElapsedMs}ms"
        )]
        public static partial void StartupTotalTime(ILogger logger, long elapsedMs);

        [LoggerMessage(
            EventId = 1006,
            Level = LogLevel.Debug,
            Message = "CLI tool check for '{Tool}' completed in {ElapsedMs}ms (available: {Available})"
        )]
        public static partial void CliToolCheckCompleted(ILogger logger, string tool, long elapsedMs, bool available);

        [LoggerMessage(
            EventId = 1007,
            Level = LogLevel.Debug,
            Message = "CLI tool check for '{Tool}' failed after {ElapsedMs}ms"
        )]
        public static partial void CliToolCheckFailed(ILogger logger, string tool, long elapsedMs, Exception exception);
    }
}
