using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using KubeNavigator.Model;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace KubeNavigator.ViewModels.Shelf;

public partial class PodShellViewModel : ObservableObject, IShelfItem
{
    private PodExecSession? _session;
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<PodShellViewModel> _logger;
    private StreamReader? _reader;
    private string? _lastLine;
    private readonly AsyncManualResetEvent _initialized = new AsyncManualResetEvent(false);

    public PodShellViewModel(PodViewModel pod, ClusterViewModel cluster, ThemeManager themeManager)
    {
        Pod = pod;
        Cluster = cluster;
        ThemeManager = themeManager;
        _cts = new CancellationTokenSource();
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<PodShellViewModel>();
    }

    public event EventHandler<string>? TextReceived;
    public event EventHandler? Closed;

    public PodViewModel Pod { get; }
    public ClusterViewModel Cluster { get; }
    public ThemeManager ThemeManager { get; }

    KubernetesResourceViewModel IShelfItem.Resource => Pod;

    public string Title => $"Pod {Pod.Name}";

    public Task OnCloseAsync()
    {
        Log.ExecSessionClosing(_logger, Pod.Name, Pod.Namespace);
        _cts?.Cancel();
        _session?.Dispose();
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        Log.ExecSessionStarting(_logger, Pod.Name, Pod.Namespace);

        _session = await Cluster.Context.ExecAsync(Pod.Pod, _cts.Token);
        Log.ExecSessionOpened(_logger, Pod.Name, Pod.Namespace);
        _session.Closed += async (s, e) =>
        {
            await Cluster.App.DispatcherQueue.EnqueueAsync(async () =>
            {
                var exitedByUser =
                    _lastLine
                        ?.Replace("\0", string.Empty)
                        .Split(Environment.NewLine)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .LastOrDefault()
                        ?.EndsWith("exit")
                    ?? false;

                if (!exitedByUser)
                {
                    Log.ExecSessionClosedUnexpectedly(_logger, Pod.Name, Pod.Namespace, _lastLine);
                    Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                        "Exec session closed",
                        _lastLine ?? "Unknown error",
                        NotificationSeverity.Error
                    );
                }
                else
                {
                    Log.ExecSessionClosedByUser(_logger, Pod.Name, Pod.Namespace);
                }

                await Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(this);
            });
        };

        _ = Task.Run(
            async () =>
            {
                _reader = new StreamReader(_session.Stream);

                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var buff = new Memory<char>(new char[1024]);
                        var chars = await _reader.ReadAsync(buff);
                        if (chars > 0)
                        {
                            var text = buff.ToString();
                            _lastLine = text;
                            TextReceived?.Invoke(this, text);
                        }
                        else
                        {
                            await Task.Delay(500);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ExecSessionReadFailed(_logger, Pod.Name, Pod.Namespace, e);
                    }
                }

                Log.ExecSessionReadLoopExited(_logger, Pod.Name, Pod.Namespace);
            },
            cancellationToken: _cts.Token
        );

        _initialized.Set();
    }

    public void Write(string text)
    {
        try
        {
            _session!.Stream.Write(Encoding.UTF8.GetBytes(text));
        }
        catch (Exception e)
        {
            Log.ExecSessionWriteFailed(_logger, Pod.Name, Pod.Namespace, e);
        }
    }

    public async Task ResizeAsync(TerminalSize size)
    {
        await _initialized.WaitAsync();
        _session?.Resize(size);
        Log.ExecSessionResized(_logger, Pod.Name, size.Width, size.Height);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 8001,
            Level = LogLevel.Information,
            Message = "Starting exec session for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionStarting(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 8002,
            Level = LogLevel.Information,
            Message = "Exec session opened for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionOpened(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 8003,
            Level = LogLevel.Information,
            Message = "Closing exec session for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionClosing(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 8004,
            Level = LogLevel.Warning,
            Message = "Exec session closed unexpectedly for pod {PodName} in namespace {Namespace}, last output: {LastLine}"
        )]
        public static partial void ExecSessionClosedUnexpectedly(
            ILogger logger,
            string podName,
            string? @namespace,
            string? lastLine
        );

        [LoggerMessage(
            EventId = 8005,
            Level = LogLevel.Debug,
            Message = "Exec session closed by user for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionClosedByUser(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 8006,
            Level = LogLevel.Error,
            Message = "Exec session read failed for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionReadFailed(
            ILogger logger,
            string podName,
            string? @namespace,
            Exception exception
        );

        [LoggerMessage(
            EventId = 8007,
            Level = LogLevel.Debug,
            Message = "Exec session read loop exited for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionReadLoopExited(
            ILogger logger,
            string podName,
            string? @namespace
        );

        [LoggerMessage(
            EventId = 8008,
            Level = LogLevel.Error,
            Message = "Exec session write failed for pod {PodName} in namespace {Namespace}"
        )]
        public static partial void ExecSessionWriteFailed(
            ILogger logger,
            string podName,
            string? @namespace,
            Exception exception
        );

        [LoggerMessage(
            EventId = 8009,
            Level = LogLevel.Debug,
            Message = "Exec session resized for pod {PodName} to {Width}x{Height}"
        )]
        public static partial void ExecSessionResized(
            ILogger logger,
            string podName,
            int width,
            int height
        );
    }
}
