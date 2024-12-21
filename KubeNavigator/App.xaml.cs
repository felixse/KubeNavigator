using KubeNavigator.Model;
using KubeNavigator.ViewModels;
using KubeNavigator.Views;
using KubeNavigator.Windows;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace KubeNavigator;

public partial class App : Application, IWindowManager
{
    private readonly List<Window> _windows = [];
    private Window? _activeWindow;

    public IWindow ActiveWindow => _activeWindow switch
    {
        MainWindow mainWindow => mainWindow.ViewModel,
        DetailWindow detailsWindow => detailsWindow.ViewModel,
        _ => throw new NotImplementedException()
    };

    public App()
    {
        this.InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {

    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        var settings = new SettingsViewModel();
        var app = new AppViewModel(() => new ConfirmationDialogService(), this, settings, dispatcherQueue);
        app.DetailWindowViewModels.CollectionChanged += OnDetailWindowsCollectionchanged;
        var mainWindow = new MainWindow(app.MainWindow);
        var bar = DispatcherQueue.GetForCurrentThread();
        mainWindow.Closed += OnWindowClosed;
        mainWindow.Activated += OnWindowActivated;
        _windows.Add(mainWindow);
        mainWindow.Activate();
    }

    private void OnDetailWindowsCollectionchanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var detail in e.NewItems?.Cast<DetailWindowViewModel>() ?? [])
        {
            var window = new DetailWindow(detail);
            window.Closed += OnWindowClosed;
            window.Activated += OnWindowActivated;
            _windows.Add(window);
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
        }
    }
}
