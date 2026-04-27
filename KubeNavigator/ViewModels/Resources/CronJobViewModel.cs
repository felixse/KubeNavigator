using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class CronJobViewModel : KubernetesResourceViewModel
{
    public CronJobViewModel(V1CronJob resource, ClusterViewModel cluster)
        : base(resource, ResourceType.CronJob, cluster) { }

    public V1CronJob CronJob => (V1CronJob)Resource;

    public static readonly ImmutableArray<ResourceColumn> CronJobColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Schedule",
            vm => ((CronJobViewModel)vm).Schedule,
            PropertyName: nameof(Schedule)
        ),
        new(
            "Timezone",
            vm => ((CronJobViewModel)vm).Timezone,
            PropertyName: nameof(Timezone)
        ),
        new(
            "Resumed",
            vm => ((CronJobViewModel)vm).Resumed,
            PropertyName: nameof(Resumed)
        ),
        new(
            "Active",
            vm => ((CronJobViewModel)vm).Active,
            PropertyName: nameof(Active)
        ),
        new(
            "Last Schedule",
            vm => ((CronJobViewModel)vm).LastSchedule,
            ResourceColumnType.Age,
            nameof(LastSchedule)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => CronJobColumns;

    public string Schedule => CronJob.Spec?.Schedule ?? string.Empty;

    public string Timezone => CronJob.Spec?.TimeZone ?? string.Empty;

    public string Resumed => CronJob.Spec?.Suspend == true ? "No" : "Yes";

    public int Active => CronJob.Status?.Active?.Count ?? 0;

    public DateTime? LastSchedule => CronJob.Status?.LastScheduleTime;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetCronJobRows()] },
            new DetailsSection
            {
                Header = "Template",
                Rows = [.. GetTemplateRows()],
            },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetCronJobRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = CronJob.CreationTimestamp().ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = CronJob.Name() },
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
                    .. CronJob.Metadata.Labels?.Select(l => new TextCollectionElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            },
        };

        if (CronJob.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. CronJob.Metadata.Annotations?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Schedule",
            Content = new TextContent { Value = Schedule },
        };
        yield return new HeaderedRow
        {
            Header = "Timezone",
            Content = new TextContent { Value = Timezone },
        };
        yield return new HeaderedRow
        {
            Header = "Concurrency Policy",
            Content = new TextContent { Value = CronJob.Spec?.ConcurrencyPolicy ?? string.Empty },
        };
        yield return new HeaderedRow
        {
            Header = "Resumed",
            Content = new TextContent { Value = Resumed },
        };
        yield return new HeaderedRow
        {
            Header = "Successful Jobs History Limit",
            Content = new TextContent
            {
                Value = CronJob.Spec?.SuccessfulJobsHistoryLimit?.ToString() ?? string.Empty,
            },
        };
        yield return new HeaderedRow
        {
            Header = "Failed Jobs History Limit",
            Content = new TextContent
            {
                Value = CronJob.Spec?.FailedJobsHistoryLimit?.ToString() ?? string.Empty,
            },
        };
        yield return new HeaderedRow
        {
            Header = "Last Schedule",
            Content = new TextContent
            {
                Value = CronJob.Status?.LastScheduleTime?.ToString() ?? string.Empty,
            },
        };
        yield return new HeaderedRow
        {
            Header = "Last Successful Run",
            Content = new TextContent
            {
                Value = CronJob.Status?.LastSuccessfulTime?.ToString() ?? string.Empty,
            },
        };
        yield return new HeaderedRow
        {
            Header = "Active",
            Content = new TextContent { Value = Active.ToString() },
        };
    }

    private IEnumerable<IDetailsRow> GetTemplateRows()
    {
        var jobSpec = CronJob.Spec?.JobTemplate?.Spec;

        yield return new HeaderedRow
        {
            Header = "Parallelism",
            Content = new TextContent { Value = jobSpec?.Parallelism?.ToString() ?? string.Empty },
        };
        yield return new HeaderedRow
        {
            Header = "Completions",
            Content = new TextContent { Value = jobSpec?.Completions?.ToString() ?? string.Empty },
        };
        yield return new HeaderedRow
        {
            Header = "Resumed",
            Content = new TextContent { Value = jobSpec?.Suspend == true ? "No" : "Yes" },
        };
        yield return new HeaderedRow
        {
            Header = "Active Deadline Seconds",
            Content = new TextContent
            {
                Value = jobSpec?.ActiveDeadlineSeconds?.ToString() ?? string.Empty,
            },
        };
    }
}
