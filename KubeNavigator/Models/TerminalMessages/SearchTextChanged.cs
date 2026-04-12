using System.Text.Json.Serialization;

namespace KubeNavigator.Models.TerminalMessages;

public class SearchTextChanged : OutgoingMessage
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("findPrevious")]
    public bool FindPrevious { get; set; }

    [JsonPropertyName("incremental")]
    public bool Incremental { get; set; }
}
