using k8s.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace KubeNavigator.Views;

public sealed partial class ContainerStatusView : UserControl
{


    public V1ContainerStatus? Status
    {
        get { return (V1ContainerStatus?)GetValue(MyPropertyProperty); }
        set { SetValue(MyPropertyProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MyPropertyProperty =
        DependencyProperty.Register(nameof(Status), typeof(V1ContainerStatus), typeof(ContainerStatusView), new PropertyMetadata(null));



    public ContainerStatusView()
    {
        this.InitializeComponent();
    }
}
