using CommunityToolkit.WinUI.Controls;
using KubeNavigator.ViewModels;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

/// <summary>
/// A <see cref="ContentControl"/> that dynamically builds a
/// <see cref="DataTable"/> header from the <see cref="ResourceColumn"/>
/// definitions on a <see cref="KubernetesResourceTypeListViewModel"/>.
/// </summary>
internal sealed class DynamicTableHeaderControl : ContentControl
{
    private KubernetesResourceTypeListViewModel? _lastVm;

    public DynamicTableHeaderControl()
    {
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is KubernetesResourceTypeListViewModel listVm && listVm != _lastVm)
        {
            _lastVm = listVm;
            Content = DynamicResourceTable.BuildHeader(listVm, listVm.Columns);
        }
    }
}
