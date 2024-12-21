using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public class DetailsCollectionItem : IDetailsItem
{
    public required string Title { get; set; }

    public required ICollection<string> Items { get; set; }
}
