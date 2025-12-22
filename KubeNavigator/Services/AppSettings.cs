using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace KubeNavigator.Services;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    private AppTheme _theme = AppTheme.System;
}

public enum AppTheme
{
    Light,
    Dark,
    System
}
