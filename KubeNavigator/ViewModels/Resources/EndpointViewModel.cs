using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class EndpointViewModel : KubernetesResourceViewModel
{
    public EndpointViewModel(V1Endpoints resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Endpoint, cluster) { }

    public V1Endpoints Endpoints => (V1Endpoints)Resource;

    public static readonly ImmutableArray<ResourceColumn> EndpointColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Endpoints",
            vm => ((EndpointViewModel)vm).EndpointsSummary,
            PropertyName: nameof(EndpointsSummary)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => EndpointColumns;

    public string EndpointsSummary =>
        Endpoints.Subsets is { Count: > 0 }
            ? string.Join(
                ", ",
                Endpoints.Subsets.SelectMany(s => s.Addresses ?? []).Select(a => a.Ip)
            )
            : string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        var addressRows = GetAddressRows().ToList();
        if (addressRows.Count > 0)
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Addresses",
                    Rows =
                    [
                        new FullWidthRow
                        {
                            Content = new TableContent
                            {
                                Columns = ["IP", "Hostname", "Target"],
                                Rows = addressRows,
                            },
                        },
                    ],
                }
            );
        }

        var portRows = GetPortRows().ToList();
        if (portRows.Count > 0)
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Ports",
                    Rows =
                    [
                        new FullWidthRow
                        {
                            Content = new TableContent
                            {
                                Columns = ["Port", "Name", "Protocol"],
                                Rows = portRows,
                            },
                        },
                    ],
                }
            );
        }

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private List<IDetailsRow> GetInfoRows()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = Endpoints.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = Endpoints.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = Endpoints.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (Endpoints.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Endpoints.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (Endpoints.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Endpoints.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        return rows;
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetAddressRows()
    {
        if (Endpoints.Subsets is not { Count: > 0 })
            return [];

        return Endpoints.Subsets.SelectMany(subset =>
            (subset.Addresses ?? []).Select(addr =>
            {
                var targetRef = addr.TargetRef;
                ITableCellContent targetCell =
                    targetRef is not null && !string.IsNullOrEmpty(targetRef.Name)
                        ? new LinkContent
                        {
                            ResourceName = targetRef.Name,
                            ResourceType = ResourceType.FromKind(targetRef.Kind ?? string.Empty)
                                ?? ResourceType.Pod,
                        }
                        : (TextContent)string.Empty;

                return (IEnumerable<ITableCellContent>)
                    new ITableCellContent[]
                    {
                        (TextContent)(addr.Ip ?? string.Empty),
                        (TextContent)(addr.Hostname ?? string.Empty),
                        targetCell,
                    };
            })
        );
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetPortRows()
    {
        if (Endpoints.Subsets is not { Count: > 0 })
            return [];

        return Endpoints.Subsets
            .SelectMany(subset => subset.Ports ?? [])
            .Select(p =>
                (IEnumerable<ITableCellContent>)
                    new ITableCellContent[]
                    {
                        (TextContent)(p.Port.ToString()),
                        (TextContent)(p.Name ?? string.Empty),
                        (TextContent)(p.Protocol ?? string.Empty),
                    }
            );
    }
}
