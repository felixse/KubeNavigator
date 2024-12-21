using System.Text.Json.Serialization;

namespace KubeNavigator.Model.Helm;
public class HelmReleaseInformation
{
    [JsonPropertyName("first_deployed")]
    public string FirstDeployed { get; set; }

    [JsonPropertyName("last_deployed")]
    public string LastDeployed { get; set; }

    public string Deleted { get; set; }

    public string Description { get; set; }

    public string Status { get; set; }

    public string Notes { get; set; }
}
