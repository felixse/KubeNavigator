using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.Models;

public interface IContentDialogService
{
    Task<bool> ConfirmResourceDeletionAsync(
        ResourceType resourceType,
        IEnumerable<string> resourceNames,
        string clusterName
    );

    Task<PortForwardOptions?> GetPortForwardOptionsAsync(
        KubernetesResourceViewModel resource,
        PortForwardOptions? options
    );

    Task<bool> ShowConnectingDialogAsync(
        string clusterName,
        Func<CancellationToken, Task> connectAction
    );

    Task<bool> ConfirmHelmReleaseDeletionAsync(
        IEnumerable<string> releaseNames,
        string clusterName,
        Func<CancellationToken, Task<string?>> uninstallAction
    );

    Task<bool> ShowToolsNotFoundDialogAsync(string message);

    Task ShowInfoDialogAsync(string title, string message);
}
