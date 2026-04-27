using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class MutatingWebhookConfigurationViewModel : KubernetesResourceViewModel
{
    public MutatingWebhookConfigurationViewModel(
        V1MutatingWebhookConfiguration resource,
        ClusterViewModel cluster
    )
        : base(resource, ResourceType.MutatingWebhookConfiguration, cluster) { }

    public V1MutatingWebhookConfiguration WebhookConfig =>
        (V1MutatingWebhookConfiguration)Resource;

    public static readonly ImmutableArray<ResourceColumn> MutatingWebhookConfigurationColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Webhooks",
            vm => ((MutatingWebhookConfigurationViewModel)vm).WebhookCount,
            PropertyName: nameof(WebhookCount)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns =>
        MutatingWebhookConfigurationColumns;

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
                            Value = FormatClientConfig(wh.ClientConfig),
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
                        Header = "Reinvocation Policy",
                        Content = new TextContent
                        {
                            Value = wh.ReinvocationPolicy ?? string.Empty,
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
                            Value = FormatLabelSelector(wh.NamespaceSelector),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Object Selector",
                        Content = new TextContent
                        {
                            Value = FormatLabelSelector(wh.ObjectSelector),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Rules",
                        Content = new TextContent
                        {
                            Value = FormatRules(wh.Rules),
                        },
                    },
                ],
            })
            .ToList();
    }

    private static string FormatClientConfig(Admissionregistrationv1WebhookClientConfig? config)
    {
        if (config is null)
            return string.Empty;

        if (config.Service is not null)
        {
            var parts = new List<string>();
            parts.Add($"Name: {config.Service.Name}");
            parts.Add($"Namespace: {config.Service.NamespaceProperty}");
            if (config.Service.Port.HasValue)
                parts.Add($"Port: {config.Service.Port}");
            if (!string.IsNullOrEmpty(config.Service.Path))
                parts.Add($"Path: {config.Service.Path}");
            return string.Join("\n", parts);
        }

        return config.Url ?? string.Empty;
    }

    private static string FormatLabelSelector(V1LabelSelector? selector)
    {
        if (selector is null)
            return string.Empty;

        var lines = new List<string>();

        lines.Add($"Match Expressions: {(selector.MatchExpressions is { Count: > 0 }
            ? string.Join(", ", selector.MatchExpressions.Select(e =>
                $"{e.Key} {e.OperatorProperty} ({string.Join(", ", e.Values ?? [])})"))
            : string.Empty)}");

        if (selector.MatchLabels is { Count: > 0 })
        {
            lines.Add($"Match Labels:");
            foreach (var l in selector.MatchLabels)
            {
                lines.Add($"  {l.Key}={l.Value}");
            }
        }

        return string.Join("\n", lines);
    }

    private static string FormatRules(IList<V1RuleWithOperations>? rules)
    {
        if (rules is null || rules.Count == 0)
            return string.Empty;

        var allLines = new List<string>();
        foreach (var r in rules)
        {
            var groups = string.Join(", ", r.ApiGroups?.Select(g => string.IsNullOrEmpty(g) ? "*" : g) ?? []);
            allLines.Add($"API Groups: {groups}");
            allLines.Add($"API Versions: {string.Join(", ", r.ApiVersions ?? [])}");
            allLines.Add($"Operations: {string.Join(", ", r.Operations ?? [])}");
            allLines.Add($"Resources: {string.Join(", ", r.Resources ?? [])}");
            if (r.Scope is not null)
                allLines.Add($"Scope: {r.Scope}");
        }

        return string.Join("\n", allLines);
    }
}
