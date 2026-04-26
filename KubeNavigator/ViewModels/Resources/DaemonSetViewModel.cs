using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class DaemonSetViewModel : KubernetesResourceViewModel
{
    public DaemonSetViewModel(V1DaemonSet resource, ClusterViewModel cluster)
        : base(resource, ResourceType.DaemonSet, cluster) { }

    public V1DaemonSet DaemonSet => (V1DaemonSet)Resource;

    public static readonly ImmutableArray<ResourceColumn> DaemonSetColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Desired", vm => ((DaemonSetViewModel)vm).Desired, PropertyName: nameof(Desired)),
        new("Current", vm => ((DaemonSetViewModel)vm).Current, PropertyName: nameof(Current)),
        new("Ready", vm => ((DaemonSetViewModel)vm).Ready, PropertyName: nameof(Ready)),
        new("Updated", vm => ((DaemonSetViewModel)vm).Updated, PropertyName: nameof(Updated)),
        new("Available", vm => ((DaemonSetViewModel)vm).Available, PropertyName: nameof(Available)),
        new(
            "Node Selector",
            vm => ((DaemonSetViewModel)vm).NodeSelector,
            PropertyName: nameof(NodeSelector)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => DaemonSetColumns;

    public int Desired => DaemonSet.Status?.DesiredNumberScheduled ?? 0;

    public int Current => DaemonSet.Status?.CurrentNumberScheduled ?? 0;

    public int Ready => DaemonSet.Status?.NumberReady ?? 0;

    public int Updated => DaemonSet.Status?.UpdatedNumberScheduled ?? 0;

    public int Available => DaemonSet.Status?.NumberAvailable ?? 0;

    public string NodeSelector =>
        DaemonSet.Spec?.Template?.Spec?.NodeSelector is { Count: > 0 } ns
            ? string.Join(", ", ns.Select(kv => $"{kv.Key}={kv.Value}"))
            : string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. await GetDaemonSetRowsAsync()] },
        };

        var podRows = await GetPodRowsAsync();
        sections.Add(
            new DetailsSection
            {
                Header = "Pods",
                Rows =
                [
                    new FullWidthRow
                    {
                        Content = new TableContent
                        {
                            Columns = ["Name", "Namespace", "Ready", "CPU", "Memory", "Status"],
                            Rows = podRows,
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

    private async Task<List<IDetailsRow>> GetDaemonSetRowsAsync()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent { Value = DaemonSet.CreationTimestamp().ToString() },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = DaemonSet.Name() },
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
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. DaemonSet.Metadata.Labels?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            },
        };

        if (DaemonSet.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. DaemonSet.Metadata.Annotations.Select(a => new TextCollectionElement
                        {
                            Value = $"{a.Key}={a.Value}",
                        }),
                    ],
                },
            });
        }

        rows.Add(new HeaderedRow
        {
            Header = "Selector",
            Content = new CollectionContent
            {
                Items =
                [
                    .. DaemonSet.Spec?.Selector?.MatchLabels?.Select(s => new TextCollectionElement
                    {
                        Value = $"{s.Key}={s.Value}",
                    }) ?? [],
                ],
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Node Selector",
            Content = new CollectionContent
            {
                Items =
                [
                    .. DaemonSet.Spec?.Template?.Spec?.NodeSelector?.Select(
                        n => new TextCollectionElement { Value = $"{n.Key}={n.Value}" }
                    ) ?? [],
                ],
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Images",
            Content = new CollectionContent
            {
                Items =
                [
                    .. DaemonSet.Spec?.Template?.Spec?.Containers?.Select(c =>
                        new TextCollectionElement { Value = c.Image }
                    ) ?? [],
                ],
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Strategy Type",
            Content = new TextContent
            {
                Value = DaemonSet.Spec?.UpdateStrategy?.Type ?? string.Empty,
            },
        });

        var tolerationsTable = new TableContent
        {
            Columns = ["Key", "Operator", "Value", "Effect", "Seconds"],
            Rows =
                DaemonSet.Spec?.Template?.Spec?.Tolerations?.Select(t =>
                    (IEnumerable<ITableCellContent>)
                        new TextContent[]
                        {
                            t.Key,
                            t.OperatorProperty,
                            t.Value,
                            t.Effect,
                            t.TolerationSeconds?.ToString() ?? string.Empty,
                        }
                )
                ?? [],
        };

        rows.Add(new ExpandableRow
        {
            Header = "Tolerations",
            Summary = tolerationsTable.Count.ToString(),
            Content = tolerationsTable,
        });

        var podStatuses = await GetPodStatusSummaryAsync();
        rows.Add(new HeaderedRow
        {
            Header = "Pod Status",
            Content = new TextContent
            {
                Value = podStatuses,
            },
        });

        return rows;
    }

    private async Task<string> GetPodStatusSummaryAsync()
    {
        var pods = await Cluster.GetResourcesAsync(ResourceType.Pod);

        var matchLabels = DaemonSet.Spec?.Selector?.MatchLabels;
        if (matchLabels is null || matchLabels.Count == 0)
            return string.Empty;

        var statusGroups = pods
            .Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == DaemonSet.Namespace()
                && matchLabels.All(label =>
                    pod.Metadata.Labels?.ContainsKey(label.Key) == true
                    && pod.Metadata.Labels[label.Key] == label.Value
                )
            )
            .Select(p =>
            {
                var pod = (V1Pod)p.Resource;
                return pod.Metadata.DeletionTimestamp is not null
                    ? "Terminating"
                    : pod.Status?.Phase ?? "Unknown";
            })
            .GroupBy(s => s)
            .Select(g => $"{g.Key}: {g.Count()}");

        return string.Join(", ", statusGroups);
    }

    private async Task<IEnumerable<IEnumerable<ITableCellContent>>> GetPodRowsAsync()
    {
        var pods = await Cluster.GetResourcesAsync(ResourceType.Pod);
        var matchLabels = DaemonSet.Spec?.Selector?.MatchLabels;

        if (matchLabels is null || matchLabels.Count == 0)
            return [];

        return pods
            .Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == DaemonSet.Namespace()
                && matchLabels.All(label =>
                    pod.Metadata.Labels?.ContainsKey(label.Key) == true
                    && pod.Metadata.Labels[label.Key] == label.Value
                )
            )
            .Select(p =>
            {
                var pod = (V1Pod)p.Resource;
                var ready =
                    pod.Status?.ContainerStatuses != null
                        ? $"{pod.Status.ContainerStatuses.Count(c => c.Ready)}/{pod.Status.ContainerStatuses.Count}"
                        : "0/0";
                var metrics = Cluster.Context.GetPodMetrics(pod.Namespace(), pod.Name());
                var cpu = metrics?.Cpu.Format() ?? "-";
                var memory = metrics?.Memory.Format() ?? "-";
                var status = pod.Metadata.DeletionTimestamp is not null
                    ? "Terminating"
                    : pod.Status?.Phase ?? string.Empty;
                return (IEnumerable<ITableCellContent>)
                    new ITableCellContent[]
                    {
                        new LinkContent
                        {
                            ResourceName = pod.Name(),
                            ResourceType = ResourceType.Pod,
                        },
                        new LinkContent
                        {
                            ResourceName = pod.Namespace(),
                            ResourceType = ResourceType.Namespace,
                        },
                        (TextContent)ready,
                        (TextContent)cpu,
                        (TextContent)memory,
                        (StatusCellContent)status,
                    };
            });
    }
}
