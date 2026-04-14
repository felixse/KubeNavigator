using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.KubeConfigModels;
using KubeNavigator.Models;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace KubeNavigator.ViewModels;

public partial class AppViewModel : ObservableObject
{
    private readonly Func<IContentDialogService> _contentDialogServiceFactory;
    private readonly ISettingsService _settingsService;

    public ObservableCollection<DetailWindowViewModel> DetailWindowViewModels { get; } = [];

    public ObservableCollection<ClusterViewModel> Clusters { get; set; }

    public ObservableCollection<ForwardedPortViewModel> ForwardedPorts { get; } = [];

    public WindowViewModel MainWindow { get; }

    public IWindowManager WindowManager { get; }

    public SettingsViewModel Settings { get; }

    public DispatcherQueue DispatcherQueue { get; }

    public ThemeManager ThemeManager { get; }

    public LoggingService LoggingService { get; }

    public HelmService HelmService { get; }

    public AppViewModel(
        Func<IContentDialogService> contentDialogServiceFactory,
        IWindowManager windowManager,
        SettingsViewModel settings,
        DispatcherQueue dispatcherQueue,
        ThemeManager themeManager,
        LoggingService loggingService,
        ISettingsService settingsService
    )
    {
        _contentDialogServiceFactory = contentDialogServiceFactory;
        WindowManager = windowManager;
        Settings = settings;
        DispatcherQueue = dispatcherQueue;
        ThemeManager = themeManager;
        LoggingService = loggingService;
        _settingsService = settingsService;
        HelmService = new HelmService(
            LoggerFactoryExtensions.CreateLogger<HelmService>(loggingService.LoggerFactory)
        );

        var configContent = System.IO.File.ReadAllText(
            KubernetesClientConfiguration.KubeConfigDefaultLocation
        );
        var config = KubernetesYaml.Deserialize<K8SConfiguration>(configContent); // todo move to service, make singleton

        Clusters =
        [
            .. config.Contexts.Select(c => new ClusterViewModel(
                c.Name,
                this,
                new KubernetesContext(c.Name, loggingService.LoggerFactory, settingsService)
            )),
        ];

        MainWindow = new WindowViewModel(this, _contentDialogServiceFactory());
    }

    [RelayCommand]
    public void CreateDetailsWindow(IDetailsSource detailsSource)
    {
        var detailsWindow = new DetailWindowViewModel(
            detailsSource,
            _contentDialogServiceFactory()
        );
        DetailWindowViewModels.Add(detailsWindow);
    }
}
