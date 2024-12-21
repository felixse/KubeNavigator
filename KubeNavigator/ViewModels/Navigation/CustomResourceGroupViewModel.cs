using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Navigation;
public partial class CustomResourceGroupViewModel : ObservableObject, INavigationTarget
{
    public CustomResourceGroupViewModel(string groupName)
    {
        GroupName = groupName;
    }

    public string GroupName { get; }

    public ObservableCollection<KubernetesResourceTypeListViewModel> Resources { get; } = [];

    string INavigationTarget.Title => GroupName;

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
