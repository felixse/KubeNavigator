using System.Text.Json.Serialization;
using KubeNavigator.Models;
using KubeNavigator.Models.Helm;
using KubeNavigator.Models.TerminalMessages;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator;

[JsonSerializable(typeof(DetailsDictionaryEntry))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(HelmRelease))]
[JsonSerializable(typeof(IncomingMessage))]
[JsonSerializable(typeof(OutgoingMessage))]
[JsonSerializable(typeof(TerminalSize))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class SerializerContext : JsonSerializerContext { }
