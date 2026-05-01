using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views.Controls;

public sealed partial class PortForwardsPopupButton : UserControl
{
    public WindowViewModel ViewModel
    {
        get { return (WindowViewModel)GetValue(ViewModelProperty); }
        set { SetValue(ViewModelProperty, value); }
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(WindowViewModel),
        typeof(PortForwardsPopupButton),
        new PropertyMetadata(null)
    );

    public PortForwardsPopupButton()
    {
        InitializeComponent();
    }
}
