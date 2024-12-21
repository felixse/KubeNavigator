using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KubeNavigator.Messages;
using KubeNavigator.ViewModels.Navigation;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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
            await cluster.Context.ConnectAsync();

            Workspace.SetContextAsync(cluster);
        }
        catch (System.Exception e)
        {
            WeakReferenceMessenger.Default.Send(new ShowNotificationMessage { Message = $"Failed to connect to cluster: {e.Message}", Title = "Error", Severity = NotificationSeverity.Error });
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
