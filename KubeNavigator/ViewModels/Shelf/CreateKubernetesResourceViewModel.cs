using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels.Shelf;

public partial class CreateKubernetesResourceViewModel : ObservableObject, IShelfItem
{
    public CreateKubernetesResourceViewModel(
        ClusterViewModel cluster,
        ResourceType resourceType,
        string? targetNamespace
    )
    {
        Cluster = cluster;
        ResourceType = resourceType;
        TargetNamespace = targetNamespace;
    }

    public ClusterViewModel Cluster { get; }
    public ResourceType ResourceType { get; }
    public string? TargetNamespace { get; }

    KubernetesResourceViewModel? IShelfItem.Resource => null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateAndCloseCommand))]
    public partial bool ContentLoaded { get; private set; }

    public string Title => $"Create {ResourceType.SingularDisplayName}";

    public Func<string>? TextRetriever { get; set; }

    public event EventHandler? Closed;

    public string GenerateScaffoldYaml()
    {
        var sb = new StringBuilder();

        var apiVersion = string.IsNullOrEmpty(ResourceType.Group)
            ? ResourceType.Version
            : $"{ResourceType.Group}/{ResourceType.Version}";

        sb.AppendLine($"apiVersion: {apiVersion}");
        sb.AppendLine($"kind: {ResourceType.Kind}");
        sb.AppendLine("metadata:");
        sb.AppendLine($"  name: new-{ResourceType.Kind.ToLowerInvariant()}");

        if (ResourceType.IsNamespaceScoped)
        {
            var ns = TargetNamespace ?? "default";
            sb.AppendLine($"  namespace: {ns}");
        }

        sb.AppendLine("spec: {}");

        ContentLoaded = true;
        return sb.ToString();
    }

    public Task OnCloseAsync()
    {
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(ContentLoaded))]
    public async Task CreateAsync()
    {
        var text = TextRetriever?.Invoke();
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                await Cluster.Context.CreateResourceFromYamlAsync(
                    text,
                    ResourceType,
                    ResourceType.IsNamespaceScoped ? TargetNamespace : null
                );

                Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                    "Success",
                    $"{ResourceType.Kind} has been created",
                    NotificationSeverity.Success
                );
            }
            catch (Exception ex)
            {
                Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                    "Error",
                    $"Failed to create {ResourceType.Kind}: {ex.Message}",
                    NotificationSeverity.Error
                );
            }
        }
    }

    [RelayCommand(CanExecute = nameof(ContentLoaded))]
    public async Task CreateAndCloseAsync()
    {
        await CreateAsync();
        await Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(this);
    }
}
