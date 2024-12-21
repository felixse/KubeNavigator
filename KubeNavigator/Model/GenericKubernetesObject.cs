using k8s;
using k8s.Models;
using System.Text.Json.Serialization;

namespace KubeNavigator.Model;

public class GenericKubernetesObject : KubernetesObject, IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ObjectMeta? Metadata { get; set; }
}
