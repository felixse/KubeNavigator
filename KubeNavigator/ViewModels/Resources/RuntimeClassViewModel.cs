using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class RuntimeClassViewModel : KubernetesResourceViewModel
{
    public RuntimeClassViewModel(V1RuntimeClass resource, ClusterViewModel cluster)
        : base(resource, ResourceType.RuntimeClass, cluster) { }

    public V1RuntimeClass RuntimeClass => (V1RuntimeClass)Resource;

    public static readonly ImmutableArray<ResourceColumn> RuntimeClassColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Handler",
            vm => ((RuntimeClassViewModel)vm).Handler,
            PropertyName: nameof(Handler)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => RuntimeClassColumns;

    public string Handler => RuntimeClass.Handler ?? string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetRuntimeClassRows()] },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetRuntimeClassRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = RuntimeClass.CreationTimestamp().ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = RuntimeClass.Name() },
        };
        yield return new HeaderedRow
        {
            Header = "Namespace",
            Content = new LinkContent
            {
                ResourceName = Resource.Namespace(),
                ResourceType = ResourceType.Namespace,
            },
        };

        yield return new HeaderedRow
        {
            Header = "Labels",
            Content = new CollectionContent
            {
                Items =
                [
                    .. RuntimeClass.Metadata.Labels?.Select(l => new TextCollectionElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            },
        };

        if (RuntimeClass.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. RuntimeClass.Metadata.Annotations?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Handler",
            Content = new TextContent { Value = Handler },
        };
    }
}
