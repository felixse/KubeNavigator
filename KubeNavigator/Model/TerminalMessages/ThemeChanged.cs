using System.Text.Json.Serialization;

namespace KubeNavigator.Model.TerminalMessages
{
    public class ThemeChanged : OutgoingMessage
    {
        [JsonPropertyName("theme")]
        public required string Theme { get; set; }
    }
}
