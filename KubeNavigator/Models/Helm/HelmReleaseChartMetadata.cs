namespace KubeNavigator.Models.Helm;

public class HelmReleaseChartMetadata
{
    public required string Name { get; set; }

    public required string Version { get; set; }

    public string? AppVersion { get; set; }
}
