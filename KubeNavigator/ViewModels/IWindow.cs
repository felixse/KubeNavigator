using KubeNavigator.Model;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels;

public interface IWindow
{
    IUserConfirmationService UserConfirmationService { get; }

    IShelfHost ShelfHost { get; }
}
