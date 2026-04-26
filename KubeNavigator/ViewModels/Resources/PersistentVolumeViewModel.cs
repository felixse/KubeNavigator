using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class PersistentVolumeViewModel : KubernetesResourceViewModel
{
    public PersistentVolumeViewModel(V1PersistentVolume resource, ClusterViewModel cluster)
        : base(resource, ResourceType.PersistentVolume, cluster) { }

    public V1PersistentVolume PersistentVolume => (V1PersistentVolume)Resource;

    public static readonly ImmutableArray<ResourceColumn> PersistentVolumeColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Storage Class",
            vm => ((PersistentVolumeViewModel)vm).StorageClassName,
            PropertyName: nameof(StorageClassName)
        ),
        new(
            "Capacity",
            vm => ((PersistentVolumeViewModel)vm).Capacity,
            PropertyName: nameof(Capacity)
        ),
        new(
            "Claim",
            vm => ((PersistentVolumeViewModel)vm).Claim,
            PropertyName: nameof(Claim)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new(
            "Status",
            vm => ((PersistentVolumeViewModel)vm).Status,
            ResourceColumnType.Status,
            nameof(Status)
        ),
    ];

    public override ImmutableArray<ResourceColumn> Columns => PersistentVolumeColumns;

    public string StorageClassName => PersistentVolume.Spec?.StorageClassName ?? string.Empty;

    public string Capacity =>
        PersistentVolume.Spec?.Capacity?.TryGetValue("storage", out var storage) == true
            ? storage.ToString()
            : string.Empty;

    public string Claim =>
        PersistentVolume.Spec?.ClaimRef is { } cr
            ? $"{cr.NamespaceProperty}/{cr.Name}"
            : string.Empty;

    public string Status => PersistentVolume.Status?.Phase ?? string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetPersistentVolumeRows()] },
        };

        var claimRef = PersistentVolume.Spec?.ClaimRef;
        if (claimRef is not null)
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Claim",
                    Rows =
                    [
                        new HeaderedRow
                        {
                            Header = "Type",
                            Content = new TextContent
                            {
                                Value = claimRef.Kind ?? "PersistentVolumeClaim",
                            },
                        },
                        new HeaderedRow
                        {
                            Header = "Name",
                            Content = new LinkContent
                            {
                                ResourceName = claimRef.Name,
                                ResourceType = ResourceType.PersistentVolumeClaim,
                            },
                        },
                        new HeaderedRow
                        {
                            Header = "Namespace",
                            Content = new LinkContent
                            {
                                ResourceName = claimRef.NamespaceProperty,
                                ResourceType = ResourceType.Namespace,
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

    private List<IDetailsRow> GetPersistentVolumeRows()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = PersistentVolume.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = PersistentVolume.Name() },
            },
        };

        if (PersistentVolume.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. PersistentVolume.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (PersistentVolume.Metadata.Finalizers?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Finalizers",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. PersistentVolume.Metadata.Finalizers.Select(f =>
                            new TextCollectionElement { Value = f }
                        ),
                    ],
                },
            });
        }

        rows.Add(new HeaderedRow
        {
            Header = "Capacity",
            Content = new TextContent { Value = Capacity },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Access Modes",
            Content = new CollectionContent
            {
                Items =
                [
                    .. PersistentVolume.Spec?.AccessModes?.Select(m =>
                        new TextCollectionElement { Value = m }
                    ) ?? [],
                ],
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Reclaim Policy",
            Content = new TextContent
            {
                Value = PersistentVolume.Spec?.PersistentVolumeReclaimPolicy ?? string.Empty,
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

        return rows;
    }
}
