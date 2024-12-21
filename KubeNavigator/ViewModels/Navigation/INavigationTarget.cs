using System.ComponentModel;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Navigation;

public interface INavigationTarget : INotifyPropertyChanged
{
    string Title { get; }

    Task OnNavigatedTo();

    Task OnNavigatedFrom();
}
