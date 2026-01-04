using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeNavigator.Models;

public interface IKubernetesResourceRepository
{
    ResourceType ResourceType { get; }

    IReadOnlyCollection<T> GetItems<T>()
        where T : IKubernetesObject<V1ObjectMeta>;

    void AddSubscriber(IKubernetesResourceEventSubscriber subscriber);

    void RemoveSubscriber(IKubernetesResourceEventSubscriber subscriber);

    Task StartAsync();
}
