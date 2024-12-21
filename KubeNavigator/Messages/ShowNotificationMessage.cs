namespace KubeNavigator.Messages;

public enum NotificationSeverity
{
    Success,
    Info,
    Warning,
    Error,
}

public class ShowNotificationMessage
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required NotificationSeverity Severity { get; set; }
}
