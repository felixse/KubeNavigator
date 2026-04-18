using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels;

public partial class ClusterListViewModel : ObservableObject, INavigationTarget
{
    public string Title => "Clusters";

    public ObservableCollection<ClusterViewModel> Clusters { get; set; }
    public WorkspaceViewModel Workspace { get; }

    public ClusterListViewModel(WorkspaceViewModel workspace, PortForwardsViewModel portForwards)
    {
        Workspace = workspace;
        Clusters = workspace.Window.App.Clusters;
    }

    public async Task ConnectAsync(ClusterViewModel cluster)
    {
        try
        {
            var connected = await Workspace.Window.ContentDialogService.ShowConnectingDialogAsync(
                cluster.Name,
                ct => cluster.Context.ConnectAsync(ct)
            );

            if (connected)
            {
                await Workspace.SetContextAsync(cluster);
            }
        }
        catch (Exception e)
        {
            Workspace.Window.ShowMessage(
                "Error",
                $"Failed to connect to cluster: {e.Message}",
                NotificationSeverity.Error
            );
        }
    }

    public async Task ConnectInNewTabAsync(ClusterViewModel cluster)
    {
        var connected = await Workspace.Window.ContentDialogService.ShowConnectingDialogAsync(
            cluster.Name,
            ct => cluster.Context.ConnectAsync(ct)
        );

        if (connected)
        {
            await Workspace.Window.OpenInNewWorkspaceAsync(null, cluster);
        }
    }

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
