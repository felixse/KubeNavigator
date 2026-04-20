using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels.Resources;

public partial class KubernetesResourceViewModel : ObservableObject, ISelectable, IDetailsSource
{
    public event EventHandler? DetailsRefreshRequested;

    public string Name => Resource.Name();
    public string Namespace => Resource.Namespace();
    public DateTime? Age => Resource.Metadata.CreationTimestamp;

    private static readonly ImmutableArray<ResourceColumn> DefaultColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public static ImmutableArray<ResourceColumn> GetDefaultColumns() => DefaultColumns;

    public virtual ImmutableArray<ResourceColumn> Columns => DefaultColumns;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial IKubernetesObject<V1ObjectMeta> Resource { get; set; }
    public ResourceType ResourceType { get; }
    public ClusterViewModel Cluster { get; }

    public string WindowTitle => $"{ResourceType.SingularDisplayName}: {Name}";

    public string PanelTitle => Name;

    public string PanelSubtitle => ResourceType.SingularDisplayName;

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
        var sections = new List<IDetailsSection>
        {
            new DetailsSection
            {
                Rows =
                [
                    new HeaderedRow
                    {
                        Header = "Created",
                        Content = new TextContent
                        {
                            Value = Resource.CreationTimestamp().ToString(),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Name",
                        Content = new TextContent { Value = Resource.Name() },
                    },
                    new HeaderedRow
                    {
                        Header = "Namespace",
                        Content = new LinkContent
                        {
                            ResourceName = Resource.Namespace(),
                            ResourceType = ResourceType.Namespace,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Annotations",
                        Content = new CollectionContent
                        {
                            Items =
                            [
                                .. Resource.Metadata.Annotations?.Select(
                                    a => new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                                ) ?? [],
                            ],
                        },
                    },
                ],
            },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    protected async Task<IDetailsSection?> GetEventsSectionAsync()
    {
        var events = (await Cluster.Context.GetEventsForResourceAsync(Resource)).ToList();
        if (events.Count == 0)
        {
            return null;
        }

        return new DetailsSection
        {
            Header = "Events",
            Rows =
            [
                .. events.Select(e => new GroupRow
                {
                    Header = new DetailsGroupHeader
                    {
                        Title = e.Note,
                        Category = e.Type == "Warning" ? Category.Warning : Category.Default,
                    },
                    Rows = [.. GetEventRows(e)],
                }),
            ],
        };
    }

    private static IEnumerable<IDetailsRow> GetEventRows(Eventsv1Event @event)
    {
        yield return new HeaderedRow
        {
            Header = "Source",
            Content = new TextContent
            {
                Value = $"{@event.ReportingController} {@event.ReportingInstance}",
            },
        };

        yield return new HeaderedRow
        {
            Header = "Count",
            Content = new TextContent { Value = @event.DeprecatedCount?.ToString() },
        };

        yield return new HeaderedRow
        {
            Header = "Sub-object",
            Content = new TextContent { Value = @event.Regarding.FieldPath },
        };

        yield return new HeaderedRow
        {
            Header = "Last seen",
            Content = new TextContent { Value = @event.DeprecatedLastTimestamp?.ToString() },
        };
    }
}
