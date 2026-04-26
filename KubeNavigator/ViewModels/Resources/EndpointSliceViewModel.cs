using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class EndpointSliceViewModel : KubernetesResourceViewModel
{
    public EndpointSliceViewModel(V1EndpointSlice resource, ClusterViewModel cluster)
        : base(resource, ResourceType.EndpointSlice, cluster) { }

    public V1EndpointSlice EndpointSlice => (V1EndpointSlice)Resource;

    public static readonly ImmutableArray<ResourceColumn> EndpointSliceColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Address Type",
            vm => ((EndpointSliceViewModel)vm).AddressType,
            PropertyName: nameof(AddressType)
        ),
        new(
            "Ports",
            vm => ((EndpointSliceViewModel)vm).PortsSummary,
            PropertyName: nameof(PortsSummary)
        ),
        new(
            "Endpoints",
            vm => ((EndpointSliceViewModel)vm).EndpointsSummary,
            PropertyName: nameof(EndpointsSummary)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => EndpointSliceColumns;

    public string AddressType => EndpointSlice.AddressType ?? string.Empty;

    public string PortsSummary =>
        EndpointSlice.Ports is { Count: > 0 }
            ? string.Join(
                ", ",
                EndpointSlice.Ports.Select(p =>
                    p.Port.HasValue ? $"{p.Name}:{p.Port}" : p.Name ?? string.Empty
                )
            )
            : string.Empty;

    public string EndpointsSummary =>
        EndpointSlice.Endpoints is { Count: > 0 }
            ? string.Join(", ", EndpointSlice.Endpoints.SelectMany(e => e.Addresses ?? []))
            : string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        var addressRows = GetAddressRows();
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
                            Columns =
                            [
                                "IP",
                                "Hostname",
                                "Node",
                                "Zone",
                                "Target",
                                "Conditions",
                            ],
                            Rows = addressRows,
                        },
                    },
                ],
            }
        );

        var portRows = GetPortRows();
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
                    Value = EndpointSlice.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = EndpointSlice.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = EndpointSlice.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (EndpointSlice.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. EndpointSlice.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (EndpointSlice.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. EndpointSlice.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        var ownerRef = EndpointSlice.Metadata.OwnerReferences?.FirstOrDefault();
        if (ownerRef is not null)
        {
            var ownerResourceType = ResourceType.FromKind(ownerRef.Kind);
            rows.Add(new HeaderedRow
            {
                Header = "Controlled By",
                Content = ownerResourceType is not null
                    ? new LinkContent
                    {
                        Prefix = $"{ownerRef.Kind}/",
                        ResourceName = ownerRef.Name,
                        ResourceType = ownerResourceType,
                    }
                    : new TextContent { Value = $"{ownerRef.Kind}/{ownerRef.Name}" },
            });
        }

        return rows;
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetAddressRows()
    {
        if (EndpointSlice.Endpoints is not { Count: > 0 })
            return [];

        return EndpointSlice.Endpoints.SelectMany(ep =>
        {
            var hostname = ep.Hostname ?? string.Empty;
            var nodeName = ep.NodeName ?? string.Empty;
            var zone = ep.Zone ?? string.Empty;
            var targetRefName = ep.TargetRef?.Name ?? string.Empty;
            var targetRefKind = ep.TargetRef?.Kind ?? string.Empty;
            var conditions = new List<string>();
            if (ep.Conditions?.Ready == true)
                conditions.Add("Ready");
            if (ep.Conditions?.Serving == true)
                conditions.Add("Serving");
            if (ep.Conditions?.Terminating == true)
                conditions.Add("Terminating");
            var conditionsStr = string.Join(", ", conditions);

            var targetRefResourceType = !string.IsNullOrEmpty(targetRefKind)
                ? ResourceType.FromKind(targetRefKind)
                : null;

            ITableCellContent nodeCell = !string.IsNullOrEmpty(nodeName)
                ? new LinkContent
                {
                    ResourceName = nodeName,
                    ResourceType = ResourceType.Node,
                }
                : (TextContent)string.Empty;

            ITableCellContent targetCell =
                targetRefResourceType is not null && !string.IsNullOrEmpty(targetRefName)
                    ? new LinkContent
                    {
                        ResourceName = targetRefName,
                        ResourceType = targetRefResourceType,
                    }
                    : (TextContent)(
                        !string.IsNullOrEmpty(targetRefName)
                            ? $"{targetRefKind}/{targetRefName}"
                            : string.Empty
                    );

            return (ep.Addresses ?? []).Select(addr =>
                (IEnumerable<ITableCellContent>)
                    new ITableCellContent[]
                    {
                        (TextContent)addr,
                        (TextContent)hostname,
                        nodeCell,
                        (TextContent)zone,
                        targetCell,
                        (TextContent)conditionsStr,
                    }
            );
        });
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetPortRows()
    {
        if (EndpointSlice.Ports is not { Count: > 0 })
            return [];

        return EndpointSlice.Ports.Select(p =>
            (IEnumerable<ITableCellContent>)
                new ITableCellContent[]
                {
                    (TextContent)(p.Port?.ToString() ?? string.Empty),
                    (TextContent)(p.Name ?? string.Empty),
                    (TextContent)(p.Protocol ?? string.Empty),
                }
        );
    }
}
