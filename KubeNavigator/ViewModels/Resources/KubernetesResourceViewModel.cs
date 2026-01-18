using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Resources;

public partial class KubernetesResourceViewModel : ObservableObject, ISelectable
{
    public event EventHandler? DetailsRefreshRequested;

    public string Name => Resource.Name();
    public string Namespace => Resource.Namespace();
    public DateTime? Age => Resource.Metadata.CreationTimestamp;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial IKubernetesObject<V1ObjectMeta> Resource { get; set; }
    public ResourceType ResourceType { get; }
    public ClusterViewModel Cluster { get; }

    public List<ItemCommand> Commands { get; } = [];

    public KubernetesResourceViewModel(
        IKubernetesObject<V1ObjectMeta> resource,
        ResourceType resourceType,
        ClusterViewModel cluster
    )
    {
        Commands.Add(
            new ItemCommand
            {
                Name = "Edit",
                Symbol = "Edit",
                Command = EditCommand,
            }
        );
        Commands.Add(
            new ItemCommand
            {
                Name = "Delete",
                Symbol = "Delete",
                Command = DeleteCommand,
            }
        );
        Cluster = cluster;
        Resource = resource;
        ResourceType = resourceType;
    }

    [RelayCommand]
    public void Edit()
    {
        Cluster.App.WindowManager.ActiveWindow.ShelfHost.OpenShelfItem(
            new EditKubernetesResourceViewModel(this)
        );
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        await Cluster.DeleteResourcesAsync(ResourceType, [this]);
    }

    public void RequestDetailsRefresh()
    {
        DetailsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    public virtual async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        return
        [
            new DetailsSection
            {
                Items =
                [
                    new DetailsTextItem
                    {
                        Title = "Created",
                        Value = Resource.CreationTimestamp().ToString(),
                    },
                    new DetailsTextItem { Title = "Name", Value = Resource.Name() },
                    new DetailsLinkItem
                    {
                        Title = "Namespace",
                        ResourceName = Resource.Namespace(),
                        ResourceType = ResourceType.Namespace,
                    },
                    new DetailsCollectionItem
                    {
                        Title = "Annotations",
                        Items =
                        [
                            .. Resource.Metadata.Annotations?.Select(
                                a => new DetailsCollectionItemElement
                                {
                                    Value = $"{a.Key}={a.Value}",
                                }
                            ) ?? [],
                        ],
                    },
                ],
            },
            events,
        ];
    }

    protected async Task<IDetailsSection> GetEventsSectionAsync()
    {
        var events = await Cluster.Context.GetEventsForResourceAsync(Resource);
        return new GroupedDetailsSection
        {
            Title = "Events",
            Groups =
            [
                .. events.Select(e => new DetailsGroup
                {
                    Header = new DetailsGroupHeader
                    {
                        Title = e.Note,
                        Category = e.Type == "Warning" ? Category.Warning : Category.Default,
                    },
                    Items = [.. GetEventItems(e)],
                }),
            ],
        };
    }

    private static IEnumerable<IDetailsItem> GetEventItems(Eventsv1Event @event)
    {
        yield return new DetailsTextItem
        {
            Title = "Source",
            Value = $"{@event.ReportingController} {@event.ReportingInstance}",
        };

        yield return new DetailsTextItem
        {
            Title = "Count",
            Value = @event.DeprecatedCount?.ToString(),
        };

        yield return new DetailsTextItem
        {
            Title = "Sub-object",
            Value = @event.Regarding.FieldPath,
        };

        yield return new DetailsTextItem
        {
            Title = "Last seen",
            Value = @event.DeprecatedLastTimestamp?.ToString(),
        };
    }
}
