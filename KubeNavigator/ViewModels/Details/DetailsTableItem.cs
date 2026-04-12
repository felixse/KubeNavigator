using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KubeNavigator.ViewModels.Details;

internal partial class DetailsTableItem : ObservableObject, IDetailsItem
{
    public string Title { get; set; }
    public IEnumerable<string> Columns { get; init; }

    public IEnumerable<IEnumerable<string>> Rows { get; init; }

    public int Count => Rows.Count();

    public bool IsExpandable { get; set; } = true;

    public DetailsTableItem(
        string title,
        bool isExpandable,
        IEnumerable<string> columns,
        IEnumerable<IEnumerable<string>> rows
    )
    {
        Title = title;
        IsExpandable = isExpandable;
        Columns = columns;
        Rows = rows;
    }
}
