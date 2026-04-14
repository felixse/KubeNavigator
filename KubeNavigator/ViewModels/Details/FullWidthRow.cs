namespace KubeNavigator.ViewModels.Details;

public class FullWidthRow : IDetailsRow
{
    public string? Header { get; set; }

    public IDetailsContent? Content { get; set; }
}
