using CliWrap;
using CliWrap.Buffered;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using k8s.Models;
using KubeNavigator.Messages;
using KubeNavigator.ViewModels.Resources;
using System;
using System.IO;
using System.Threading.Tasks;

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

    public event EventHandler Closed;

    public async Task<string> LoadResourceBodyAsync()
    {
        var result = await Cli.Wrap("kubectl").WithArguments(args =>
        {
            args.Add("get");
            args.Add(Resource.ResourceType.Plural);
            args.Add(Resource.Resource.Name());
            if (Resource.ResourceType.IsNamespaceScoped)
            {
                args.Add("-n");
                args.Add(Resource.Resource.Namespace());
            }
            args.Add($"--context");
            args.Add(Resource.Cluster.Name);
            args.Add("-o");
            args.Add("yaml");
        }).ExecuteBufferedAsync();

        ContentLoaded = true;
        return result.StandardOutput;
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
            var filePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(filePath, text);

            var result = await Cli.Wrap("kubectl").WithArguments(args =>
            {
                args.Add("apply");
                args.Add("-f");
                args.Add(filePath);
                if (Resource.ResourceType.IsNamespaceScoped)
                {
                    args.Add("-n");
                    args.Add(Resource.Resource.Namespace());
                }
                args.Add($"--context");
                args.Add(Resource.Cluster.Name);

            }).ExecuteBufferedAsync();

            File.Delete(filePath);

            WeakReferenceMessenger.Default.Send(new ShowNotificationMessage { Message = $"{Resource.Resource.Kind} {Resource.Name} has been updated", Title = "Success", Severity = NotificationSeverity.Success });
        }
    }

    [RelayCommand(CanExecute = nameof(ContentLoaded))]
    public async Task SaveAndCloseAsync()
    {
        await SaveAsync();
        await Resource.Cluster.App.WindowManager.ActiveWindow.ShelfHost.CloseShelfItemAsync(this);
    }
}
