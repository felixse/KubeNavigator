using System;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels;

internal class ServiceTargetPod : ITargetPort
{
    private readonly ServiceViewModel _service;
    private readonly V1ServicePort _port;

    public ServiceTargetPod(ServiceViewModel service, V1ServicePort port)
    {
        _service = service;
        _port = port;
    }

    public string Value => _port.TargetPort.Value;

    public async Task<(int, IKubernetesObject<V1ObjectMeta>)> GetPortAndResourceAsync()
    {
        var podRepository = await _service.Cluster.Context.GetResourceRepositoryAsync(
            ResourceType.Pod
        );
        var pods = podRepository.GetItems<V1Pod>();

        var selector = _service.Service.Spec.Selector;
        if (selector == null || !selector.Any())
        {
            throw new InvalidOperationException("Service does not have a selector");
        }

        var targetPod = pods.FirstOrDefault(pod =>
        {
            if (pod.Metadata?.Labels == null)
            {
                return false;
            }

            return selector.All(selectorLabel =>
                pod.Metadata.Labels.TryGetValue(selectorLabel.Key, out var labelValue)
                && labelValue == selectorLabel.Value
            );
        });

        if (targetPod == null)
        {
            throw new InvalidOperationException($"No pod found matching service selector");
        }

        IKubernetesObject<V1ObjectMeta> resource = targetPod;

        if (int.TryParse(_port.TargetPort.Value, out int port))
        {
            return (port, targetPod);
        }
        else
        {
            var namedPort = _port.TargetPort.Value;
            foreach (var container in targetPod.Spec.Containers)
            {
                var containerPort = container.Ports?.FirstOrDefault(p => p.Name == namedPort);
                if (containerPort != null)
                {
                    return (containerPort.ContainerPort, resource);
                }
            }

            throw new InvalidOperationException(
                $"No container port with name '{namedPort}' found in pod {targetPod.Metadata.Name}"
            );
        }
    }
}
