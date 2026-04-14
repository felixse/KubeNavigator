using System;

namespace KubeNavigator.ViewModels.Details;

public class EditorContent : IDetailsContent
{
    public string? Title { get; set; }

    public required string Value { get; set; }

    public Func<string>? TextRetriever { get; set; }
}
