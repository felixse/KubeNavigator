namespace KubeNavigator.Model.Details;

public class DetailsTextItem : IDetailsItem
{
    public enum Color
    {
        Default, Info, Success, Warning, Error
    }

    public required string Title { get; set; }

    public string? Value { get; set; }

    public Color ValueColor { get; set; } = Color.Default;

    public string ValueColorString => ValueColor.ToString();
}
