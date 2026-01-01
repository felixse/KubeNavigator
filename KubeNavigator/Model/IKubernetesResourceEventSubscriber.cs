using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeNavigator.Model;

public interface IKubernetesResourceEventSubscriber
{
    public Task OnResourceEvent(
        KubernetesResourceEvent resourceEvent,
        ResourceType resourceType,
        IKubernetesObject<V1ObjectMeta> resource
    );
}
