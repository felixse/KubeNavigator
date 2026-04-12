using System.Text.Json.Serialization;

namespace KubeNavigator.Models.TerminalMessages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(InitializeTerminal), nameof(InitializeTerminal))]
[JsonDerivedType(typeof(OutputReceived), nameof(OutputReceived))]
[JsonDerivedType(typeof(ClearRequested), nameof(ClearRequested))]
[JsonDerivedType(typeof(ThemeChanged), nameof(ThemeChanged))]
[JsonDerivedType(typeof(SearchTextChanged), nameof(SearchTextChanged))]
public abstract class OutgoingMessage { }
