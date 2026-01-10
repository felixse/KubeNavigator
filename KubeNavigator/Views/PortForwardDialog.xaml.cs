using System;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Globalization.NumberFormatting;

namespace KubeNavigator.Views;

public sealed partial class PortForwardDialog : ContentDialog
{
    public PortForwardDialog(KubernetesResourceViewModel resource)
    {
        this.InitializeComponent();
        Resource = resource;

        PortNumberBox.NumberFormatter = new DecimalFormatter
        {
            IntegerDigits = 5,
            FractionDigits = 0,
        };

        Title = $"Port Forwarding";
        SubTitleTextBlock.Text = $"{Resource.ResourceType.SingularDisplayName}: {Resource.Name}";
        Port = new Random().Next(49152, 65535);

        PrimaryButtonText = "Start";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;
    }

    public KubernetesResourceViewModel Resource { get; }

    public bool OpenInBrowser
    {
        get { return (bool)GetValue(OpenInBrowserProperty); }
        set { SetValue(OpenInBrowserProperty, value); }
    }

    public static readonly DependencyProperty OpenInBrowserProperty = DependencyProperty.Register(
        nameof(OpenInBrowser),
        typeof(bool),
        typeof(PortForwardDialog),
        new PropertyMetadata(false)
    );

    public int Port { get; set; }

    private void PortNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        Port = (int)args.NewValue;
    }
}
