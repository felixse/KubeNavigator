using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ItemListView : UserControl
{
    public ListViewModel? ViewModel { get; set; }

    public ItemListView()
    {
        this.InitializeComponent();
    }
}
