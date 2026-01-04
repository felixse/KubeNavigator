using System.Collections.Generic;
using System.Threading.Tasks;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.Models;

public interface IUserConfirmationService
{
    Task<bool> ConfirmResourceDeletionAsync(
        ResourceType resourceType,
        IEnumerable<string> resourceNames,
        string clusterName
    );

    Task<PortForwardOptions?> GetPortForwardOptionsAsync(
        PodViewModel pod,
        PortForwardOptions? options
    );
}
