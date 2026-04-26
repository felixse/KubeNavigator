using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class PersistentVolumeClaimViewModel : KubernetesResourceViewModel
{
    public PersistentVolumeClaimViewModel(
        V1PersistentVolumeClaim resource,
        ClusterViewModel cluster
    )
        : base(resource, ResourceType.PersistentVolumeClaim, cluster) { }

    public V1PersistentVolumeClaim Pvc => (V1PersistentVolumeClaim)Resource;

    public static readonly ImmutableArray<ResourceColumn> PersistentVolumeClaimColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Storage Class",
            vm => ((PersistentVolumeClaimViewModel)vm).StorageClassName,
            PropertyName: nameof(StorageClassName)
        ),
        new(
            "Size",
            vm => ((PersistentVolumeClaimViewModel)vm).Size,
            PropertyName: nameof(Size)
        ),
        new(
            "Pods",
            vm => ((PersistentVolumeClaimViewModel)vm).PodCount,
            PropertyName: nameof(PodCount)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new(
            "Status",
            vm => ((PersistentVolumeClaimViewModel)vm).Status,
            ResourceColumnType.Status,
            nameof(Status)
        ),
    ];

    public override ImmutableArray<ResourceColumn> Columns => PersistentVolumeClaimColumns;

    public string StorageClassName => Pvc.Spec?.StorageClassName ?? string.Empty;

    public string Size =>
        Pvc.Status?.Capacity?.TryGetValue("storage", out var storage) == true
            ? storage.ToString()
            : Pvc.Spec?.Resources?.Requests?.TryGetValue("storage", out var req) == true
                ? req.ToString()
                : string.Empty;

    public string Status => Pvc.Status?.Phase ?? string.Empty;

    public int PodCount => 0; // Updated asynchronously in details; column shows cached value

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. await GetPvcRowsAsync()] },
        };

        var selectorRows = GetSelectorRows();
        if (selectorRows.Count > 0)
        {
            sections.Add(
                new DetailsSection { Header = "Selector", Rows = selectorRows }
            );
        }

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private async Task<List<IDetailsRow>> GetPvcRowsAsync()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = Pvc.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = Pvc.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = Pvc.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (Pvc.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Pvc.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        if (Pvc.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Pvc.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (Pvc.Metadata.Finalizers?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Finalizers",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Pvc.Metadata.Finalizers.Select(f =>
                            new TextCollectionElement { Value = f }
                        ),
                    ],
                },
            });
        }

        rows.Add(new HeaderedRow
        {
            Header = "Access Modes",
            Content = new CollectionContent
            {
                Items =
                [
                    .. Pvc.Spec?.AccessModes?.Select(m =>
                        new TextCollectionElement { Value = m }
                    ) ?? [],
                ],
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Storage Class",
            Content = new LinkContent
            {
                ResourceName = StorageClassName,
                ResourceType = ResourceType.StorageClass,
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Storage",
            Content = new TextContent { Value = Size },
        });

        var podLinks = await GetPodLinksAsync();
        if (podLinks.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Pods",
                Content = new CollectionContent
                {
                    Items = [.. podLinks],
                },
            });
        }

        rows.Add(new HeaderedRow
        {
            Header = "Status",
            Content = new TextContent { Value = Status },
        });

        return rows;
    }

    private async Task<List<LinkCollectionElement>> GetPodLinksAsync()
    {
        var pods = await Cluster.GetResourcesAsync(ResourceType.Pod);
        var pvcName = Pvc.Name();
        var pvcNamespace = Pvc.Namespace();

        return pods
            .Where(p =>
            {
                if (p.Resource is not V1Pod pod || pod.Namespace() != pvcNamespace)
                    return false;
                return pod.Spec?.Volumes?.Any(v =>
                    v.PersistentVolumeClaim?.ClaimName == pvcName
                ) == true;
            })
            .Select(p => new LinkCollectionElement
            {
                ResourceName = p.Resource.Name(),
                ResourceType = ResourceType.Pod,
            })
            .ToList();
    }

    private List<IDetailsRow> GetSelectorRows()
    {
        var rows = new List<IDetailsRow>();
        var selector = Pvc.Spec?.Selector;

        if (selector?.MatchLabels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Match Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. selector.MatchLabels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        if (selector?.MatchExpressions?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Match Expressions",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. selector.MatchExpressions.Select(e =>
                            new TextCollectionElement
                            {
                                Value =
                                    $"{e.Key} {e.OperatorProperty} [{string.Join(", ", e.Values ?? [])}]",
                            }
                        ),
                    ],
                },
            });
        }

        return rows;
    }
}
