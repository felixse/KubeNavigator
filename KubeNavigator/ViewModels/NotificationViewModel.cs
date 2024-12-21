using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Messages;
using System;

namespace KubeNavigator.ViewModels;

public partial class NotificationViewModel : ObservableObject
{
    public NotificationViewModel(WindowViewModel window, TimeSpan? dismissAfter)
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

    public WindowViewModel Window { get; }

    [RelayCommand]
    public void Dismiss()
    {
        Window.DismissNotification(this);
    }

    public required string Title { get; set; }
    public required string Message { get; set; }
    public required NotificationSeverity Severity { get; set; }
}
