using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KubeNavigator.ViewModels;

public partial class NotificationViewModel : ObservableObject
{
    public NotificationViewModel(IWindow window, TimeSpan? dismissAfter)
    {
        Window = window;
        if (dismissAfter != null)
        {
            var timer = Window.App.DispatcherQueue.CreateTimer();
            timer.Tick += (sender, e) => Dismiss();
            timer.Interval = dismissAfter.Value;
            timer.IsRepeating = false;
            timer.Start();
        }
    }

    public IWindow Window { get; }

    [RelayCommand]
    public void Dismiss()
    {
        Window.DismissNotification(this);
    }

    public required string Title { get; set; }
    public required string Message { get; set; }
    public required NotificationSeverity Severity { get; set; }
}
