using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Navigation;

public partial class PinnedNavigationTargetViewModel : ObservableObject, INavigationTarget
{
    public PinnedNavigationTargetViewModel(INavigationTarget navigationTarget, WorkspaceViewModel workspace)
    {
        NavigationTarget = navigationTarget;
        Workspace = workspace;
    }

    public string Title => NavigationTarget.Title;

    public INavigationTarget NavigationTarget { get; }
    public WorkspaceViewModel Workspace { get; }

    [RelayCommand]
    public async Task OpenInNewTab()
    {
        await Workspace.Window.OpenInNewWorkspaceAsync(this, Workspace.Cluster);
    }

    [RelayCommand]
    public void UnPin()
    {
        Workspace.UnPinResourceType(NavigationTarget);
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
