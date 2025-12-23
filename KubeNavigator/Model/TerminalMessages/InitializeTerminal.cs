using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages
{
    public class InitializeTerminal : OutgoingMessage
    {
        [JsonPropertyName("theme")]
        public required string Theme { get; set; }

        [JsonPropertyName("readOnly")]
        public required bool ReadOnly { get; set; }
        }
}
