using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using KubeNavigator.Messages;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Shelf;

public partial class PodShellViewModel : ObservableObject, IShelfItem
{
    private PodExecSession? _session;
    private readonly CancellationTokenSource _cts;
    private StreamReader? _reader;
    private string? _lastLine;
    private readonly AsyncManualResetEvent _initialized = new AsyncManualResetEvent(false);

    public PodShellViewModel(PodViewModel pod, ClusterViewModel cluster)
    {
        Pod = pod;
        Cluster = cluster;
        _cts = new CancellationTokenSource();
    }

    public event EventHandler<string>? TextReceived;
    public event EventHandler Closed;

    public PodViewModel Pod { get; }
    public ClusterViewModel Cluster { get; }

    KubernetesResourceViewModel IShelfItem.Resource => Pod;

    public string Title => $"Pod {Pod.Name}";

    public Task OnCloseAsync()
    {
        _cts?.Cancel();
        _session?.Dispose();
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        Debug.WriteLine("start");

        //var webSocket = await Cluster.ExecAsync(Pod.Pod, _cts.Token);
        //var demux = new StreamDemuxer(webSocket);
        //demux.Start();
        //var stream = demux.GetStream(ChannelIndex.StdOut, ChannelIndex.StdIn);
        //_stream = stream;


        _session = await Cluster.Context.ExecAsync(Pod.Pod, _cts.Token);
        _session.Closed += async (s, e) =>
        {
            await Cluster.App.DispatcherQueue.EnqueueAsync(async () =>
            {
                var exitedByUser = _lastLine?.Replace("\0", string.Empty).Split(Environment.NewLine).Where(l => !string.IsNullOrWhiteSpace(l)).LastOrDefault()?.EndsWith("exit") ?? false;

                if (!exitedByUser)
                {
                    WeakReferenceMessenger.Default.Send(new ShowNotificationMessage { Message = _lastLine ?? "Unknown error", Title = "Exec Session closed", Severity = NotificationSeverity.Error });
                }

                await Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(this);
            });
        };
        //_writer = new StreamWriter(stream);

        _ = Task.Run(async () =>
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
                    Debug.WriteLine("Read failed: " + e.Message);
                }
            }


            Debug.WriteLine("Read loop exited");
        }, cancellationToken: _cts.Token);

        _initialized.Set();
    }

    public void Write(string text)
    {
        try
        {
            _session.Stream.Write(Encoding.UTF8.GetBytes(text));
        }
        catch (Exception e)
        {
            Debug.WriteLine("Write failed: " + e.Message);
        }
    }

    public async Task ResizeAsync(TerminalSize size)
    {
        await _initialized.WaitAsync();
        _session.Resize(size);
        Debug.WriteLine($"Resized to {size.Width}*{size.Height}");
    }
}
