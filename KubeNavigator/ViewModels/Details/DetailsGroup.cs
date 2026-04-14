namespace KubeNavigator.ViewModels.Details;

public class DetailsGroupHeader
{
    public required string Title { get; set; }

    public string? Symbol { get; set; }

    public Category Category { get; set; } = Category.Default;

    public string CategoryString => Category.ToString();
}
