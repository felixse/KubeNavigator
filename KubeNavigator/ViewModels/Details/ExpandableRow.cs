namespace KubeNavigator.ViewModels.Details;

public class ExpandableRow : IDetailsRow
{
    public required string Header { get; set; }

    public required string Summary { get; set; }

    public required IDetailsContent Content { get; set; }
}
