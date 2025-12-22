using KubeNavigator.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.UI.ViewManagement;

namespace KubeNavigator;

public class ThemeManager
{
    private readonly List<FrameworkElement> _themeTargets = [];
    private readonly UISettings _uiSettings;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;

    public ThemeManager(ISettingsService settingsService, DispatcherQueue dispatcherQueue)
    {
        _settingsService = settingsService;
        _dispatcherQueue = dispatcherQueue;
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnSystemThemeChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public void RegisterThemeTarget(FrameworkElement element)
    {
        if (!_themeTargets.Contains(element))
        {
            _themeTargets.Add(element);
            ApplyTheme(element, _settingsService.Settings.Theme);
        }
    }

    public void UnregisterThemeTarget(FrameworkElement element)
    {
        _themeTargets.Remove(element);
    }

    private void OnSystemThemeChanged(UISettings sender, object args)
    {
        if (_settingsService.Settings.Theme == AppTheme.System)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ApplyCurrentTheme();
            });
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        ApplyCurrentTheme();
    }

    private void ApplyCurrentTheme()
    {
        foreach (var target in _themeTargets)
        {
            ApplyTheme(target, _settingsService.Settings.Theme);
        }
    }

    private void ApplyTheme(FrameworkElement element, AppTheme theme)
    {
        element.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            AppTheme.System => GetSystemTheme(),
            _ => ElementTheme.Default
        };
    }

    private ElementTheme GetSystemTheme()
    {
        var uiTheme = _uiSettings.GetColorValue(UIColorType.Background).ToString();
        return uiTheme == "#FF000000" ? ElementTheme.Dark : ElementTheme.Light;
    }
}
