using KubeNavigator.Models;

namespace KubeNavigator.ViewModels.Details;

public class LinkCollectionElement : IDetailsCollectionElement
{
    public required string ResourceName { get; set; }

    public required ResourceType ResourceType { get; set; }
}
