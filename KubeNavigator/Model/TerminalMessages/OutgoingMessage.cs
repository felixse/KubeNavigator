using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OutputReceived), nameof(OutputReceived))]
[JsonDerivedType(typeof(ClearRequested), nameof(ClearRequested))]
public abstract class OutgoingMessage
{
}
