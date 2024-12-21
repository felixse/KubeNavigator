using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

public class InputReceived() : IncomingMessage(nameof(InputReceived))
{
    [JsonPropertyName("data")]
    public required string Data { get; set; }
}
