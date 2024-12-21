using KubeNavigator.ViewModels;

namespace KubeNavigator.Model;

public interface IWindowManager
{
    IWindow ActiveWindow { get; }
}
