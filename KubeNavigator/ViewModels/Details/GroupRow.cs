using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class GroupRow : IDetailsRow
{
    public required DetailsGroupHeader Header { get; set; }

    public required List<IDetailsRow> Rows { get; set; }
}
