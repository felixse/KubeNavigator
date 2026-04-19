using System;
using KubeNavigator.ViewModels.ClusterMetrics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ClusterMetricsCard : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(ClusterMetricBaseViewModel),
        typeof(ClusterMetricsCard),
        new PropertyMetadata(null)
    );

    public ClusterMetricBaseViewModel ViewModel
    {
        get => (ClusterMetricBaseViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ClusterMetricsCard()
    {
        this.InitializeComponent();
    }
}
