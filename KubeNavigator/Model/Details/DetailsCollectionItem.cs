using System.Collections.Generic;

namespace KubeNavigator.Model.Details;

public class DetailsCollectionItemElement
{
    public required string Value { get; set; }

    public string? SecondaryValue { get; set; }
}

public class DetailsCollectionItem : IDetailsItem
{
    public required string Title { get; set; }

    public bool IsWrapLayout { get; set; } = true;

    public required ICollection<DetailsCollectionItemElement> Items { get; set; }
}
