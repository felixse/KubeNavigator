using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class GroupRow : IDetailsRow
{
    public DetailsGroupHeader? Header { get; set; }

    public bool HasHeader => Header is not null;

    public required List<IDetailsRow> Rows { get; set; }
}
