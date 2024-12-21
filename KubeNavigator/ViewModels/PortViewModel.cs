using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;
public partial class PortViewModel : ObservableObject
{
    public PortViewModel(V1ContainerPort port, PodViewModel pod, ClusterViewModel cluster, ForwardedPortViewModel? forwardedPort)
    {
        Name = port.Name;
        HostPort = port.HostPort;
        ContainerPort = port.ContainerPort;
        Protocol = port.Protocol;
        Port = port;
        Pod = pod;
        Cluster = cluster;
        ForwardedPort = forwardedPort;
    }

    public string? Name { get; }

    public int? HostPort { get; }

    public int ContainerPort { get; }

    public string? Protocol { get; }

    public string Link
    {
        get
        {
            var target = HostPort ?? ContainerPort;
            var name = !string.IsNullOrEmpty(Name) ? $"{Name} :" : string.Empty;
            var suffix = ForwardedPort?.LocalPort switch
            {
                null => string.Empty,
                _ => $" → {ForwardedPort.LocalPort}"
            };

            return $"{name}{target}/{Protocol}{suffix}";
        }
    }

    [ObservableProperty]
    public partial ForwardedPortViewModel? ForwardedPort { get; private set; }
    public V1ContainerPort Port { get; }
    public PodViewModel Pod { get; }
    public ClusterViewModel Cluster { get; }

    [RelayCommand]
    public void Stop()
    {
        ForwardedPort?.Stop();
        OnPropertyChanged(nameof(Link));
    }

    [RelayCommand]
    public void Start()
    {
        ForwardedPort?.Start();
        OnPropertyChanged(nameof(Link));
    }

    [RelayCommand]
    public void DeleteForwardedPort()
    {
        ForwardedPort?.Delete();
        ForwardedPort = null;
        OnPropertyChanged(nameof(Link));
    }

    [RelayCommand]
    public async Task ShowForwardingDialogAsync()
    {
        var currentOptions = ForwardedPort?.Status == ForwardedPortStatus.Active ? new PortForwardOptions { Port = ForwardedPort.LocalPort } : null;
        var options = await Cluster.App.WindowManager.ActiveWindow.UserConfirmationService.GetPortForwardOptionsAsync(Pod, currentOptions);

        if (options == null)
        {
            return;
        }

        if (ForwardedPort == null)
        {
            ForwardedPort = Cluster.CreateForwardedPort(Pod, Port, options.Port);
        }
        else
        {
            ForwardedPort.Stop();
            ForwardedPort.LocalPort = options.Port;
        }

        ForwardedPort.Start();

        if (options.OpenInBrowser)
        {
            await ForwardedPort.OpenInBrowserAsync();
        }

        OnPropertyChanged(nameof(Link));
    }

    [RelayCommand]
    public async Task InvokeAsync()
    {
        if (ForwardedPort != null)
        {
            if (ForwardedPort.Status != ForwardedPortStatus.Active)
            {
                ForwardedPort.Start();

            }

            await ForwardedPort.OpenInBrowserAsync();
        }
        else
        {
            var randomPort = 0;
            do
            {
                randomPort = new Random().Next(49152, 65535);
            } while (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == randomPort));

            ForwardedPort = Cluster.CreateForwardedPort(Pod, Port, randomPort);
            ForwardedPort.Start();
            await ForwardedPort.OpenInBrowserAsync();
        }

        OnPropertyChanged(nameof(Link));
    }
}
