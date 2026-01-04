using KubeNavigator.ViewModels;

namespace KubeNavigator.Models;

public interface IWindowManager
{
    IWindow ActiveWindow { get; }
}
