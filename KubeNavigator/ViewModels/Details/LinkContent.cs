using KubeNavigator.Models;

namespace KubeNavigator.ViewModels.Details;

public class LinkContent : IDetailsContent, ITableCellContent
{
    public string? Prefix { get; set; }

    public string? ResourceName { get; set; }

    public required ResourceType ResourceType { get; set; }
}
