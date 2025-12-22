using k8s;
using k8s.Models;
using System.Collections.Generic;

namespace KubeNavigator.Model;

public class GenericKubernetesItems<T> : IItems<T>, IKubernetesObject
    where T : IKubernetesObject
{
    public IList<T> Items { get; set; }
    public string ApiVersion { get; set; }
    public string Kind { get; set; }
}
