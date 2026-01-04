using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels.Shelf;

public partial class EditKubernetesResourceViewModel : ObservableObject, IShelfItem
{
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
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                await Resource.Cluster.Context.ApplyResourceFromYamlAsync(
                    text,
                    Resource.ResourceType,
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
