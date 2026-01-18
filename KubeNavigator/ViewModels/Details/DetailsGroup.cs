using System.Collections.Generic;

namespace KubeNavigator.ViewModels.Details;

public class DetailsGroupHeader
{
    public required string Title { get; set; }

    public string? Symbol { get; set; }

    public Category Category { get; set; } = Category.Default;

    public string CategoryString => Category.ToString();
}

public class DetailsGroup
{
    public required DetailsGroupHeader Header { get; set; }

    public required List<IDetailsItem> Items { get; set; }
}
