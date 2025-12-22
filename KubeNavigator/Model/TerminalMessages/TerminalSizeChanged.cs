using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

public class TerminalSizeChanged() : IncomingMessage
{
    [JsonPropertyName("cols")]
    public required int Columns { get; set; }

    [JsonPropertyName("rows")]
    public required int Rows { get; set; }
}
