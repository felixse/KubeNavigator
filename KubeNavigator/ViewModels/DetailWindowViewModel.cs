using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels;

public partial class DetailWindowViewModel : ObservableObject, IShelfHost, IWindow
{
    public AppViewModel App { get; }

    public DetailsViewModel Details { get; }

    public IUserConfirmationService UserConfirmationService { get; }

    public IShelfHost ShelfHost => this;

    [ObservableProperty]
    public partial string Title { get; set; }

    public ObservableCollection<IShelfItem> ShelfItems { get; } = [];

    [ObservableProperty]
    public partial IShelfItem? SelectedShelfItem { get; set; }

    public ObservableCollection<NotificationViewModel> Notifications { get; set; } = [];

    public DetailWindowViewModel(
        KubernetesResourceViewModel resource,
        IUserConfirmationService userConfirmationService
    )
    {
        App = resource.Cluster.App;
        Details = new DetailsViewModel(resource, this);

        Details.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DetailsViewModel.SelectedResource))
            {
                Title =
                    $"{Details.SelectedResource.ResourceType.SingularDisplayName}: {Details.SelectedResource.Name}";
            }
        };
        Title =
            $"{Details.SelectedResource.ResourceType.SingularDisplayName}: {Details.SelectedResource.Name}";
        UserConfirmationService = userConfirmationService;
    }

    public async Task CloseShelfItemAsync(IShelfItem item)
    {
        if (ShelfItems.Remove(item))
        {
            await item.OnCloseAsync();
        }
    }

    public void OpenShelfItem(IShelfItem item)
    {
        var existing = ShelfItems.FirstOrDefault(t =>
            t.Resource == item.Resource && item.GetType() == t.GetType()
        );
        if (existing != null)
        {
            SelectedShelfItem = existing;
            return;
        }

        ShelfItems.Add(item);
        SelectedShelfItem = item;
    }

    public void ShowMessage(string title, string message, NotificationSeverity severity)
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            Notifications.Add(
                new NotificationViewModel(
                    this,
                    dismissAfter: severity == NotificationSeverity.Success
                        ? TimeSpan.FromSeconds(5)
                        : null
                )
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                }
            );
        });
    }

    public void DismissNotification(NotificationViewModel notification)
    {
        Notifications.Remove(notification);
    }
}
