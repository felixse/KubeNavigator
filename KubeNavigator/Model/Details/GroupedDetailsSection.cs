using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public class GroupedDetailsSection : IDetailsSection
{
    public required string Title { get; init; }

    public required List<DetailsGroup> Groups { get; init; }
}
