using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace KubeNavigator.Models;

public class GenericKubernetesObject : KubernetesObject, IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ObjectMeta? Metadata { get; set; }
}
