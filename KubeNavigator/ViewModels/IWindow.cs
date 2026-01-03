using KubeNavigator.Model;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels;

public interface IWindow
{
    IUserConfirmationService UserConfirmationService { get; }

    IShelfHost? ShelfHost { get; }

    AppViewModel App { get; }

    void ShowMessage(string title, string message, NotificationSeverity severity);

    void DismissNotification(NotificationViewModel notification);
}
