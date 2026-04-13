using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

internal partial class NamespaceViewModel : KubernetesResourceViewModel
{
    public NamespaceViewModel(IKubernetesObject<V1ObjectMeta> resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Namespace, cluster) { }

    public V1Namespace Namespace => (V1Namespace)Resource;

    public static readonly ImmutableArray<ResourceColumn> NamespaceColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Labels", vm => ((NamespaceViewModel)vm).Labels, PropertyName: nameof(Labels)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new("Status", vm => ((NamespaceViewModel)vm).Status, ResourceColumnType.Status, nameof(Status)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => NamespaceColumns;

    public string Labels =>
        Resource.Metadata?.Labels != null
            ? string.Join(", ", Resource.Metadata.Labels.Select(kv => $"{kv.Key}={kv.Value}"))
            : string.Empty;

    public string Status => Namespace.Status?.Phase ?? "Unknown";

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        return
        [
            new DetailsSection
            {
                Items =
                [
                    new DetailsTextItem
                    {
                        Title = "Created",
                        Value = Resource.CreationTimestamp().ToString(),
                    },
                    new DetailsTextItem { Title = "Name", Value = Resource.Name() },
                    new DetailsCollectionItem
                    {
                        Title = "Labels",
                        Items =
                        [
                            .. Resource.Metadata.Labels?.Select(
                                a => new DetailsCollectionItemElement
                                {
                                    Value = $"{a.Key}={a.Value}",
                                }
                            ) ?? [],
                        ],
                    },
                    new DetailsTextItem
                    {
                        Title = "Status",
                        Value = Status,
                        ValueColor = Status switch
                        {
                            "Active" => Category.Success,
                            _ => Category.Default,
                        },
                    },
                ],
            },
            events,
        ];
    }
}
