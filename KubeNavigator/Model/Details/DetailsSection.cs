using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public class DetailsSection : IDetailsSection
{
    public string? Header { get; set; }

    public required IReadOnlyCollection<IDetailsItem> Items { get; set; }
}
