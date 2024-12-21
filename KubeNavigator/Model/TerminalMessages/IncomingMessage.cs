using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(InputReceived), nameof(InputReceived))]
[JsonDerivedType(typeof(TerminalSizeChanged), nameof(TerminalSizeChanged))]
public class IncomingMessage(string type)
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = type;
}
