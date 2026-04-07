using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace KubeNavigator.Views.Controls;

public partial class StatusTextBlock : UserControl
{
    private readonly TextBlock _textBlock;

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status),
        typeof(string),
        typeof(StatusTextBlock),
        new PropertyMetadata(null, OnStatusChanged)
    );

    public string? Status
    {
        get => (string?)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public StatusTextBlock()
    {
        _textBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        Content = _textBlock;
        ActualThemeChanged += OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        ApplyStatusBrush(Status);
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusTextBlock control)
        {
            control._textBlock.Text = e.NewValue as string ?? string.Empty;
            control.ApplyStatusBrush(e.NewValue as string);
        }
    }

    private static string? GetBrushResourceKey(string? status) =>
        status?.ToLower() switch
        {
            "running" or "active" or "deployed" => "SystemFillColorSuccessBrush",
            "pending" => "SystemFillColorCautionBrush",
            "terminating" => "TextFillColorSecondaryBrush",
            _ => "TextFillColorPrimaryBrush",
        };

    private void ApplyStatusBrush(string? status)
    {
        var resourceKey = GetBrushResourceKey(status);

        if (resourceKey is not null && TryFindResource(resourceKey) is Brush brush)
        {
            _textBlock.Foreground = brush;
        }
        else
        {
            _textBlock.ClearValue(TextBlock.ForegroundProperty);
        }
    }

    private object? TryFindResource(string key)
    {
        // Walk up the resource chain from this element so the lookup
        // respects the current ActualTheme (light / dark / high-contrast).
        FrameworkElement? current = this;
        while (current is not null)
        {
            if (current.Resources.TryGetValue(key, out var value))
            {
                return value;
            }

            current = current.Parent as FrameworkElement;
        }

        // Fall back to application-level resources.
        if (Application.Current.Resources.TryGetValue(key, out var appValue))
        {
            return appValue;
        }

        return null;
    }
}
