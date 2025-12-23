using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Web.AtomPub;

namespace KubeNavigator.Views;

public sealed partial class ClusterListView : UserControl
{
    public ClusterListViewModel? ViewModel { get; set; }

    public ClusterListView()
    {
        this.InitializeComponent();
    }

    private async void OnOpen(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ClusterViewModel cluster)
        {
            await ViewModel.ConnectAsync(cluster);
        }
    }

    private async void OnOpenInNewTab(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ClusterViewModel cluster)
        {
            await ViewModel.ConnectInNewTabAsync(cluster);
        }
    }

    private async void SplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (sender.DataContext is ClusterViewModel cluster)
        {
            await ViewModel.ConnectAsync(cluster);
        }
    }
}
