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
    public IUserConfirmationService UserConfirmationService { get; }

    public IShelfHost ShelfHost => SelectedWorkspace;

    public Func<IReadOnlyList<string>, Task<string?>>? FilePickerHandler { get; set; }

    public ObservableCollection<WorkspaceViewModel> Workspaces { get; set; }

    public ObservableCollection<NotificationViewModel> Notifications { get; set; }

    [ObservableProperty]
    public partial WorkspaceViewModel SelectedWorkspace { get; set; }

    [ObservableProperty]
    public partial bool IsCommandPanelOpen { get; set; }

    public AppViewModel App { get; }

    public int WorkspacesCount => Workspaces.Count;

    public WindowViewModel(AppViewModel app, IUserConfirmationService userConfirmationService)
    {
        App = app;
        UserConfirmationService = userConfirmationService;

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
                .FirstOrDefault(r => r.Title == navigationTarget.Title);
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

    public Task<string?> PickFileAsync(IReadOnlyList<string> fileTypes)
    {
        if (FilePickerHandler is null)
        {
            Serilog.Log.Error("FilePickerHandler is not set on {ViewModelType}", nameof(WindowViewModel));
            return Task.FromResult<string?>(null);
        }

        return FilePickerHandler(fileTypes);
    }
}
