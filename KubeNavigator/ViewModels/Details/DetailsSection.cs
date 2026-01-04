using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class DetailsSection : IDetailsSection
{
    public string? Header { get; set; }

    public required List<IDetailsItem> Items { get; set; }
}
