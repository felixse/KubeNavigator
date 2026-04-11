using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Models;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels;

public partial class SettingsViewModel : ObservableObject, INavigationTarget
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowManager _windowManager;

    public string Title => "Settings";

    public string AppVersion
    {
        get
        {
            var version = global::Windows.ApplicationModel.Package.Current.Id.Version;
            return $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }

    public AppTheme Theme
    {
        get => _settingsService.Settings.Theme;
        set => _settingsService.Settings.Theme = value;
    }

    public string KubectlPath
    {
        get => _settingsService.Settings.KubectlPath;
        set
        {
            if (_settingsService.Settings.KubectlPath != value)
            {
                _settingsService.Settings.KubectlPath = value;
                OnPropertyChanged();
            }
        }
    }

    public string HelmPath
    {
        get => _settingsService.Settings.HelmPath;
        set
        {
            if (_settingsService.Settings.HelmPath != value)
            {
                _settingsService.Settings.HelmPath = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<AppTheme> AvailableThemes { get; } = Enum.GetValues<AppTheme>();

    public SettingsViewModel(ISettingsService settingsService, IWindowManager windowManager)
    {
        _settingsService = settingsService;
        _windowManager = windowManager;
    }

    [RelayCommand]
    private async Task BrowseKubectlPathAsync()
    {
        var path = await _windowManager.ActiveWindow.PickFileAsync([".exe", "*"]);
        if (path is not null)
        {
            KubectlPath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseHelmPathAsync()
    {
        var path = await _windowManager.ActiveWindow.PickFileAsync([".exe", "*"]);
        if (path is not null)
        {
            HelmPath = path;
        }
    }

    public Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
