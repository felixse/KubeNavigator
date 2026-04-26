using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class StorageClassViewModel : KubernetesResourceViewModel
{
    public StorageClassViewModel(V1StorageClass resource, ClusterViewModel cluster)
        : base(resource, ResourceType.StorageClass, cluster) { }

    public V1StorageClass StorageClass => (V1StorageClass)Resource;

    public static readonly ImmutableArray<ResourceColumn> StorageClassColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Provisioner",
            vm => ((StorageClassViewModel)vm).Provisioner,
            PropertyName: nameof(Provisioner)
        ),
        new(
            "Reclaim Policy",
            vm => ((StorageClassViewModel)vm).ReclaimPolicy,
            PropertyName: nameof(ReclaimPolicy)
        ),
        new(
            "Default",
            vm => ((StorageClassViewModel)vm).IsDefault,
            PropertyName: nameof(IsDefault)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => StorageClassColumns;

    public string Provisioner => StorageClass.Provisioner ?? string.Empty;

    public string ReclaimPolicy => StorageClass.ReclaimPolicy ?? string.Empty;

    public string IsDefault =>
        StorageClass.Metadata.Annotations?.TryGetValue(
            "storageclass.kubernetes.io/is-default-class",
            out var val
        ) == true && val == "true"
            ? "Yes"
            : "No";

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. await GetStorageClassRowsAsync()] },
        };

        var pvRows = await GetPersistentVolumeRowsAsync();
        sections.Add(
            new DetailsSection
            {
                Header = "Persistent Volumes",
                Rows =
                [
                    new FullWidthRow
                    {
                        Content = new TableContent
                        {
                            Columns = ["Name", "Capacity", "Status"],
                            Rows = pvRows,
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

    private async Task<List<IDetailsRow>> GetStorageClassRowsAsync()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = StorageClass.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = StorageClass.Name() },
            },
        };

        if (StorageClass.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. StorageClass.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        rows.Add(new HeaderedRow
        {
            Header = "Provisioner",
            Content = new TextContent { Value = Provisioner },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Volume Binding Mode",
            Content = new TextContent
            {
                Value = StorageClass.VolumeBindingMode ?? string.Empty,
            },
        });

        rows.Add(new HeaderedRow
        {
            Header = "Reclaim Policy",
            Content = new TextContent { Value = ReclaimPolicy },
        });

        if (StorageClass.MountOptions?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Mount Options",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. StorageClass.MountOptions.Select(m => new TextCollectionElement
                        {
                            Value = m,
                        }),
                    ],
                },
            });
        }
        else
        {
            rows.Add(new HeaderedRow
            {
                Header = "Mount Options",
                Content = new TextContent { Value = string.Empty },
            });
        }

        return rows;
    }

    private async Task<IEnumerable<IEnumerable<ITableCellContent>>> GetPersistentVolumeRowsAsync()
    {
        var pvs = await Cluster.GetResourcesAsync(ResourceType.PersistentVolume);

        return pvs
            .Where(p =>
                p.Resource is V1PersistentVolume pv
                && pv.Spec?.StorageClassName == StorageClass.Name()
            )
            .Select(p =>
            {
                var pv = (V1PersistentVolume)p.Resource;
                var capacity =
                    pv.Spec?.Capacity?.TryGetValue("storage", out var storage) == true
                        ? storage.ToString()
                        : string.Empty;
                var status = pv.Status?.Phase ?? string.Empty;
                return (IEnumerable<ITableCellContent>)
                    new ITableCellContent[]
                    {
                        new LinkContent
                        {
                            ResourceName = pv.Name(),
                            ResourceType = ResourceType.PersistentVolume,
                        },
                        (TextContent)capacity,
                        (StatusCellContent)status,
                    };
            });
    }
}
