using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class RoleViewModel : KubernetesResourceViewModel
{
    public RoleViewModel(V1Role resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Role, cluster) { }

    public V1Role Role => (V1Role)Resource;

    public static readonly ImmutableArray<ResourceColumn> RoleColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => RoleColumns;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetRoleInfoRows()] },
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

    private List<IDetailsRow> GetRoleInfoRows()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = Role.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = Role.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = Role.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (Role.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Role.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (Role.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Role.Metadata.Labels.Select(l =>
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
        var rules = Role.Rules;
        if (rules is null || rules.Count == 0)
            return [];

        return rules
            .Select(rule =>
            {
                var rows = new List<IDetailsRow>
                {
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
                                ? string.Join(
                                    ", ",
                                    rule.ApiGroups.Select(g =>
                                        string.IsNullOrEmpty(g) ? "*" : g
                                    )
                                )
                                : "*",
                        },
                    },
                };

                if (rule.ResourceNames is { Count: > 0 })
                {
                    rows.Add(new HeaderedRow
                    {
                        Header = "Resource Names",
                        Content = new TextContent
                        {
                            Value = string.Join(", ", rule.ResourceNames),
                        },
                    });
                }

                return (IDetailsRow)new GroupRow { Rows = rows };
            })
            .ToList();
    }
}
