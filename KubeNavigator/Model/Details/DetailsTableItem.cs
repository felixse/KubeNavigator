using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KubeNavigator.Model.Details;
internal partial class DetailsTableItem : ObservableObject, IDetailsItem
{
    public string Title { get; set; }
    public IEnumerable<string> Columns { get; init; }

    public IEnumerable<IEnumerable<string>> Rows { get; init; }

    public string Markdown { get; init; }

    public string ExpandButtonText => IsExpanded ? "Hide" : "Show";

    public int Count => Rows.Count();

    [ObservableProperty]
    [NotifyPropertyChangedFor("ExpandButtonText")]
    public partial bool IsExpanded { get; set; }

    [RelayCommand]
    public void ToggleIsExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    public DetailsTableItem(string title, IEnumerable<string> columns, IEnumerable<IEnumerable<string>> rows)
    {
        Title = title;
        Columns = columns;
        Rows = rows;

        Markdown = GenerateMarkdown();
    }

    public string GenerateMarkdown()
    {
        var stringBuilder = new StringBuilder();
        foreach (var column in Columns)
        {
            stringBuilder.Append($"| {column} ");
        }

        stringBuilder.AppendLine("|");

        foreach (var column in Columns)
        {
            stringBuilder.Append("|-");
        }
        stringBuilder.AppendLine("|");
        foreach (var row in Rows)
        {
            foreach (var cell in row)
            {
                stringBuilder.Append($"| {cell} ");
            }
            stringBuilder.AppendLine("|");
        }

        return stringBuilder.ToString();
    }
}
