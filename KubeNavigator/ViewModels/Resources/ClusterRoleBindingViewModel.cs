using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class ClusterRoleBindingViewModel : KubernetesResourceViewModel
{
    public ClusterRoleBindingViewModel(V1ClusterRoleBinding resource, ClusterViewModel cluster)
        : base(resource, ResourceType.ClusterRoleBinding, cluster) { }

    public V1ClusterRoleBinding Binding => (V1ClusterRoleBinding)Resource;

    public static readonly ImmutableArray<ResourceColumn> ClusterRoleBindingColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Cluster Role",
            vm => ((ClusterRoleBindingViewModel)vm).ClusterRoleName,
            PropertyName: nameof(ClusterRoleName)
        ),
        new(
            "Types",
            vm => ((ClusterRoleBindingViewModel)vm).SubjectTypes,
            PropertyName: nameof(SubjectTypes)
        ),
        new(
            "Bindings",
            vm => ((ClusterRoleBindingViewModel)vm).BindingNames,
            PropertyName: nameof(BindingNames)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => ClusterRoleBindingColumns;

    public string ClusterRoleName => Binding.RoleRef?.Name ?? string.Empty;

    public string SubjectTypes =>
        Binding.Subjects is { Count: > 0 }
            ? string.Join(", ", Binding.Subjects.Select(s => s.Kind).Distinct())
            : string.Empty;

    public string BindingNames =>
        Binding.Subjects is { Count: > 0 }
            ? string.Join(", ", Binding.Subjects.Select(s => s.Name))
            : string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        sections.Add(
            new DetailsSection
            {
                Header = "Reference",
                Rows =
                [
                    new FullWidthRow
                    {
                        Content = new TableContent
                        {
                            Columns = ["Kind", "Name", "API Group"],
                            Rows = GetReferenceRows(),
                        },
                    },
                ],
            }
        );

        if (Binding.Subjects is { Count: > 0 })
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Bindings",
                    Rows =
                    [
                        new FullWidthRow
                        {
                            Content = new TableContent
                            {
                                Columns = ["Type", "Name", "Namespace"],
                                Rows = GetBindingRows(),
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
                    Value = Binding.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = Binding.Name() },
            },
        };

        if (Binding.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Binding.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (Binding.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Binding.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        return rows;
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetReferenceRows()
    {
        var roleRef = Binding.RoleRef;
        if (roleRef is null)
            return [];

        return
        [
            new ITableCellContent[]
            {
                (TextContent)(roleRef.Kind ?? string.Empty),
                new LinkContent
                {
                    ResourceName = roleRef.Name,
                    ResourceType = ResourceType.FromKind(roleRef.Kind ?? string.Empty),
                },
                (TextContent)(roleRef.ApiGroup ?? string.Empty),
            },
        ];
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetBindingRows()
    {
        if (Binding.Subjects is not { Count: > 0 })
            return [];

        return Binding.Subjects.Select(s =>
        {
            var nsContent = !string.IsNullOrEmpty(s.NamespaceProperty)
                ? (ITableCellContent)new LinkContent
                {
                    ResourceName = s.NamespaceProperty,
                    ResourceType = ResourceType.Namespace,
                }
                : (TextContent)string.Empty;

            return (IEnumerable<ITableCellContent>)
                new ITableCellContent[]
                {
                    (TextContent)(s.Kind ?? string.Empty),
                    (TextContent)(s.Name ?? string.Empty),
                    nsContent,
                };
        });
    }
}
