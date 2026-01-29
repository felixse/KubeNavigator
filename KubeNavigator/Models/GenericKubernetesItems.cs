using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeNavigator.Models;

public class GenericKubernetesItems<T> : KubernetesList<T>, IKubernetesObject
    where T : IKubernetesObject
{
    public GenericKubernetesItems(
        IList<T> items,
        string apiVersion = null,
        string kind = null,
        V1ListMeta metadata = null
    )
        : base(items, apiVersion, kind, metadata) { }
}
