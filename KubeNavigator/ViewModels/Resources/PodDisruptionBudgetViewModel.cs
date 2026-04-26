using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class PodDisruptionBudgetViewModel : KubernetesResourceViewModel
{
    public PodDisruptionBudgetViewModel(V1PodDisruptionBudget resource, ClusterViewModel cluster)
        : base(resource, ResourceType.PodDisruptionBudget, cluster) { }

    public V1PodDisruptionBudget PDB => (V1PodDisruptionBudget)Resource;

    public static readonly ImmutableArray<ResourceColumn> PodDisruptionBudgetColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Min Available",
            vm => ((PodDisruptionBudgetViewModel)vm).MinAvailable,
            PropertyName: nameof(MinAvailable)
        ),
        new(
            "Max Unavailable",
            vm => ((PodDisruptionBudgetViewModel)vm).MaxUnavailable,
            PropertyName: nameof(MaxUnavailable)
        ),
        new(
            "Current Healthy",
            vm => ((PodDisruptionBudgetViewModel)vm).CurrentHealthy,
            PropertyName: nameof(CurrentHealthy)
        ),
        new(
            "Desired Healthy",
            vm => ((PodDisruptionBudgetViewModel)vm).DesiredHealthy,
            PropertyName: nameof(DesiredHealthy)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => PodDisruptionBudgetColumns;

    public string MinAvailable => PDB.Spec?.MinAvailable?.Value ?? "-";

    public string MaxUnavailable => PDB.Spec?.MaxUnavailable?.Value ?? "-";

    public string CurrentHealthy => PDB.Status?.CurrentHealthy.ToString() ?? "-";

    public string DesiredHealthy => PDB.Status?.DesiredHealthy.ToString() ?? "-";

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
            Content = new TextContent { Value = PDB.CreationTimestamp().ToString() },
        };

        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = PDB.Name() },
        };

        yield return new HeaderedRow
        {
            Header = "Namespace",
            Content = new LinkContent
            {
                ResourceName = PDB.Namespace(),
                ResourceType = ResourceType.Namespace,
            },
        };

        if (PDB.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. PDB.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            };
        }

        if (PDB.Metadata.Labels?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. PDB.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            };
        }

        var selector = PDB.Spec?.Selector?.MatchLabels;
        yield return new HeaderedRow
        {
            Header = "Selector",
            Content = selector is { Count: > 0 }
                ? new CollectionContent
                {
                    Items =
                    [
                        .. selector.Select(s =>
                            new TextCollectionElement { Value = $"{s.Key}={s.Value}" }
                        ),
                    ],
                }
                : new TextContent { Value = "<none>" },
        };

        yield return new HeaderedRow
        {
            Header = "Min Available",
            Content = new TextContent { Value = PDB.Spec?.MinAvailable?.Value ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Max Unavailable",
            Content = new TextContent { Value = PDB.Spec?.MaxUnavailable?.Value ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Current Healthy",
            Content = new TextContent { Value = PDB.Status?.CurrentHealthy.ToString() ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Desired Healthy",
            Content = new TextContent { Value = PDB.Status?.DesiredHealthy.ToString() ?? "-" },
        };

        if (PDB.Status?.Conditions is { Count: > 0 } conditions)
        {
            yield return new HeaderedRow
            {
                Header = "Conditions",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. conditions.Select(c => new ConditionCollectionElement
                        {
                            Type = c.Type,
                            Status = c.Status,
                            Message = c.Message,
                            Reason = c.Reason,
                            LastTransitionTime = c.LastTransitionTime,
                        }),
                    ],
                },
            };
        }
    }
}
