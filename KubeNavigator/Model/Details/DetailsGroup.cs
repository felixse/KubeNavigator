using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public class DetailsGroup
{
    public required string Title { get; set; } // todo support special headers with e.g. pod status icon later

    public required IReadOnlyCollection<IDetailsItem> Items { get; set; }
}
