using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.KubeConfigModels;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace KubeNavigator.ViewModels;

public partial class AppViewModel : ObservableObject
{
    private readonly Func<IUserConfirmationService> _userConfirmationServiceFactory;

    public ObservableCollection<DetailWindowViewModel> DetailWindowViewModels { get; } = [];

    public ObservableCollection<ClusterViewModel> Clusters { get; set; }

    public ObservableCollection<ForwardedPortViewModel> ForwardedPorts { get; } = [];

    public WindowViewModel MainWindow { get; }

    public IWindowManager WindowManager { get; }

    public SettingsViewModel Settings { get; }

    public DispatcherQueue DispatcherQueue { get; }

    public ThemeManager ThemeManager { get; }

    public AppViewModel(Func<IUserConfirmationService> userConfirmationServiceFactory, IWindowManager windowManager, SettingsViewModel settings, DispatcherQueue dispatcherQueue, ThemeManager themeManager)
    {
        _userConfirmationServiceFactory = userConfirmationServiceFactory;
        WindowManager = windowManager;
        Settings = settings;
        DispatcherQueue = dispatcherQueue;
        ThemeManager = themeManager;
        var configContent = System.IO.File.ReadAllText(KubernetesClientConfiguration.KubeConfigDefaultLocation);
        var config = KubernetesYaml.Deserialize<K8SConfiguration>(configContent); // todo move to service, make singleton

        Clusters = [.. config.Contexts.Select(c => new ClusterViewModel(c.Name, this, new KubernetesContext(c.Name)))];

        MainWindow = new WindowViewModel(this, _userConfirmationServiceFactory());
    }

    [RelayCommand]
    public void CreateDetailsWindow(KubernetesResourceViewModel resource)
    {
        var detailsWindow = new DetailWindowViewModel(resource, _userConfirmationServiceFactory());
        DetailWindowViewModels.Add(detailsWindow);
    }
}
