namespace KubeNavigator.ViewModels.Details;

public class TextCollectionElement : IDetailsCollectionElement
{
    public required string Value { get; set; }

    public string? SecondaryValue { get; set; }
}
