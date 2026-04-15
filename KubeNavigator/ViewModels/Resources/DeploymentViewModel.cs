using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class DeploymentViewModel : KubernetesResourceViewModel
{
    public DeploymentViewModel(V1Deployment resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Deployment, cluster) { }

    public V1Deployment Deployment => (V1Deployment)Resource;

    public static readonly ImmutableArray<ResourceColumn> DeploymentColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Ready", vm => ((DeploymentViewModel)vm).Ready, PropertyName: nameof(Ready)),
        new("Desired", vm => ((DeploymentViewModel)vm).Desired, PropertyName: nameof(Desired)),
        new("Updated", vm => ((DeploymentViewModel)vm).Updated, PropertyName: nameof(Updated)),
        new("Available", vm => ((DeploymentViewModel)vm).Available, PropertyName: nameof(Available)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new("Conditions", vm => ((DeploymentViewModel)vm).Conditions, ResourceColumnType.Conditions, nameof(Conditions)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => DeploymentColumns;

    public string Ready =>
        $"{Deployment.Status?.ReadyReplicas ?? 0}/{Deployment.Status?.Replicas ?? 0}";

    public int? Desired => Deployment.Spec?.Replicas;

    public int? Updated => Deployment.Status?.UpdatedReplicas;

    public int? Available => Deployment.Status?.AvailableReplicas;

    public List<string> Conditions =>
        Deployment.Status?.Conditions is null
            ? []
            : Deployment
                .Status.Conditions.Where(c => c.Status == "True")
                .Select(c => c.Type)
                .ToList();

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetDeploymentRows()] },
        };

        var revisionRows = await GetReplicaSetRowsAsync();
        sections.Add(
            new DetailsSection
            {
                Header = "Deploy Revisions",
                Rows =
                [
                    new FullWidthRow
                    {
                        Content = new TableContent
                        {
                            IsExpandable = false,
                            Columns = ["Name", "Namespace", "Pods", "Age"],
                            Rows = revisionRows,
                        },
                    },
                ],
            }
        );

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
                            IsExpandable = false,
                            Columns = ["Name", "Node", "Namespace", "Ready", "CPU", "Memory", "Status"],
                            Rows = podRows,
                        },
                    },
                ],
            }
        );

        sections.Add(events);

        return sections;
    }

    private IEnumerable<IDetailsRow> GetDeploymentRows()
    {
        yield return new HeaderedRow { Header = "Created", Content = new TextContent { Value = Deployment.CreationTimestamp().ToString() } };
        yield return new HeaderedRow { Header = "Name", Content = new TextContent { Value = Deployment.Name() } };
        yield return new HeaderedRow { Header = "Namespace", Content = new LinkContent { ResourceName = Resource.Namespace(), ResourceType = ResourceType.Namespace } };

        yield return new HeaderedRow
        {
            Header = "Labels",
            Content = new CollectionContent
            {
                Items = [.. Deployment.Metadata.Labels?.Select(l => new TextCollectionElement { Value = $"{l.Key}={l.Value}" }) ?? []],
            },
        };

        if (Deployment.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items = [.. Deployment.Metadata.Annotations?.Select(l => new TextCollectionElement { Value = $"{l.Key}={l.Value}" }) ?? []],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Replicas",
            Content = new TextContent
            {
                Value = $"{Deployment.Status?.ReadyReplicas ?? 0} ready / {Deployment.Spec?.Replicas ?? 0} desired",
            },
        };

        yield return new HeaderedRow
        {
            Header = "Selector",
            Content = new CollectionContent
            {
                Items = [.. Deployment.Spec?.Selector?.MatchLabels?.Select(s => new TextCollectionElement { Value = $"{s.Key}={s.Value}" }) ?? []],
            },
        };

        yield return new HeaderedRow
        {
            Header = "Node Selector",
            Content = new CollectionContent
            {
                Items = [.. Deployment.Spec?.Template?.Spec?.NodeSelector?.Select(n => new TextCollectionElement { Value = $"{n.Key}: {n.Value}" }) ?? []],
            },
        };

        yield return new HeaderedRow
        {
            Header = "Strategy Type",
            Content = new TextContent { Value = Deployment.Spec?.Strategy?.Type ?? string.Empty },
        };

        yield return new HeaderedRow
        {
            Header = "Conditions",
            Content = new CollectionContent
            {
                Items =
                [
                    .. Deployment.Status?.Conditions?.Select(c => new ConditionCollectionElement
                    {
                        Type = c.Type,
                        Status = c.Status,
                        Message = c.Message,
                        Reason = c.Reason,
                        LastTransitionTime = c.LastTransitionTime,
                    }) ?? [],
                ],
            },
        };

        yield return new HeaderedRow
        {
            Header = "Tolerations",
            Content = new TableContent
            {
                IsExpandable = true,
                Columns = ["Key", "Operator", "Value", "Effect", "Seconds"],
                Rows = Deployment.Spec?.Template?.Spec?.Tolerations?.Select(t =>
                    (IEnumerable<ITableCellContent>)new TextContent[]
                    {
                        t.Key,
                        t.OperatorProperty,
                        t.Value,
                        t.Effect,
                        t.TolerationSeconds?.ToString() ?? string.Empty,
                    }
                )
                    ?? [],
            },
        };

        var affinityYaml =
            Deployment.Spec?.Template?.Spec?.Affinity != null
                ? KubernetesYaml.Serialize(Deployment.Spec.Template.Spec.Affinity)
                : string.Empty;

        yield return new HeaderedRow
        {
            Header = "Affinities",
            Content = new EditorContent { Value = affinityYaml },
        };
    }

    private async Task<IEnumerable<IEnumerable<ITableCellContent>>> GetReplicaSetRowsAsync()
    {
        var replicaSets = await Cluster.GetResourcesAsync(ResourceType.ReplicaSet);
        return replicaSets
            .Where(rs =>
                rs.Resource is V1ReplicaSet replicaSet
                && replicaSet.Metadata.OwnerReferences?.Any(o =>
                    o.Kind == "Deployment" && o.Name == Deployment.Name()
                ) == true
                && rs.Namespace == Deployment.Namespace()
            )
            .Select(rs =>
            {
                var replicaSet = (V1ReplicaSet)rs.Resource;
                return (IEnumerable<ITableCellContent>)new TextContent[]
                {
                    replicaSet.Name(),
                    replicaSet.Namespace(),
                    $"{replicaSet.Status?.ReadyReplicas ?? 0}/{replicaSet.Status?.Replicas ?? 0}",
                    replicaSet.Metadata.CreationTimestamp?.ToString() ?? string.Empty,
                };
            });
    }

    private async Task<IEnumerable<IEnumerable<ITableCellContent>>> GetPodRowsAsync()
    {
        var pods = await Cluster.GetResourcesAsync(ResourceType.Pod);
        var matchLabels = Deployment.Spec?.Selector?.MatchLabels;

        if (matchLabels is null || matchLabels.Count == 0)
            return [];

        return pods.Where(p =>
                p.Resource is V1Pod pod
                && pod.Namespace() == Deployment.Namespace()
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
                var cpu =
                    pod.Spec?.Containers?.FirstOrDefault()
                        ?.Resources?.Requests?.TryGetValue("cpu", out var cpuReq) == true
                        ? cpuReq.ToString()
                        : "-";
                var memory =
                    pod.Spec?.Containers?.FirstOrDefault()
                        ?.Resources?.Requests?.TryGetValue("memory", out var memReq) == true
                        ? memReq.ToString()
                        : "-";
                var status = pod.Metadata.DeletionTimestamp is not null
                    ? "Terminating"
                    : pod.Status?.Phase ?? string.Empty;
                return (IEnumerable<ITableCellContent>)new TextContent[]
                {
                    pod.Name(),
                    pod.Spec?.NodeName ?? string.Empty,
                    pod.Namespace(),
                    ready,
                    cpu,
                    memory,
                    status,
                };
            });
    }
}
