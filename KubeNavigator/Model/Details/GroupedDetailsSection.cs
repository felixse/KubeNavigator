using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public class GroupedDetailsSection : IDetailsSection
{
    public required string Title { get; init; }

    public required IReadOnlyCollection<DetailsGroup> Groups { get; init; }
}
