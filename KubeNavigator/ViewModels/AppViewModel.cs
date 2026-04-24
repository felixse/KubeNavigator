using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Models;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.Details;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace KubeNavigator.ViewModels;

public partial class AppViewModel : ObservableObject
{
    private readonly Func<IContentDialogService> _contentDialogServiceFactory;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AppViewModel> _logger;

    public ObservableCollection<DetailWindowViewModel> DetailWindowViewModels { get; } = [];

    public ObservableCollection<ClusterViewModel> Clusters { get; set; }

    public ObservableCollection<ForwardedPortViewModel> ForwardedPorts { get; } = [];

    public WindowViewModel MainWindow { get; }

    public IWindowManager WindowManager { get; }

    public SettingsViewModel Settings { get; }

    public DispatcherQueue DispatcherQueue { get; }

    public ThemeManager ThemeManager { get; }

    public LoggingService LoggingService { get; }

    public ViewStateService ViewStateService { get; }

    public HelmService HelmService { get; }

    public AppViewModel(
        Func<IContentDialogService> contentDialogServiceFactory,
        IWindowManager windowManager,
        SettingsViewModel settings,
        DispatcherQueue dispatcherQueue,
        ThemeManager themeManager,
        LoggingService loggingService,
        ISettingsService settingsService,
        ViewStateService viewStateService,
        IReadOnlyList<string> contextNames
    )
    {
        _contentDialogServiceFactory = contentDialogServiceFactory;
        _logger = loggingService.LoggerFactory.CreateLogger<AppViewModel>();
        WindowManager = windowManager;
        Settings = settings;
        DispatcherQueue = dispatcherQueue;
        ThemeManager = themeManager;
        LoggingService = loggingService;
        _settingsService = settingsService;
        ViewStateService = viewStateService;
        HelmService = new HelmService(
            LoggerFactoryExtensions.CreateLogger<HelmService>(loggingService.LoggerFactory)
        );

        Clusters =
        [
            .. contextNames.Select(name => new ClusterViewModel(
                name,
                this,
                new KubernetesContext(name, loggingService.LoggerFactory, settingsService)
            )),
        ];

        MainWindow = new WindowViewModel(this, _contentDialogServiceFactory());
    }

    public void StartWatchingKubeConfig(KubeConfigWatcher watcher)
    {
        watcher.ContextAdded += OnContextAdded;
        watcher.ContextRemoved += OnContextRemoved;
        watcher.ContextsChanged += OnContextsChanged;
    }

    private void OnContextAdded(string contextName)
    {
        if (Clusters.Any(c => c.Name == contextName))
        {
            return;
        }

        var cluster = new ClusterViewModel(
            contextName,
            this,
            new KubernetesContext(contextName, LoggingService.LoggerFactory, _settingsService)
        );
        Clusters.Add(cluster);

        Log.KubeConfigContextAdded(_logger, contextName);
    }

    private async void OnContextRemoved(string contextName)
    {
        var cluster = Clusters.FirstOrDefault(c => c.Name == contextName);
        if (cluster is null)
        {
            return;
        }

        Log.KubeConfigContextRemoved(_logger, contextName);

        var affectedWorkspaces = GetWorkspacesForCluster(cluster);

        Clusters.Remove(cluster);

        foreach (var workspace in affectedWorkspaces)
        {
            await workspace.HandleClusterRemovedAsync(contextName);
        }
    }

    private async void OnContextsChanged(IReadOnlyList<string> contextNames)
    {
        var affectedWorkspaces =
            new List<(WorkspaceViewModel Workspace, ClusterViewModel Cluster)>();

        foreach (var name in contextNames)
        {
            var cluster = Clusters.FirstOrDefault(c => c.Name == name);
            if (cluster is null || cluster.Context.Status.Status != ConnectionStatus.Connected)
            {
                continue;
            }

            var workspaces = GetWorkspacesForCluster(cluster);
            foreach (var ws in workspaces)
            {
                affectedWorkspaces.Add((ws, cluster));
            }
        }

        if (affectedWorkspaces.Count == 0)
        {
            return;
        }

        var clusterNames = string.Join(
            ", ",
            affectedWorkspaces.Select(a => a.Cluster.Name).Distinct()
        );
        Log.KubeConfigContextModified(_logger, clusterNames);

        foreach (var (workspace, cluster) in affectedWorkspaces)
        {
            await workspace.HandleClusterChangedAsync(cluster);
        }
    }

    private List<WorkspaceViewModel> GetWorkspacesForCluster(ClusterViewModel cluster)
    {
        var workspaces = new List<WorkspaceViewModel>();

        foreach (var workspace in MainWindow.Workspaces)
        {
            if (workspace.Cluster == cluster)
            {
                workspaces.Add(workspace);
            }
        }

        return workspaces;
    }

    [RelayCommand]
    public void CreateDetailsWindow(IDetailsSource detailsSource)
    {
        var detailsWindow = new DetailWindowViewModel(
            detailsSource,
            _contentDialogServiceFactory()
        );
        DetailWindowViewModels.Add(detailsWindow);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 4001,
            Level = LogLevel.Information,
            Message = "KubeConfig context added: {ContextName}"
        )]
        public static partial void KubeConfigContextAdded(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 4002,
            Level = LogLevel.Information,
            Message = "KubeConfig context removed: {ContextName}"
        )]
        public static partial void KubeConfigContextRemoved(ILogger logger, string contextName);

        [LoggerMessage(
            EventId = 4003,
            Level = LogLevel.Information,
            Message = "KubeConfig context modified for connected clusters: {ClusterNames}"
        )]
        public static partial void KubeConfigContextModified(ILogger logger, string clusterNames);
    }
}
