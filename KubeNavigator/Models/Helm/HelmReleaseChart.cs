using System.Text.Json;

namespace KubeNavigator.Models.Helm;

public class HelmReleaseChart
{
    public required HelmReleaseChartMetadata Metadata { get; set; }

    public JsonElement? Values { get; set; }
}
