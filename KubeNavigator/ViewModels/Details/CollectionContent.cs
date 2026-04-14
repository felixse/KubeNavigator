using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public enum CollectionLayout
{
    Stack,
    Wrap,
}

public class CollectionContent : IDetailsContent
{
    public required ICollection<IDetailsCollectionElement> Items { get; set; }

    public CollectionLayout Layout { get; set; } = CollectionLayout.Wrap;
}
