using k8s;
using k8s.Models;
using System.Threading.Tasks;

namespace KubeNavigator.Model;
public interface IKubernetesResourceEventSubscriber
{
    public Task OnResourceEvent(KubernetesResourceEvent resourceEvent, ResourceType resourceType, IKubernetesObject<V1ObjectMeta> resource);
}
