using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace KubeNavigator.Models;

public class GenericKubernetesObject : KubernetesObject, IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("metadata")]
    public V1ObjectMeta? Metadata { get; set; }

    /// <summary>
    /// Captures all JSON properties that don't map to a declared member
    /// (e.g. <c>spec</c>, <c>status</c>). Used for CRD additionalPrinterColumns
    /// JSONPath evaluation.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
