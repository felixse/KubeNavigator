using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels;

public partial class PortViewModel : ObservableObject
{
    public PortViewModel(
        V1ContainerPort port,
        PodViewModel pod,
        ClusterViewModel cluster,
        ForwardedPortViewModel? forwardedPort
    )
    {
        Name = port.Name;
        HostPort = port.HostPort;
        TargetPort = new PodTargetPort(port.ContainerPort, pod);
        Protocol = port.Protocol;
        Cluster = cluster;
        ForwardedPort = forwardedPort;
        Resource = pod;
    }

    public PortViewModel(
        V1ServicePort port,
        ServiceViewModel service,
        ClusterViewModel cluster,
        ForwardedPortViewModel? forwardedPort
    )
    {
        Name = port.Name;
        //HostPort = port.HostPort; todo
        TargetPort = new ServiceTargetPod(service, port);
        Protocol = port.Protocol;
        Cluster = cluster;
        ForwardedPort = forwardedPort;
        Resource = service;
    }

    public string? Name { get; }

    public int? HostPort { get; }

    public ITargetPort TargetPort { get; }

    public string? Protocol { get; }

    public string Link
    {
        get
        {
            var target = HostPort?.ToString() ?? TargetPort.Value;
            var name = !string.IsNullOrEmpty(Name) ? $"{Name} :" : string.Empty;
            var suffix = ForwardedPort?.LocalPort switch
            {
                null => string.Empty,
                _ => $" → {ForwardedPort.LocalPort}",
            };

            return $"{name}{target}/{Protocol}{suffix}";
        }
    }

    [ObservableProperty]
    public partial ForwardedPortViewModel? ForwardedPort { get; private set; }

    public KubernetesResourceViewModel Resource { get; private set; }

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
        var currentOptions =
            ForwardedPort?.Status == ForwardedPortStatus.Active
                ? new PortForwardOptions { Port = ForwardedPort.LocalPort }
                : null;
        var options =
            await Cluster.App.WindowManager.ActiveWindow.UserConfirmationService.GetPortForwardOptionsAsync(
                Resource,
                currentOptions
            );

        if (options == null)
        {
            return;
        }

        if (ForwardedPort == null)
        {
            var (targetPort, targetResource) = await TargetPort.GetPortAndResourceAsync();

            ForwardedPort = Cluster.CreateForwardedPort(
                Resource,
                targetResource,
                targetPort,
                options.Port,
                Protocol
            );
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
            } while (
                IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpListeners()
                    .Any(x => x.Port == randomPort)
            );

            var (targetPort, targetResource) = await TargetPort.GetPortAndResourceAsync();

            ForwardedPort = Cluster.CreateForwardedPort(
                Resource,
                targetResource,
                targetPort,
                randomPort,
                Protocol
            );
            ForwardedPort.Start();
            await ForwardedPort.OpenInBrowserAsync();
        }

        OnPropertyChanged(nameof(Link));
    }
}
