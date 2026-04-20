using CommunityToolkit.Mvvm.ComponentModel;

namespace KubeNavigator.Services;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    public partial AppTheme Theme { get; set; } = AppTheme.System;

    [ObservableProperty]
    public partial string HelmPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HideManagedFields { get; set; } = true;
}

public enum AppTheme
{
    Light,
    Dark,
    System,
}
