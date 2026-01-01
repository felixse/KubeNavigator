namespace KubeNavigator.Model.Details;

public class DetailsTextItem : IDetailsItem
{
    public required string Title { get; set; }

    public string? Value { get; set; }

    public Category ValueColor { get; set; } = Category.Default;

    public string ValueColorString => ValueColor.ToString();
}
