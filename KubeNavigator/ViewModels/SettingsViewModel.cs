using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.ViewModels.Navigation;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class SettingsViewModel : ObservableObject, INavigationTarget
{
    public string Title => "Settings";

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
