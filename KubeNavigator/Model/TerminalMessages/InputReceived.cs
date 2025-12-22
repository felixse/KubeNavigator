using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

public class InputReceived() : IncomingMessage
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }
}
