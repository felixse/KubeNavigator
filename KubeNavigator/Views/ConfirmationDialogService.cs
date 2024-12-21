using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.Views;

public class ConfirmationDialogService : IUserConfirmationService
{
    public Page Page { get; set; }

    public async Task<bool> ConfirmResourceDeletionAsync(ResourceType resourceType, IEnumerable<string> resourceNames, string clusterName)
    {
        var dialog = new ConfirmDeletionDialog(resourceType, resourceNames.First(), clusterName); // todo list multiple resources in dialog if more than one selected
        dialog.XamlRoot = Page.XamlRoot;
        var result = await dialog.ShowAsync();

        return result == ContentDialogResult.Primary;
    }

    public async Task<PortForwardOptions?> GetPortForwardOptionsAsync(PodViewModel pod, PortForwardOptions? options)
    {
        var dialog = new PortForwardDialog(pod);
        dialog.XamlRoot = Page.XamlRoot;

        if (options != null)
        {
            dialog.Port = options.Port;
            dialog.OpenInBrowser = options.OpenInBrowser;
        }

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            return new PortForwardOptions
            {
                Port = dialog.Port,
                OpenInBrowser = dialog.OpenInBrowser
            };
        }

        return null;
    }
}
