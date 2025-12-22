using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

public class OutputReceived() : OutgoingMessage
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }
}
