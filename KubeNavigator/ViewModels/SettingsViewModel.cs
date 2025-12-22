using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class SettingsViewModel : ObservableObject, INavigationTarget
{
    private readonly ISettingsService _settingsService;

    public string Title => "Settings";

    public AppTheme Theme
    {
        get => _settingsService.Settings.Theme;
        set => _settingsService.Settings.Theme = value;
    }

    public IReadOnlyList<AppTheme> AvailableThemes { get; } = Enum.GetValues<AppTheme>();

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
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
