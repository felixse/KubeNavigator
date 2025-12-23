using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KubeNavigator.Messages;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Navigation;
using KubeNavigator.ViewModels.Shelf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class WindowViewModel : ObservableRecipient,
    IRecipient<ShowNotificationMessage>, IWindow
{
    public IUserConfirmationService UserConfirmationService { get; }

    public IShelfHost ShelfHost => SelectedWorkspace;

    public ObservableCollection<WorkspaceViewModel> Workspaces { get; set; }

    public ObservableCollection<NotificationViewModel> Notifications { get; set; }

    [ObservableProperty]
    public partial WorkspaceViewModel SelectedWorkspace { get; set; }

    public AppViewModel App { get; }

    public int WorkspacesCount => Workspaces.Count;

    public WindowViewModel(AppViewModel app, IUserConfirmationService userConfirmationService)
    {
        App = app;
        UserConfirmationService = userConfirmationService;
        Messenger.RegisterAll(this);

        Notifications = [];
        Workspaces = [new WorkspaceViewModel(this)];
        Workspaces.CollectionChanged += OnTabsCollectionChanged;

        SelectedWorkspace = Workspaces[0];
    }

    public void DismissNotification(NotificationViewModel notification)
    {
        Notifications.Remove(notification);
    }

    private void OnTabsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(WorkspacesCount));
    }

    partial void OnSelectedWorkspaceChanged(WorkspaceViewModel? oldValue, WorkspaceViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.IsActive = false;
        }

        if (newValue != null)
        {
            newValue.IsActive = true;
        }
    }

    public async Task OpenInNewWorkspaceAsync(INavigationTarget? navigationTarget, ClusterViewModel cluster)
    {
        var workspace = new WorkspaceViewModel(this);
        await workspace.SetContextAsync(cluster);

        Workspaces.Add(workspace);
        SelectedWorkspace = workspace;
        if (navigationTarget != null)
        {
            workspace.SelectedItem = workspace.NavigationGroups.SelectMany(c => c.Items).FirstOrDefault(r => r.Title == navigationTarget.Title);
        }
    }

    void IRecipient<ShowNotificationMessage>.Receive(ShowNotificationMessage message)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            Notifications.Add(new NotificationViewModel(this, dismissAfter: message.Severity == NotificationSeverity.Success ? TimeSpan.FromSeconds(5) : null)
            {
                Title = message.Title,
                Message = message.Message,
                Severity = message.Severity,
            });
        });
    }
}
