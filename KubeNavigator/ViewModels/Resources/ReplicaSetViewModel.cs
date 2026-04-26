using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class ReplicaSetViewModel : KubernetesResourceViewModel
{
    public ReplicaSetViewModel(V1ReplicaSet resource, ClusterViewModel cluster)
        : base(resource, ResourceType.ReplicaSet, cluster) { }

    public V1ReplicaSet ReplicaSet => (V1ReplicaSet)Resource;

    public static readonly ImmutableArray<ResourceColumn> ReplicaSetColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Desired", vm => ((ReplicaSetViewModel)vm).Desired, PropertyName: nameof(Desired)),
        new("Current", vm => ((ReplicaSetViewModel)vm).Current, PropertyName: nameof(Current)),
        new("Ready", vm => ((ReplicaSetViewModel)vm).Ready, PropertyName: nameof(Ready)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => ReplicaSetColumns;

    public int Desired => ReplicaSet.Spec?.Replicas ?? 0;

    public int Current => ReplicaSet.Status?.Replicas ?? 0;

    public int Ready => ReplicaSet.Status?.ReadyReplicas ?? 0;

    public string ControlledBy =>
        ReplicaSet.Metadata.OwnerReferences?.FirstOrDefault()?.Name ?? string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. await GetReplicaSetRowsAsync()] },
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

    private async Task<List<IDetailsRow>> GetReplicaSetRowsAsync()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent { Value = ReplicaSet.CreationTimestamp().ToString() },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = ReplicaSet.Name() },
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
                        .. ReplicaSet.Metadata.Labels?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            },
        };

        if (ReplicaSet.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. ReplicaSet.Metadata.Annotations.Select(a => new TextCollectionElement
                        {
                            Value = $"{a.Key}={a.Value}",
                        }),
                    ],
                },
            });
        }

        var ownerRef = ReplicaSet.Metadata.OwnerReferences?.FirstOrDefault();
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

        rows.Add(new HeaderedRow
        {
            Header = "Selector",
            Content = new CollectionContent
            {
                Items =
                [
                    .. ReplicaSet.Spec?.Selector?.MatchLabels?.Select(s => new TextCollectionElement
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
                    .. ReplicaSet.Spec?.Template?.Spec?.NodeSelector?.Select(
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
                    .. ReplicaSet.Spec?.Template?.Spec?.Containers?.Select(c =>
                        new TextCollectionElement { Value = c.Image }
                    ) ?? [],
                ],
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Replicas",
            Content = new TextContent
            {
                Value = $"{Current} current / {Desired} desired",
            },
        });

        var tolerationsTable = new TableContent
        {
            Columns = ["Key", "Operator", "Value", "Effect", "Seconds"],
            Rows =
                ReplicaSet.Spec?.Template?.Spec?.Tolerations?.Select(t =>
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

        var affinity = ReplicaSet.Spec?.Template?.Spec?.Affinity;
        var affinityYaml = affinity != null
            ? KubernetesYaml.Serialize(affinity)
            : string.Empty;

        rows.Add(new ExpandableRow
        {
            Header = "Affinities",
            Summary = new object?[]
            {
                affinity?.PodAffinity,
                affinity?.NodeAffinity,
                affinity?.PodAntiAffinity,
            }
                .Count(a => a != null)
                .ToString(),
            Content = new EditorContent { Value = affinityYaml, IsReadOnly = true },
        });

        var podStatuses = await GetPodStatusSummaryAsync();
        rows.Add(new HeaderedRow
        {
            Header = "Pod Status",
            Content = new TextContent { Value = podStatuses },
        });

        return rows;
    }

    private async Task<string> GetPodStatusSummaryAsync()
    {
        var pods = await Cluster.GetResourcesAsync(ResourceType.Pod);

        var matchLabels = ReplicaSet.Spec?.Selector?.MatchLabels;
        if (matchLabels is null || matchLabels.Count == 0)
            return string.Empty;

        var statusGroups = pods
            .Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == ReplicaSet.Namespace()
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
        var matchLabels = ReplicaSet.Spec?.Selector?.MatchLabels;

        if (matchLabels is null || matchLabels.Count == 0)
            return [];

        return pods
            .Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == ReplicaSet.Namespace()
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
