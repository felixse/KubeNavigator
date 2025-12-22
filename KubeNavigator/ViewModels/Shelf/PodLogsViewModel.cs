using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.ViewModels.Resources;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Shelf;

public partial class PodLogsViewModel(PodViewModel pod, ClusterViewModel cluster, ThemeManager themeManager) : ObservableObject, IShelfItem
{
    private CancellationTokenSource? _cts;

    public ThemeManager ThemeManager { get; } = themeManager;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            using var stream = await Cluster.Context.OpenLogStreamAsync(Pod.Pod, _cts.Token);
            using var reader = new StreamReader(stream);

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
        }, cancellationToken: _cts.Token);

    }

    public event EventHandler<string>? LineReceived;
    public event EventHandler Closed;

    public PodViewModel Pod { get; } = pod;
    public ClusterViewModel Cluster { get; } = cluster;

    public string Title => $"{Pod.Name} Logs";

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    KubernetesResourceViewModel IShelfItem.Resource => Pod;

    public Task OnCloseAsync()
    {
        _cts?.Cancel();
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
