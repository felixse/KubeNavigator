using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;
using Windows.System;

namespace KubeNavigator.ViewModels;

public enum ForwardedPortStatus
{
    Active,
    Disabled,
    Error,
}

public partial class ForwardedPortViewModel : ObservableObject, ISelectable
{
    private CancellationTokenSource? _cancellationTokenSource;

    public int TargetPort { get; set; }

    public int LocalPort { get; set; }

    public string DisplayText => $"localhost:{LocalPort} → {Resource.Name}:{TargetPort}";

    public string? Protocol { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    public partial ForwardedPortStatus Status { get; set; }

    public bool IsActive => Status == ForwardedPortStatus.Active;

    public static Microsoft.UI.Xaml.Visibility Active(bool isActive) =>
        isActive ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public static Microsoft.UI.Xaml.Visibility NotActive(bool isActive) =>
        isActive ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;

    public ClusterViewModel Cluster { get; }
    public KubernetesResourceViewModel Resource { get; }
    public IKubernetesObject<V1ObjectMeta> TargetResource { get; }
    public bool IsSelected { get; set; }

    public List<ItemCommand> Commands { get; } = [];

    public ForwardedPortViewModel(
        ClusterViewModel cluster,
        KubernetesResourceViewModel resource,
        IKubernetesObject<V1ObjectMeta> targetResource,
        int localPort,
        int targetPort,
        string? protocol
    )
    {
        TargetPort = targetPort;
        Protocol = protocol;
        LocalPort = localPort;
        Cluster = cluster;
        Resource = resource;
        TargetResource = targetResource;
    }

    [RelayCommand]
    public void Start()
    {
        _cancellationTokenSource = Cluster.ForwardContainerPort(
            TargetResource,
            TargetPort,
            LocalPort
        );
        Status = ForwardedPortStatus.Active;
    }

    [RelayCommand]
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        Status = ForwardedPortStatus.Disabled;
    }

    [RelayCommand]
    public async Task EditAsync()
    {
        var currentOptions = new PortForwardOptions { Port = LocalPort };
        var options =
            await Cluster.App.WindowManager.ActiveWindow.ContentDialogService.GetPortForwardOptionsAsync(
                Resource,
                currentOptions
            );

        if (options == null)
        {
            return;
        }

        Stop();
        LocalPort = options.Port;
        OnPropertyChanged(nameof(DisplayText));
        Start();

        if (options.OpenInBrowser)
        {
            await OpenInBrowserAsync();
        }
    }

    [RelayCommand]
    public void Delete()
    {
        // todo show confirmation dialog
        Cluster.DeleteForwardedPort(this, Resource);
    }

    [RelayCommand]
    public async Task OpenInBrowserAsync()
    {
        await Launcher.LaunchUriAsync(new Uri($"http://localhost:{LocalPort}"));
    }
}
