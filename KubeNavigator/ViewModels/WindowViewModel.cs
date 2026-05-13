using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels;

public enum NotificationSeverity
{
    Success,
    Info,
    Warning,
    Error,
}

public partial class WindowViewModel : ObservableObject, IWindow
{
    private readonly ILogger<WindowViewModel> _logger;

    public IContentDialogService ContentDialogService { get; }

    public IShelfHost ShelfHost => SelectedWorkspace;

    public string Title
    {
        get
        {
            var clusterName = SelectedWorkspace?.Cluster?.Name;
            var itemTitle = SelectedWorkspace?.SelectedItem?.Title;

            return (clusterName, itemTitle) switch
            {
                (not null, not null) => $"{itemTitle} [{clusterName}] - KubeNavigator",
                _ => "KubeNavigator",
            };
        }
    }

    public Func<IReadOnlyList<string>, Task<string?>>? FilePickerHandler { get; set; }

    public Func<string, IReadOnlyList<string>, Task<string?>>? FileSaverHandler { get; set; }

    public ObservableCollection<WorkspaceViewModel> Workspaces { get; set; }

    public ObservableCollection<NotificationViewModel> Notifications { get; set; }

    [ObservableProperty]
    public partial WorkspaceViewModel SelectedWorkspace { get; set; }

    [ObservableProperty]
    public partial bool IsCommandPanelOpen { get; set; }

    public AppViewModel App { get; }

    public int WorkspacesCount => Workspaces.Count;

    public WindowViewModel(AppViewModel app, IContentDialogService contentDialogService)
    {
        App = app;
        _logger = app.LoggingService.LoggerFactory.CreateLogger<WindowViewModel>();
        ContentDialogService = contentDialogService;

        Notifications = [];
        Workspaces = [new WorkspaceViewModel(this)];
        Workspaces.CollectionChanged += OnTabsCollectionChanged;

        SelectedWorkspace = Workspaces[0];
    }

    [RelayCommand]
    public void OpenCommandPanel()
    {
        SelectedWorkspace.CommandText = string.Empty;
        IsCommandPanelOpen = true;
    }

    [RelayCommand]
    public void AddTab()
    {
        var workspace = new WorkspaceViewModel(this);
        Workspaces.Add(workspace);
        SelectedWorkspace = workspace;
    }

    public void DismissNotification(NotificationViewModel notification)
    {
        Notifications.Remove(notification);
    }

    private void OnTabsCollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e
    )
    {
        OnPropertyChanged(nameof(WorkspacesCount));
    }

    public async Task OpenInNewWorkspaceAsync(
        INavigationTarget? navigationTarget,
        ClusterViewModel cluster
    )
    {
        var workspace = new WorkspaceViewModel(this);
        await workspace.SetContextAsync(cluster);

        Workspaces.Add(workspace);
        SelectedWorkspace = workspace;
        if (navigationTarget != null)
        {
            workspace.SelectedItem = workspace
                .NavigationGroups.SelectMany(c => c.Items)
                .SelectMany(item =>
                    item is CustomResourceGroupViewModel crg
                        ? crg.Resources.Cast<INavigationTarget>()
                        : [item]
                )
                .FirstOrDefault(r =>
                    r is KubernetesResourceTypeListViewModel rl
                    && navigationTarget is KubernetesResourceTypeListViewModel sourceRl
                        ? rl.ResourceType == sourceRl.ResourceType
                        : r.Title == navigationTarget.Title);
        }
    }

    public void NavigateToSettings()
    {
        SelectedWorkspace.SelectedItem = App.Settings;
    }

    public void ShowMessage(string title, string message, NotificationSeverity severity)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            Notifications.Add(
                new NotificationViewModel(
                    this,
                    dismissAfter: severity == NotificationSeverity.Success
                        ? TimeSpan.FromSeconds(5)
                        : null
                )
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                }
            );
        });
    }

    async partial void OnSelectedWorkspaceChanged(
        WorkspaceViewModel? oldValue,
        WorkspaceViewModel value
    )
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnWorkspacePropertyChanged;
        }

        value.PropertyChanged += OnWorkspacePropertyChanged;
        OnPropertyChanged(nameof(Title));
        await value.ShowPendingDialogAsync();
    }

    private void OnWorkspacePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (
            e.PropertyName
            is nameof(WorkspaceViewModel.Cluster)
                or nameof(WorkspaceViewModel.SelectedItem)
        )
        {
            OnPropertyChanged(nameof(Title));
        }
    }

    public Task<string?> PickFileAsync(IReadOnlyList<string> fileTypes)
    {
        if (FilePickerHandler is null)
        {
            Log.FilePickerHandlerNotSet(_logger, nameof(WindowViewModel));
            return Task.FromResult<string?>(null);
        }

        return FilePickerHandler(fileTypes);
    }

    public Task<string?> SaveFileAsync(string suggestedFileName, IReadOnlyList<string> fileTypes)
    {
        if (FileSaverHandler is null)
        {
            Log.FilePickerHandlerNotSet(_logger, nameof(WindowViewModel));
            return Task.FromResult<string?>(null);
        }

        return FileSaverHandler(suggestedFileName, fileTypes);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 4001,
            Level = LogLevel.Error,
            Message = "FilePickerHandler is not set on {ViewModelType}"
        )]
        public static partial void FilePickerHandlerNotSet(ILogger logger, string viewModelType);
    }
}
