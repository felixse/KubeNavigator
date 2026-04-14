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
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels;

public partial class DetailWindowViewModel : ObservableObject, IShelfHost, IWindow
{
    private readonly ILogger<DetailWindowViewModel> _logger;

    public AppViewModel App { get; }

    public DetailsViewModel Details { get; }

    public IContentDialogService ContentDialogService { get; }

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
        IContentDialogService contentDialogService
    )
    {
        App = detailsSource.Cluster.App;
        _logger = App.LoggingService.LoggerFactory.CreateLogger<DetailWindowViewModel>();
        ContentDialogService = contentDialogService;
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
            Log.FilePickerHandlerNotSet(_logger, nameof(DetailWindowViewModel));
            return Task.FromResult<string?>(null);
        }

        return FilePickerHandler(fileTypes);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 4101,
            Level = LogLevel.Error,
            Message = "FilePickerHandler is not set on {ViewModelType}"
        )]
        public static partial void FilePickerHandlerNotSet(ILogger logger, string viewModelType);
    }
}
