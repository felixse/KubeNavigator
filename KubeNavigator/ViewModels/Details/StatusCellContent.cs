namespace KubeNavigator.ViewModels.Details;

public class StatusCellContent : ITableCellContent
{
    public string? Value { get; set; }

    public static implicit operator StatusCellContent(string? value) => new() { Value = value };
}
