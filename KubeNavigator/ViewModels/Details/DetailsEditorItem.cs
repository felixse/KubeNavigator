using System;
using System.Collections.Generic;
using System.Text;

namespace KubeNavigator.ViewModels.Details;

internal class DetailsEditorItem : IDetailsItem
{
    public string Title { get; set; }
    public string Value { get; set; }
    public bool ShowTitleInColumn { get; set; }

    public Func<string>? TextRetriever { get; set; }

    public DetailsEditorItem(string title, string value)
    {
        Title = title;
        Value = value;
    }
}
