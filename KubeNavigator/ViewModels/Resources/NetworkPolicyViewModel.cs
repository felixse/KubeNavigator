using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class NetworkPolicyViewModel : KubernetesResourceViewModel
{
    public NetworkPolicyViewModel(V1NetworkPolicy resource, ClusterViewModel cluster)
        : base(resource, ResourceType.NetworkPolicy, cluster) { }

    public V1NetworkPolicy NetworkPolicy => (V1NetworkPolicy)Resource;

    public static readonly ImmutableArray<ResourceColumn> NetworkPolicyColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Policy Types",
            vm => ((NetworkPolicyViewModel)vm).PolicyTypes,
            PropertyName: nameof(PolicyTypes)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => NetworkPolicyColumns;

    public string PolicyTypes =>
        NetworkPolicy.Spec?.PolicyTypes is { Count: > 0 } types
            ? string.Join(", ", types)
            : string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        var ingressRows = GetIngressRows();
        if (ingressRows.Count > 0)
        {
            sections.Add(new DetailsSection { Header = "Ingress", Rows = ingressRows });
        }

        var egressRows = GetEgressRows();
        if (egressRows.Count > 0)
        {
            sections.Add(new DetailsSection { Header = "Egress", Rows = egressRows });
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
                    Value = NetworkPolicy.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = NetworkPolicy.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = NetworkPolicy.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (NetworkPolicy.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. NetworkPolicy.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (NetworkPolicy.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. NetworkPolicy.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        var podSelector = NetworkPolicy.Spec?.PodSelector?.MatchLabels;
        rows.Add(new HeaderedRow
        {
            Header = "Pod Selector",
            Content = podSelector is { Count: > 0 }
                ? new CollectionContent
                {
                    Items =
                    [
                        .. podSelector.Select(s =>
                            new TextCollectionElement { Value = $"{s.Key}={s.Value}" }
                        ),
                    ],
                }
                : new TextContent { Value = "<all pods>" },
        });

        return rows;
    }

    private List<IDetailsRow> GetIngressRows()
    {
        var ingress = NetworkPolicy.Spec?.Ingress;
        if (ingress is not { Count: > 0 })
            return [];

        var rows = new List<IDetailsRow>();
        foreach (var rule in ingress)
        {
            if (rule.Ports is { Count: > 0 })
            {
                rows.Add(new HeaderedRow
                {
                    Header = "Ports",
                    Content = new TextContent
                    {
                        Value = string.Join(
                            ", ",
                            rule.Ports.Select(p =>
                                $"{p.Port?.Value ?? "all"}/{p.Protocol ?? "TCP"}"
                            )
                        ),
                    },
                });
            }
        }

        return rows;
    }

    private List<IDetailsRow> GetEgressRows()
    {
        var egress = NetworkPolicy.Spec?.Egress;
        if (egress is not { Count: > 0 })
            return [];

        var rows = new List<IDetailsRow>();
        foreach (var rule in egress)
        {
            if (rule.Ports is { Count: > 0 })
            {
                rows.Add(new HeaderedRow
                {
                    Header = "Ports",
                    Content = new TextContent
                    {
                        Value = string.Join(
                            ", ",
                            rule.Ports.Select(p =>
                                $"{p.Port?.Value ?? "all"}/{p.Protocol ?? "TCP"}"
                            )
                        ),
                    },
                });
            }
        }

        return rows;
    }
}
