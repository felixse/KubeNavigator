using System.Collections.Immutable;
using CommunityToolkit.WinUI.Controls;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

/// <summary>
/// A <see cref="ContentControl"/> that dynamically builds a
/// <see cref="DataRow"/> from the <see cref="ResourceColumn"/>
/// definitions on a <see cref="KubernetesResourceViewModel"/>.
/// <para>
/// On the first bind a <see cref="DataRow"/> with all child elements is
/// created once.  When the <see cref="DataContext"/> changes (e.g. during
/// <see cref="ListView"/> virtualisation / recycling) only the bindings
/// are updated — the element tree is reused.
/// </para>
/// </summary>
internal sealed class DynamicTableRowControl : ContentControl
{
    private KubernetesResourceViewModel? _lastVm;
    private DataRow? _row;
    private ImmutableArray<ResourceColumn> _columns;

    public DynamicTableRowControl()
    {
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not KubernetesResourceViewModel resourceVm || resourceVm == _lastVm)
        {
            return;
        }

        _lastVm = resourceVm;
        var columns = resourceVm.Columns;

        // Improvement 1: reuse the existing DataRow when the column layout
        // has not changed (same column count).  This avoids destroying and
        // recreating the entire element sub-tree on every scroll-recycle.
        if (_row is not null && _columns.Length == columns.Length)
        {
            _columns = columns;
            DynamicResourceTable.UpdateRow(_row, resourceVm, columns);
            return;
        }

        // First time or column layout changed — build from scratch.
        _columns = columns;
        _row = DynamicResourceTable.CreateRow(columns);
        DynamicResourceTable.UpdateRow(_row, resourceVm, columns);
        Content = _row;
    }
}
