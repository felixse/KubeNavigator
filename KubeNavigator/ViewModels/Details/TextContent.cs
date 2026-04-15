namespace KubeNavigator.ViewModels.Details;

public class TextContent : IDetailsContent, ITableCellContent
{
    public string? Value { get; set; }

    public Category ValueColor { get; set; } = Category.Default;

    public string ValueColorString => ValueColor.ToString();

    public static implicit operator TextContent(string? value) => new() { Value = value };
}
