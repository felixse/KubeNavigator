using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeNavigator.Models;

public class GenericKubernetesItems<T> : IItems<T>, IKubernetesObject
    where T : IKubernetesObject
{
    public required IList<T> Items { get; set; }
    public required string ApiVersion { get; set; }
    public required string Kind { get; set; }
}
