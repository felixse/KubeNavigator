using KubeNavigator.Model;
using KubeNavigator.Model.Details;
using KubeNavigator.Model.Helm;
using KubeNavigator.Model.TerminalMessages;
using KubeNavigator.Services;
using System.Text.Json.Serialization;

namespace KubeNavigator;

[JsonSerializable(typeof(DetailsDictionaryEntry))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(HelmRelease))]
[JsonSerializable(typeof(IncomingMessage))]
[JsonSerializable(typeof(OutgoingMessage))]
[JsonSerializable(typeof(TerminalSize))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class SerializerContext : JsonSerializerContext
{
}
