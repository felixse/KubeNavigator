using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public class ContentDialogService : IContentDialogService
{
    private readonly ThemeManager _themeManager;

    public Page? Page { get; set; }

    public ContentDialogService(ThemeManager themeManager)
    {
        _themeManager = themeManager;
    }

    public async Task<bool> ConfirmResourceDeletionAsync(
        ResourceType resourceType,
        IEnumerable<string> resourceNames,
        string clusterName
    )
    {
        var dialog = new ConfirmDeletionDialog(resourceType, resourceNames.First(), clusterName) // todo list multiple resources in dialog if more than one selected
        {
            XamlRoot = Page?.XamlRoot,
            RequestedTheme = GetRequestedTheme(),
        };
        var result = await dialog.ShowAsync();

        return result == ContentDialogResult.Primary;
    }

    public async Task<PortForwardOptions?> GetPortForwardOptionsAsync(
        KubernetesResourceViewModel resource,
        PortForwardOptions? options
    )
    {
        var dialog = new PortForwardDialog(resource)
        {
            XamlRoot = Page?.XamlRoot,
            RequestedTheme = GetRequestedTheme(),
        };

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
                OpenInBrowser = dialog.OpenInBrowser,
            };
        }

        return null;
    }

    public async Task<bool> ShowConnectingDialogAsync(
        string clusterName,
        Func<CancellationToken, Task> connectAction
    )
    {
        var dialog = new ConnectingDialog(clusterName)
        {
            XamlRoot = Page?.XamlRoot,
            RequestedTheme = GetRequestedTheme(),
        };

        var connectTask = connectAction(dialog.CancellationToken);
        _ = dialog.ShowAsync();

        try
        {
            await connectTask;
            dialog.Hide();
            return true;
        }
        catch (OperationCanceledException)
        {
            dialog.Hide();
            return false;
        }
    }

    public async Task<bool> ShowToolsNotFoundDialogAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "CLI tools not found",
            Content = message,
            PrimaryButtonText = "Go to Settings",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Page?.XamlRoot,
            RequestedTheme = GetRequestedTheme(),
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private ElementTheme GetRequestedTheme()
    {
        return _themeManager.GetEffectiveTheme() == ThemeManager.EffectiveTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }
}
