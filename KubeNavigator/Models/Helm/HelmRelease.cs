using System.Text.Json;

namespace KubeNavigator.Models.Helm;

public class HelmRelease
{
    public required string Name { get; set; }

    public required HelmReleaseInformation Info { get; set; }

    public required HelmReleaseChart Chart { get; set; }

    public JsonElement? Config { get; set; }

    public string? Manifest { get; set; }

    public required int Version { get; set; }

    public required string Namespace { get; set; }
}
