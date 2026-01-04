using KubeNavigator.Models;

namespace KubeNavigator.ViewModels.Details;

public class DetailsLinkItem : IDetailsItem
{
    public required string Title { get; set; }

    public string? Prefix { get; set; }

    public string? ResourceName { get; set; }

    public required ResourceType ResourceType { get; set; }
}
