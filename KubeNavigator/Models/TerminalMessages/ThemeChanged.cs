using System.Text.Json.Serialization;

namespace KubeNavigator.Models.TerminalMessages
{
    public class ThemeChanged : OutgoingMessage
    {
        [JsonPropertyName("theme")]
        public required string Theme { get; set; }
    }
}
