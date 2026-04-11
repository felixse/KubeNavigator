using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels;

public partial class DetailWindowViewModel : ObservableObject, IShelfHost, IWindow
{
    public AppViewModel App { get; }

    public DetailsViewModel Details { get; }

    public IUserConfirmationService UserConfirmationService { get; }

    public IShelfHost ShelfHost => this;

    public Func<IReadOnlyList<string>, Task<string?>>? FilePickerHandler { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    public ObservableCollection<IShelfItem> ShelfItems { get; } = [];

    [ObservableProperty]
    public partial IShelfItem? SelectedShelfItem { get; set; }

    public ObservableCollection<NotificationViewModel> Notifications { get; set; } = [];

    public DetailWindowViewModel(
        IDetailsSource detailsSource,
        IUserConfirmationService userConfirmationService
    )
    {
        App = detailsSource.Cluster.App;
        Details = new DetailsViewModel(detailsSource, this);

        Details.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DetailsViewModel.SelectedItem))
            {
                Title = detailsSource.WindowTitle;
            }
        };
        Title = detailsSource.WindowTitle;
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

    public Task<string?> PickFileAsync(IReadOnlyList<string> fileTypes)
    {
        if (FilePickerHandler is null)
        {
            Serilog.Log.Error("FilePickerHandler is not set on {ViewModelType}", nameof(DetailWindowViewModel));
            return Task.FromResult<string?>(null);
        }

        return FilePickerHandler(fileTypes);
    }
}
