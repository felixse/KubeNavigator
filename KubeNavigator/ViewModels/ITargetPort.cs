using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeNavigator.ViewModels;

public interface ITargetPort
{
    Task<(int, IKubernetesObject<V1ObjectMeta>)> GetPortAndResourceAsync();

    string Value { get; }
}
