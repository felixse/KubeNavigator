using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

public class OutputReceived() : OutgoingMessage(nameof(OutputReceived))
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }
}
