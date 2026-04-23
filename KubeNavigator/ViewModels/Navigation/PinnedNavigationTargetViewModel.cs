using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Navigation;

public partial class PinnedNavigationTargetViewModel : ObservableObject, INavigationTarget
{
    private readonly ILogger<PinnedNavigationTargetViewModel> _logger;

    public PinnedNavigationTargetViewModel(
        INavigationTarget navigationTarget,
        WorkspaceViewModel workspace
    )
    {
        NavigationTarget = navigationTarget;
        Workspace = workspace;
        _logger =
            workspace.App.LoggingService.LoggerFactory.CreateLogger<PinnedNavigationTargetViewModel>();
    }

    public string Title => NavigationTarget.Title;

    public INavigationTarget NavigationTarget { get; }
    public WorkspaceViewModel Workspace { get; }

    [RelayCommand]
    public async Task OpenInNewTab()
    {
        if (Workspace.Cluster == null)
        {
            Log.ClusterNullOnOpenNewTab(_logger, Title);
            return;
        }

        await Workspace.Window.OpenInNewWorkspaceAsync(this, Workspace.Cluster);
    }

    [RelayCommand]
    public void UnPin()
    {
        Workspace.UnPinNavigationTarget(NavigationTarget);
    }

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 13001,
            Level = LogLevel.Error,
            Message = "Cannot open {NavigationTarget} in new tab: Cluster is null"
        )]
        public static partial void ClusterNullOnOpenNewTab(ILogger logger, string navigationTarget);
    }
}
