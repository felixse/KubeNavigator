using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class IngressViewModel : KubernetesResourceViewModel
{
    public IngressViewModel(V1Ingress resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Ingress, cluster) { }

    public V1Ingress Ingress => (V1Ingress)Resource;

    public static readonly ImmutableArray<ResourceColumn> IngressColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Loadbalancers",
            vm => ((IngressViewModel)vm).Loadbalancers,
            PropertyName: nameof(Loadbalancers)
        ),
        new(
            "Rules",
            vm => ((IngressViewModel)vm).RulesSummary,
            PropertyName: nameof(RulesSummary)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => IngressColumns;

    public string Loadbalancers =>
        Ingress.Status?.LoadBalancer?.Ingress is { Count: > 0 } lbs
            ? string.Join(
                ", ",
                lbs.Select(lb => lb.Ip ?? lb.Hostname ?? string.Empty)
            )
            : string.Empty;

    public string RulesSummary
    {
        get
        {
            var rules = Ingress.Spec?.Rules;
            if (rules is not { Count: > 0 })
                return string.Empty;

            var tls = Ingress.Spec?.Tls;
            var tlsHosts = tls?.SelectMany(t => t.Hosts ?? []).ToHashSet() ?? [];

            return string.Join(
                ", ",
                rules.SelectMany(r =>
                {
                    var host = r.Host ?? "*";
                    var scheme = tlsHosts.Contains(host) ? "https" : "http";
                    var paths = r.Http?.Paths;
                    if (paths is not { Count: > 0 })
                        return [$"{scheme}://{host}/"];

                    return paths.Select(p =>
                    {
                        var path = p.Path ?? "/";
                        var backend = p.Backend?.Service is { } svc
                            ? $"{svc.Name}:{svc.Port?.Number?.ToString() ?? svc.Port?.Name ?? string.Empty}"
                            : string.Empty;
                        return $"{scheme}://{host}{path} --> {backend}";
                    });
                })
            );
        }
    }

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        var ruleRows = GetRuleRows().ToList();
        if (ruleRows.Count > 0)
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Rules",
                    Rows =
                    [
                        new FullWidthRow
                        {
                            Content = new TableContent
                            {
                                Columns = ["Path", "Link", "Backends"],
                                Rows = ruleRows,
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

    private List<IDetailsRow> GetInfoRows()
    {
        var rows = new List<IDetailsRow>
        {
            new HeaderedRow
            {
                Header = "Created",
                Content = new TextContent
                {
                    Value = Ingress.CreationTimestamp().ToString(),
                },
            },
            new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = Ingress.Name() },
            },
            new HeaderedRow
            {
                Header = "Namespace",
                Content = new LinkContent
                {
                    ResourceName = Ingress.Namespace(),
                    ResourceType = ResourceType.Namespace,
                },
            },
        };

        if (Ingress.Metadata.Annotations?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Ingress.Metadata.Annotations.Select(a =>
                            new TextCollectionElement { Value = $"{a.Key}={a.Value}" }
                        ),
                    ],
                },
            });
        }

        if (Ingress.Metadata.Labels?.Count > 0)
        {
            rows.Add(new HeaderedRow
            {
                Header = "Labels",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Ingress.Metadata.Labels.Select(l =>
                            new TextCollectionElement { Value = $"{l.Key}={l.Value}" }
                        ),
                    ],
                },
            });
        }

        var tls = Ingress.Spec?.Tls;
        var ports = new List<string>();
        ports.Add("80");
        if (tls is { Count: > 0 })
            ports.Add("443");

        rows.Add(new HeaderedRow
        {
            Header = "Ports",
            Content = new TextContent { Value = string.Join(", ", ports) },
        });

        return rows;
    }

    private IEnumerable<IEnumerable<ITableCellContent>> GetRuleRows()
    {
        var rules = Ingress.Spec?.Rules;
        if (rules is not { Count: > 0 })
            yield break;

        foreach (var rule in rules)
        {
            var host = rule.Host ?? "*";
            var paths = rule.Http?.Paths;
            if (paths is not { Count: > 0 })
            {
                yield return new ITableCellContent[]
                {
                    (TextContent)"/",
                    (TextContent)host,
                    (TextContent)string.Empty,
                };
                continue;
            }

            foreach (var path in paths)
            {
                var pathValue = path.Path ?? "/";
                var link = $"{host}{pathValue}";
                var backend = path.Backend?.Service is { } svc
                    ? $"{svc.Name}:{svc.Port?.Number?.ToString() ?? svc.Port?.Name ?? string.Empty}"
                    : path.Backend?.Resource is { } res
                        ? $"{res.Kind}/{res.Name}"
                        : string.Empty;

                yield return new ITableCellContent[]
                {
                    (TextContent)pathValue,
                    (TextContent)link,
                    new LinkContent
                    {
                        Prefix = "",
                        ResourceName = path.Backend?.Service?.Name ?? string.Empty,
                        ResourceType = ResourceType.Service,
                    },
                };
            }
        }
    }
}
