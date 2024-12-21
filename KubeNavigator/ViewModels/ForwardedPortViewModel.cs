using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s.Models;
using KubeNavigator.ViewModels.Resources;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace KubeNavigator.ViewModels;

public enum ForwardedPortStatus
{
    Active,
    Disabled,
    Error
}

public partial class ForwardedPortViewModel : ObservableObject, ISelectable
{
    private CancellationTokenSource? _cancellationTokenSource;

    public int PodPort { get; set; }

    public int LocalPort { get; set; }

    public string Protocol { get; set; }

    [ObservableProperty]
    public partial ForwardedPortStatus Status { get; set; }

    public ClusterViewModel Cluster { get; }
    public PodViewModel Pod { get; }
    public V1ContainerPort Port { get; }
    public bool IsSelected { get; set; }

    public List<ItemCommand> Commands { get; } = [];

    public ForwardedPortViewModel(ClusterViewModel cluster, PodViewModel pod, V1ContainerPort port, int localPort)
    {
        PodPort = port.ContainerPort;
        Protocol = port.Protocol;
        LocalPort = localPort;
        Cluster = cluster;
        Pod = pod;
        Port = port;
    }

    [RelayCommand]
    public void Start()
    {
        _cancellationTokenSource = Cluster.ForwardContainerPort(Pod.Pod, Port, LocalPort);
        Status = ForwardedPortStatus.Active;
    }

    [RelayCommand]
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        Status = ForwardedPortStatus.Disabled;
    }

    [RelayCommand]
    public void Delete()
    {
        // todo show confirmation dialog
        Cluster.DeleteForwardedPort(this, Pod);
    }

    [RelayCommand]
    public async Task OpenInBrowserAsync()
    {
        await Launcher.LaunchUriAsync(new Uri($"http://localhost:{LocalPort}"));
    }
}
