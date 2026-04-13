using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels.Shelf;

public partial class EditKubernetesResourceViewModel : ObservableObject, IShelfItem
{
    private string? _originalYaml;

    public EditKubernetesResourceViewModel(KubernetesResourceViewModel resource)
    {
        Resource = resource;
    }

    public KubernetesResourceViewModel Resource { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAndCloseCommand))]
    public partial bool ContentLoaded { get; private set; }

    public string Title => $"Edit {Resource.Name}";

    public Func<string>? TextRetriever { get; set; }

    public event EventHandler? Closed;

    public async Task<string> LoadResourceBodyAsync()
    {
        var yaml = await Resource.Cluster.Context.GetResourceAsYamlAsync(
            Resource.ResourceType,
            Resource.Resource.Metadata.Name,
            Resource.ResourceType.IsNamespaceScoped
                ? Resource.Resource.Metadata.NamespaceProperty
                : null
        );

        _originalYaml = yaml;
        ContentLoaded = true;
        return yaml;
    }

    public Task OnCloseAsync()
    {
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(ContentLoaded))]
    public async Task SaveAsync()
    {
        var text = TextRetriever?.Invoke();
        if (!string.IsNullOrWhiteSpace(text) && _originalYaml is not null)
        {
            try
            {
                await Resource.Cluster.Context.PatchResourceFromYamlAsync(
                    _originalYaml,
                    text,
                    Resource.ResourceType,
                    Resource.ResourceType.IsNamespaceScoped
                        ? Resource.Resource.Metadata.NamespaceProperty
                        : null
                );

                // Re-fetch the resource from the server to capture any server-side
                // mutations (e.g. updated resourceVersion, defaults, status) so the
                // next save diffs against the actual server state.
                _originalYaml = await Resource.Cluster.Context.GetResourceAsYamlAsync(
                    Resource.ResourceType,
                    Resource.Resource.Metadata.Name,
                    Resource.ResourceType.IsNamespaceScoped
                        ? Resource.Resource.Metadata.NamespaceProperty
                        : null
                );

                Resource.Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                    "Success",
                    $"{Resource.Resource.Kind} {Resource.Name} has been updated",
                    NotificationSeverity.Success
                );
            }
            catch (Exception ex)
            {
                Resource.Cluster.App.WindowManager.ActiveWindow.ShowMessage(
                    "Error",
                    $"Failed to update {Resource.Resource.Kind} {Resource.Name}: {ex.Message}",
                    NotificationSeverity.Error
                );
            }
        }
    }

    [RelayCommand(CanExecute = nameof(ContentLoaded))]
    public async Task SaveAndCloseAsync()
    {
        await SaveAsync();
        await Resource.Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(this);
    }
}
