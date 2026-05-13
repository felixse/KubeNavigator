using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.ViewModels.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace KubeNavigator.ViewModels.Shelf;

public partial class PodLogsViewModel : ObservableObject, IShelfItem
{
    private CancellationTokenSource? _cts;
    private readonly ILogger<PodLogsViewModel> _logger;

    public PodLogsViewModel(PodViewModel pod, ClusterViewModel cluster, ThemeManager themeManager)
    {
        Pod = pod;
        Cluster = cluster;
        ThemeManager = themeManager;
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<PodLogsViewModel>();
    }

    public ThemeManager ThemeManager { get; }

    private const int MaxRetryDelaySeconds = 30;

    public void Start()
    {
        Log.LogStreamStarting(_logger, Pod.Name, Pod.Namespace);
        _cts = new CancellationTokenSource();
        Task.Run(() => StreamWithReconnectAsync(_cts.Token), cancellationToken: _cts.Token);
    }

    private async Task StreamWithReconnectAsync(CancellationToken cancellationToken)
    {
        var retryDelay = TimeSpan.FromSeconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var stream = await Cluster.Context.OpenLogStreamAsync(
                    Pod.Pod,
                    cancellationToken
                );
                using var reader = new StreamReader(stream);
                Log.LogStreamConnected(_logger, Pod.Name, Pod.Namespace);

                retryDelay = TimeSpan.FromSeconds(1); // reset on successful connection

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line != null)
                    {
                        LineReceived?.Invoke(this, line);
                    }
                    else
                    {
                        // End of stream — break to reconnect
                        Log.LogStreamDisconnected(_logger, Pod.Name, Pod.Namespace);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.LogStreamCancelled(_logger, Pod.Name, Pod.Namespace);
                return;
            }
            catch (Exception ex)
            {
                Log.LogStreamFailed(_logger, Pod.Name, Pod.Namespace, ex);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Log.LogStreamReconnecting(_logger, Pod.Name, Pod.Namespace, (int)retryDelay.TotalSeconds);
            try
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            retryDelay = TimeSpan.FromSeconds(
                Math.Min(retryDelay.TotalSeconds * 2, MaxRetryDelaySeconds)
            );
        }
    }

    public event EventHandler<string>? LineReceived;
    public event EventHandler? Closed;

    public PodViewModel Pod { get; }
    public ClusterViewModel Cluster { get; }

    public string Title => $"{Pod.Name} Logs";

    [RelayCommand]
    public async Task SaveLogsAsync()
    {
        var path = await Cluster.App.WindowManager.ActiveWindow.SaveFileAsync(
            $"{Pod.Name}.log",
            [".log", ".txt"]
        );
        if (path != null)
        {
            var logs = await Cluster.Context.ReadPodLogsAsync(Pod.Pod, CancellationToken.None);
            await System.IO.File.WriteAllTextAsync(path, logs);

            var directory = Path.GetDirectoryName(path)!;
            var notification = new AppNotificationBuilder()
                .AddText("Logs downloaded")
                .AddText("Click to open the destination folder.")
                .AddArgument("action", "openFolder")
                .AddArgument("path", directory)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    KubernetesResourceViewModel IShelfItem.Resource => Pod;

    public Task OnCloseAsync()
    {
        Log.LogStreamClosing(_logger, Pod.Name, Pod.Namespace);
        _cts?.Cancel();
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 9001,
            Level = LogLevel.Information,
            Message = "Starting log stream for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void LogStreamStarting(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 9002,
            Level = LogLevel.Debug,
            Message = "Log stream connected for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void LogStreamConnected(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 9003,
            Level = LogLevel.Information,
            Message = "Closing log stream for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void LogStreamClosing(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 9004,
            Level = LogLevel.Debug,
            Message = "Log stream cancelled for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void LogStreamCancelled(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 9005,
            Level = LogLevel.Error,
            Message = "Log stream failed for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void LogStreamFailed(
            ILogger logger,
            string podName,
            string? @namespace,
            Exception exception
        );

        [LoggerMessage(
            EventId = 9006,
            Level = LogLevel.Warning,
            Message = "Log stream disconnected for pod {PodName} in namespace {Namespace}, end of stream reached"
        )]
        public static partial void LogStreamDisconnected(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 9007,
            Level = LogLevel.Information,
            Message = "Reconnecting log stream for pod {PodName} in namespace {Namespace} in {DelaySeconds}s"
        )]
        public static partial void LogStreamReconnecting(
            ILogger logger,
            string podName,
            string? @namespace,
            int delaySeconds
        );
    }
}
