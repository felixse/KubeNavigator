using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(InitializeTerminal), nameof(InitializeTerminal))]
[JsonDerivedType(typeof(OutputReceived), nameof(OutputReceived))]
[JsonDerivedType(typeof(ClearRequested), nameof(ClearRequested))]
[JsonDerivedType(typeof(ThemeChanged), nameof(ThemeChanged))]
public abstract class OutgoingMessage
{
}
