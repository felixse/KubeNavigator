using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class ValidatingWebhookConfigurationViewModel : KubernetesResourceViewModel
{
    public ValidatingWebhookConfigurationViewModel(
        V1ValidatingWebhookConfiguration resource,
        ClusterViewModel cluster
    )
        : base(resource, ResourceType.ValidatingWebhookConfiguration, cluster) { }

    public V1ValidatingWebhookConfiguration WebhookConfig =>
        (V1ValidatingWebhookConfiguration)Resource;

    public static readonly ImmutableArray<ResourceColumn> ValidatingWebhookConfigurationColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Webhooks",
            vm => ((ValidatingWebhookConfigurationViewModel)vm).WebhookCount,
            PropertyName: nameof(WebhookCount)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns =>
        ValidatingWebhookConfigurationColumns;

    public int WebhookCount => WebhookConfig.Webhooks?.Count ?? 0;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetMainRows()] },
            new DetailsSection
            {
                Header = "Webhooks",
                Rows = [.. GetWebhookRows()],
            },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetMainRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent
            {
                Value = WebhookConfig.CreationTimestamp().ToString(),
            },
        };
        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = WebhookConfig.Name() },
        };

        yield return new HeaderedRow
        {
            Header = "Labels",
            Content = new CollectionContent
            {
                Items =
                [
                    .. WebhookConfig.Metadata.Labels?.Select(l => new TextCollectionElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            },
        };

        if (WebhookConfig.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. WebhookConfig.Metadata.Annotations?.Select(
                            l => new TextCollectionElement
                            {
                                Value = $"{l.Key}={l.Value}",
                            }
                        ) ?? [],
                    ],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "API Version",
            Content = new TextContent { Value = WebhookConfig.ApiVersion ?? string.Empty },
        };
    }

    private List<IDetailsRow> GetWebhookRows()
    {
        var webhooks = WebhookConfig.Webhooks;
        if (webhooks is null || webhooks.Count == 0)
            return [];

        return webhooks
            .Select(wh => (IDetailsRow)new GroupRow
            {
                Header = new DetailsGroupHeader { Title = wh.Name },
                Rows =
                [
                    new HeaderedRow
                    {
                        Header = "Client Config",
                        Content = new TextContent
                        {
                            Value = WebhookFormatHelper.FormatClientConfig(wh.ClientConfig),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Match Policy",
                        Content = new TextContent
                        {
                            Value = wh.MatchPolicy ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Failure Policy",
                        Content = new TextContent
                        {
                            Value = wh.FailurePolicy ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Admission Review Versions",
                        Content = new TextContent
                        {
                            Value = wh.AdmissionReviewVersions is { Count: > 0 }
                                ? string.Join(", ", wh.AdmissionReviewVersions)
                                : string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Side Effects",
                        Content = new TextContent
                        {
                            Value = wh.SideEffects ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Timeout Seconds",
                        Content = new TextContent
                        {
                            Value = wh.TimeoutSeconds?.ToString() ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Namespace Selector",
                        Content = new TextContent
                        {
                            Value = WebhookFormatHelper.FormatLabelSelector(wh.NamespaceSelector),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Object Selector",
                        Content = new TextContent
                        {
                            Value = WebhookFormatHelper.FormatLabelSelector(wh.ObjectSelector),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Rules",
                        Content = new TextContent
                        {
                            Value = WebhookFormatHelper.FormatRules(wh.Rules),
                        },
                    },
                ],
            })
            .ToList();
    }
}
