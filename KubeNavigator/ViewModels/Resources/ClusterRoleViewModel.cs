using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class ClusterRoleViewModel : KubernetesResourceViewModel
{
    public ClusterRoleViewModel(V1ClusterRole resource, ClusterViewModel cluster)
        : base(resource, ResourceType.ClusterRole, cluster) { }

    public V1ClusterRole ClusterRole => (V1ClusterRole)Resource;

    public static readonly ImmutableArray<ResourceColumn> ClusterRoleColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => ClusterRoleColumns;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetClusterRoleRows()] },
        };

        var ruleRows = GetRuleRows();
        if (ruleRows.Count > 0)
        {
            sections.Add(
                new DetailsSection { Header = "Rules", Rows = ruleRows }
            );
        }

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private List<IDetailsRow> GetClusterRoleRows()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = ClusterRole.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = ClusterRole.Name() },
            },
        };

        if (ClusterRole.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. ClusterRole.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (ClusterRole.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. ClusterRole.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        return rows;
    }

    private List<IDetailsRow> GetRuleRows()
    {
        var rules = ClusterRole.Rules;
        if (rules is null || rules.Count == 0)
            return [];

        return rules
            .Select(rule => (IDetailsRow)new GroupRow
            {
                Rows =
                [
                    new HeaderedRow
                    {
                        Header = "Resources",
                        Content = new TextContent
                        {
                            Value = string.Join(", ", rule.Resources ?? []),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Verbs",
                        Content = new TextContent
                        {
                            Value = string.Join(", ", rule.Verbs ?? []),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "API Groups",
                        Content = new TextContent
                        {
                            Value = rule.ApiGroups is { Count: > 0 }
                                ? string.Join(", ", rule.ApiGroups.Select(g => string.IsNullOrEmpty(g) ? "*" : g))
                                : "*",
                        },
                    },
                ],
            })
            .ToList();
    }
}
