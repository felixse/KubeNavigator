using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels;

internal class PodTargetPort : ITargetPort
{
    private readonly int _containerPort;
    private readonly KubernetesResourceViewModel _pod;

    public PodTargetPort(int containerPort, PodViewModel pod)
    {
        _containerPort = containerPort;
        _pod = pod;
    }

    public string Value => _containerPort.ToString();

    public Task<(int, IKubernetesObject<V1ObjectMeta>)> GetPortAndResourceAsync()
    {
        return Task.FromResult((_containerPort, _pod.Resource));
    }
}
