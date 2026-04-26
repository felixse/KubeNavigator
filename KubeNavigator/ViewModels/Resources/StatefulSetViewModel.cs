using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class StatefulSetViewModel : KubernetesResourceViewModel
{
    public StatefulSetViewModel(V1StatefulSet resource, ClusterViewModel cluster)
        : base(resource, ResourceType.StatefulSet, cluster) { }

    public V1StatefulSet StatefulSet => (V1StatefulSet)Resource;

    public static readonly ImmutableArray<ResourceColumn> StatefulSetColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Ready", vm => ((StatefulSetViewModel)vm).Ready, PropertyName: nameof(Ready)),
        new("Desired", vm => ((StatefulSetViewModel)vm).Desired, PropertyName: nameof(Desired)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => StatefulSetColumns;

    public string Ready =>
        $"{StatefulSet.Status?.ReadyReplicas ?? 0}/{StatefulSet.Spec?.Replicas ?? 0}";

    public int Desired => StatefulSet.Spec?.Replicas ?? 0;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. await GetStatefulSetRowsAsync()] },
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

    private async Task<List<IDetailsRow>> GetStatefulSetRowsAsync()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = StatefulSet.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = StatefulSet.Name() },
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
                        .. StatefulSet.Metadata.Labels?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            },
        };

        if (StatefulSet.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. StatefulSet.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
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
                    .. StatefulSet.Spec?.Selector?.MatchLabels?.Select(s =>
                        new TextCollectionElement { Value = $"{s.Key}={s.Value}" }
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
                    .. StatefulSet.Spec?.Template?.Spec?.Containers?.Select(c =>
                        new TextCollectionElement { Value = c.Image }
                    ) ?? [],
                ],
            },
        });

        var affinity = StatefulSet.Spec?.Template?.Spec?.Affinity;
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

        var matchLabels = StatefulSet.Spec?.Selector?.MatchLabels;
        if (matchLabels is null || matchLabels.Count == 0)
            return string.Empty;

        var statusGroups = pods
            .Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == StatefulSet.Namespace()
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
        var matchLabels = StatefulSet.Spec?.Selector?.MatchLabels;

        if (matchLabels is null || matchLabels.Count == 0)
            return [];

        return pods
            .Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == StatefulSet.Namespace()
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
