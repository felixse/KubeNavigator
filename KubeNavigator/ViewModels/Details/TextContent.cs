namespace KubeNavigator.ViewModels.Details;

public class TextContent : IDetailsContent
{
    public string? Value { get; set; }

    public Category ValueColor { get; set; } = Category.Default;

    public string ValueColorString => ValueColor.ToString();
}
