using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;
public sealed partial class PortForwardsView : UserControl
{
    public PortForwardsView()
    {
        InitializeComponent();
    }

    public PortForwardsViewModel? ViewModel { get; set; }
}
