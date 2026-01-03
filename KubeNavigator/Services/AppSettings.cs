using CommunityToolkit.Mvvm.ComponentModel;

namespace KubeNavigator.Services;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    public partial AppTheme Theme { get; set; } = AppTheme.System;
}

public enum AppTheme
{
    Light,
    Dark,
    System,
}
