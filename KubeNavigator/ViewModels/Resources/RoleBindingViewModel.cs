using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class RoleBindingViewModel : KubernetesResourceViewModel
{
    public RoleBindingViewModel(V1RoleBinding resource, ClusterViewModel cluster)
        : base(resource, ResourceType.RoleBinding, cluster) { }

    public V1RoleBinding RoleBinding => (V1RoleBinding)Resource;

    public static readonly ImmutableArray<ResourceColumn> RoleBindingColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Role",
            vm => ((RoleBindingViewModel)vm).RoleName,
            PropertyName: nameof(RoleName)
        ),
        new(
            "Types",
            vm => ((RoleBindingViewModel)vm).SubjectTypes,
            PropertyName: nameof(SubjectTypes)
        ),
        new(
            "Bindings",
            vm => ((RoleBindingViewModel)vm).BindingNames,
            PropertyName: nameof(BindingNames)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => RoleBindingColumns;

    public string RoleName => RoleBinding.RoleRef?.Name ?? string.Empty;

    public string SubjectTypes =>
        RoleBinding.Subjects is { Count: > 0 }
            ? string.Join(", ", RoleBinding.Subjects.Select(s => s.Kind).Distinct())
            : string.Empty;

    public string BindingNames =>
        RoleBinding.Subjects is { Count: > 0 }
            ? string.Join(", ", RoleBinding.Subjects.Select(s => s.Name))
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

        if (RoleBinding.Subjects is { Count: > 0 })
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
                    Value = RoleBinding.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = RoleBinding.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = RoleBinding.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (RoleBinding.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. RoleBinding.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (RoleBinding.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. RoleBinding.Metadata.Labels.Select(l =>
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
        var roleRef = RoleBinding.RoleRef;
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
        if (RoleBinding.Subjects is not { Count: > 0 })
            return [];

        return RoleBinding.Subjects.Select(s =>
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
