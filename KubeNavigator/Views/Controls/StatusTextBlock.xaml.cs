using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views.Controls;

public sealed partial class StatusTextBlock : UserControl
{
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
        InitializeComponent();
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusTextBlock control && e.NewValue is string status)
        {
            var canonical = ToCanonicalCasing(status);
            if (!string.Equals(status, canonical, StringComparison.Ordinal))
            {
                control.Status = canonical;
            }
        }
    }

    private static string ToCanonicalCasing(string status) => status.ToLowerInvariant() switch
    {
        "running" => "Running",
        "active" => "Active",
        "deployed" => "Deployed",
        "pending" => "Pending",
        "terminating" => "Terminating",
        _ => status
    };
}
