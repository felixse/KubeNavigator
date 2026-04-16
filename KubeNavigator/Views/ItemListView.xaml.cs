using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ItemListView : UserControl
{
    public ListViewModel? ViewModel { get; set; }

    private double _headerWidth;

    public ItemListView()
    {
        this.InitializeComponent();
    }

    private void HeaderContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _headerWidth = e.NewSize.Width;
        ApplyWidthToAllContainers();
    }

    private void ItemsListView_ContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args)
    {
        if (_headerWidth > 0)
        {
            args.ItemContainer.Width = _headerWidth;
        }
    }

    private void ApplyWidthToAllContainers()
    {
        if (_headerWidth <= 0 || ItemsListView.Items is null)
        {
            return;
        }

        for (var i = 0; i < ItemsListView.Items.Count; i++)
        {
            if (ItemsListView.ContainerFromIndex(i) is ListViewItem container)
            {
                container.Width = _headerWidth;
            }
        }
    }
}
