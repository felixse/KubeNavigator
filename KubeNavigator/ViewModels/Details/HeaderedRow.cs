namespace KubeNavigator.ViewModels.Details;

public class HeaderedRow : IDetailsRow
{
    public required string Header { get; set; }

    public required IDetailsContent Content { get; set; }
}
