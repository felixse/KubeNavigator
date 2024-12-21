using KubeNavigator.Model;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ConfirmDeletionDialog : ContentDialog
{
    public ConfirmDeletionDialog(ResourceType resourceType, string resourceName, string clusterName)
    {
        this.InitializeComponent();

        Title = "Please Confirm";
        //var content = $"Delete {resourceType.SingularDisplayName} \b{resourceName}\b0 in the cluster \b{clusterName}\b0?";

        PrimaryButtonText = "Delete";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;
        ResourceType.Text = resourceType.SingularDisplayName;
        ResourceName.Text = resourceName;
        ClusterName.Text = clusterName;
    }
}
