using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ClusterListView : UserControl
{
    public ClusterListViewModel? ViewModel { get; set; }

    public ClusterListView()
    {
        this.InitializeComponent();
    }

    private async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ClusterViewModel cluster)
        {
            await ViewModel.ConnectAsync(cluster);
        }
    }
}
