using System.Text.Json.Serialization;

namespace KubeNavigator.Models.TerminalMessages;

public class InputReceived : IncomingMessage
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }
}
