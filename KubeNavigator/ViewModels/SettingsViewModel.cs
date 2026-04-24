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
    private readonly LoggingService _loggingService;
    private readonly ViewStateService _viewStateService;

    public string Title => "Settings";

    public string LogDirectory => _loggingService.LogDirectory;

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

    public bool HideManagedFields
    {
        get => _settingsService.Settings.HideManagedFields;
        set
        {
            if (_settingsService.Settings.HideManagedFields != value)
            {
                _settingsService.Settings.HideManagedFields = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<AppTheme> AvailableThemes { get; } = Enum.GetValues<AppTheme>();

    public SettingsViewModel(
        ISettingsService settingsService,
        IWindowManager windowManager,
        LoggingService loggingService,
        ViewStateService viewStateService
    )
    {
        _settingsService = settingsService;
        _windowManager = windowManager;
        _loggingService = loggingService;
        _viewStateService = viewStateService;
    }

    [RelayCommand]
    private async Task OpenLogDirectoryAsync()
    {
        await global::Windows.System.Launcher.LaunchFolderPathAsync(LogDirectory);
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

    [RelayCommand]
    private async Task ResetViewStateAsync()
    {
        await _viewStateService.ResetAsync();
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
