using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KubeNavigator.ViewModels.Details;

public class SegmentedRowItem(string label, List<IDetailsRow> rows)
{
    public string Label { get; } = label;

    public List<IDetailsRow> Rows { get; } = rows;
}

public partial class SegmentedRow : ObservableObject, IDetailsRow
{
    public required Dictionary<string, List<IDetailsRow>> Segments
    {
        get;
        set
        {
            field = value;
            Items = [.. value.Select(kvp => new SegmentedRowItem(kvp.Key, kvp.Value))];
            SelectedItem = Items.FirstOrDefault();
        }
    }

    public List<SegmentedRowItem> Items { get; private set; } = [];

    [ObservableProperty]
    public partial SegmentedRowItem? SelectedItem { get; set; }
}
