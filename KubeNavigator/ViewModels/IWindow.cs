using System.Collections.Generic;
using System.Threading.Tasks;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels;

public interface IWindow
{
    IContentDialogService ContentDialogService { get; }

    IShelfHost ShelfHost { get; }

    AppViewModel App { get; }

    void ShowMessage(string title, string message, NotificationSeverity severity);

    void DismissNotification(NotificationViewModel notification);

    Task<string?> PickFileAsync(IReadOnlyList<string> fileTypes);

    Task<string?> SaveFileAsync(string suggestedFileName, IReadOnlyList<string> fileTypes);
}
