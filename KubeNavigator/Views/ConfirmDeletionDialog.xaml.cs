using KubeNavigator.Models;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ConfirmDeletionDialog : ContentDialog
{
    public string Body { get; private set; }

    public ConfirmDeletionDialog(ResourceType resourceType, string resourceName, string clusterName)
    {
        this.InitializeComponent();

        Title = "Please Confirm";
        Body =
            $"Delete {resourceType.SingularDisplayName} **{resourceName}** in the cluster **{clusterName}**?";

        PrimaryButtonText = "Delete";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;
    }
}
