using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels;

public partial class TodoViewModel : ObservableObject, INavigationTarget
{
    public TodoViewModel(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task OpenInNewTab() { }
}
