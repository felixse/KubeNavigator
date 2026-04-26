using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class LeaseViewModel : KubernetesResourceViewModel
{
    public LeaseViewModel(V1Lease resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Lease, cluster) { }

    public V1Lease Lease => (V1Lease)Resource;

    public static readonly ImmutableArray<ResourceColumn> LeaseColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Holder",
            vm => ((LeaseViewModel)vm).Holder,
            PropertyName: nameof(Holder)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => LeaseColumns;

    public string Holder => Lease.Spec?.HolderIdentity ?? "-";

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetInfoRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = Lease.CreationTimestamp().ToString() },
        };

        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = Lease.Name() },
        };

        yield return new HeaderedRow
        {
            Header = "Namespace",
            Content = new LinkContent
            {
                ResourceName = Lease.Namespace(),
                ResourceType = ResourceType.Namespace,
            },
        };

        if (Lease.Metadata.Labels?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Lease.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            };
        }

        if (Lease.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Lease.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Holder Identity",
            Content = new TextContent { Value = Lease.Spec?.HolderIdentity ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Lease Duration Seconds",
            Content = new TextContent { Value = Lease.Spec?.LeaseDurationSeconds?.ToString() ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Lease Transitions",
            Content = new TextContent { Value = Lease.Spec?.LeaseTransitions?.ToString() ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Acquire Time",
            Content = new TextContent { Value = Lease.Spec?.AcquireTime?.ToString() ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Renew Time",
            Content = new TextContent { Value = Lease.Spec?.RenewTime?.ToString() ?? "-" },
        };
    }
}
