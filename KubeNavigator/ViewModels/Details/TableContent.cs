using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KubeNavigator.ViewModels.Details;

public partial class TableContent : ObservableObject, IDetailsContent
{
    public required IEnumerable<string> Columns { get; init; }

    public required IEnumerable<IEnumerable<ITableCellContent>> Rows { get; init; }

    public int Count => Rows.Count();

    public bool IsExpandable { get; set; } = true;
}
