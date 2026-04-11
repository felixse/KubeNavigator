using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.ViewModels.Resources;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Shelf;

public partial class PodLogsViewModel : ObservableObject, IShelfItem
{
    private CancellationTokenSource? _cts;
    private readonly ILogger<PodLogsViewModel> _logger;

    public PodLogsViewModel(
        PodViewModel pod,
        ClusterViewModel cluster,
        ThemeManager themeManager
    )
    {
        Pod = pod;
        Cluster = cluster;
        ThemeManager = themeManager;
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<PodLogsViewModel>();
    }

    public ThemeManager ThemeManager { get; }

    public void Start()
    {
        Log.LogStreamStarting(_logger, Pod.Name, Pod.Namespace);
        _cts = new CancellationTokenSource();
        Task.Run(
            async () =>
            {
                try
                {
                    using var stream = await Cluster.Context.OpenLogStreamAsync(Pod.Pod, _cts.Token);
                    using var reader = new StreamReader(stream);
                    Log.LogStreamConnected(_logger, Pod.Name, Pod.Namespace);

                    while (!_cts.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(cancellationToken: _cts.Token);
                        if (line != null)
                        {
                            LineReceived?.Invoke(this, line);
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.LogStreamCancelled(_logger, Pod.Name, Pod.Namespace);
                }
                catch (Exception ex)
                {
                    Log.LogStreamFailed(_logger, Pod.Name, Pod.Namespace, ex);
                }
            },
            cancellationToken: _cts.Token
        );
    }

    public event EventHandler<string>? LineReceived;
    public event EventHandler? Closed;

    public PodViewModel Pod { get; }
    public ClusterViewModel Cluster { get; }

    public string Title => $"{Pod.Name} Logs";

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
        [LoggerMessage(EventId = 9001, Level = LogLevel.Information, Message = "Starting log stream for pod {PodName} in namespace {Namespace}")]
        public static partial void LogStreamStarting(ILogger logger, string podName, string? @namespace);

        [LoggerMessage(EventId = 9002, Level = LogLevel.Debug, Message = "Log stream connected for pod {PodName} in namespace {Namespace}")]
        public static partial void LogStreamConnected(ILogger logger, string podName, string? @namespace);

        [LoggerMessage(EventId = 9003, Level = LogLevel.Information, Message = "Closing log stream for pod {PodName} in namespace {Namespace}")]
        public static partial void LogStreamClosing(ILogger logger, string podName, string? @namespace);

        [LoggerMessage(EventId = 9004, Level = LogLevel.Debug, Message = "Log stream cancelled for pod {PodName} in namespace {Namespace}")]
        public static partial void LogStreamCancelled(ILogger logger, string podName, string? @namespace);

        [LoggerMessage(EventId = 9005, Level = LogLevel.Error, Message = "Log stream failed for pod {PodName} in namespace {Namespace}")]
        public static partial void LogStreamFailed(ILogger logger, string podName, string? @namespace, Exception exception);
    }
}
