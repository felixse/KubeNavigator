using KubeNavigator.ViewModels.Resources;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KubeNavigator.Model;

public interface IUserConfirmationService
{
    Task<bool> ConfirmResourceDeletionAsync(ResourceType resourceType, IEnumerable<string> resourceNames, string clusterName);

    Task<PortForwardOptions?> GetPortForwardOptionsAsync(PodViewModel pod, PortForwardOptions? options);
}
