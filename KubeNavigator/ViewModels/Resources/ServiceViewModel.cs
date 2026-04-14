using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class ServiceViewModel : KubernetesResourceViewModel
{
    public ServiceViewModel(V1Service resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Service, cluster) { }

    public V1Service Service => (V1Service)Resource;

    public static readonly ImmutableArray<ResourceColumn> ServiceColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Type", vm => ((ServiceViewModel)vm).Type, PropertyName: nameof(Type)),
        new("Cluster IP", vm => ((ServiceViewModel)vm).ClusterIP, PropertyName: nameof(ClusterIP)),
        new("External IP", vm => ((ServiceViewModel)vm).ExternalIP, PropertyName: nameof(ExternalIP)),
        new("Ports", vm => ((ServiceViewModel)vm).Ports, PropertyName: nameof(Ports)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new("Status", vm => ((ServiceViewModel)vm).Status, ResourceColumnType.Status, nameof(Status)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => ServiceColumns;

    public string Type => Service.Spec.Type;

    public string ClusterIP => Service.Spec.ClusterIP;

    public string ExternalIP =>
        Service.Spec.ExternalIPs is null || !Service.Spec.ExternalIPs.Any()
            ? "-"
            : string.Join(", ", Service.Spec.ExternalIPs);

    public string Ports =>
        Service.Spec.Ports is null || !Service.Spec.Ports.Any()
            ? "-"
            : string.Join(", ", Service.Spec.Ports.Select(p => $"{p.Port}/{p.Protocol}"));

    public string Status =>
        Service.Metadata.DeletionTimestamp is not null
            ? "Terminating"
            : string.Join(", ", Service.Status?.Conditions?.Select(c => c.Type) ?? []);

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetServiceRows()] },
        };

        if (Service.Spec.Type == "LoadBalancer")
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Load Balancer",
                    Rows =
                    [
                        new HeaderedRow
                        {
                            Header = "Allocate Load Balancer Node Ports",
                            Content = new TextContent
                            {
                                Value =
                                    Service.Spec.AllocateLoadBalancerNodePorts ?? false
                                        ? "True"
                                        : "False",
                            },
                        },
                        new HeaderedRow
                        {
                            Header = "External Traffic Policy",
                            Content = new TextContent
                            {
                                Value = Service.Spec.ExternalTrafficPolicy,
                            },
                        },
                        new HeaderedRow
                        {
                            Header = "Hostname",
                            Content = new TextContent
                            {
                                Value = Service.Spec.LoadBalancerIP,
                            },
                        },
                    ],
                }
            );
        }

        sections.Add(
            new DetailsSection { Header = "Connection", Rows = [.. GetConnectionRows()] }
        );

        sections.Add(events);

        return sections;
    }

    private IEnumerable<IDetailsRow> GetServiceRows()
    {
        yield return new HeaderedRow { Header = "Created", Content = new TextContent { Value = Service.CreationTimestamp().ToString() } };
        yield return new HeaderedRow { Header = "Name", Content = new TextContent { Value = Service.Name() } };
        yield return new HeaderedRow { Header = "Namespace", Content = new LinkContent { ResourceName = Resource.Namespace(), ResourceType = ResourceType.Namespace } };

        yield return new HeaderedRow
        {
            Header = "Labels",
            Content = new CollectionContent
            {
                Items = [.. Service.Metadata.Labels?.Select(l => new TextCollectionElement { Value = $"{l.Key}={l.Value}" }) ?? []],
            },
        };

        if (Service.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items = [.. Service.Metadata.Annotations?.Select(l => new TextCollectionElement { Value = $"{l.Key}={l.Value}" }) ?? []],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Selector",
            Content = new CollectionContent
            {
                Items = [.. Service.Spec.Selector?.Select(s => new TextCollectionElement { Value = $"{s.Key}={s.Value}" }) ?? []],
            },
        };

        yield return new HeaderedRow { Header = "Type", Content = new TextContent { Value = Type } };
        yield return new HeaderedRow { Header = "Session Affinity", Content = new TextContent { Value = Service.Spec.SessionAffinity } };
        yield return new HeaderedRow { Header = "Internal Traffic Policy", Content = new TextContent { Value = Service.Spec.InternalTrafficPolicy } };
    }

    private IEnumerable<IDetailsRow> GetConnectionRows()
    {
        yield return new HeaderedRow { Header = "Cluster IP", Content = new TextContent { Value = ClusterIP } };

        yield return new HeaderedRow
        {
            Header = "Cluster IPs",
            Content = new CollectionContent
            {
                Items = [.. Service.Spec.ClusterIPs?.Select(c => new TextCollectionElement { Value = c }) ?? []],
            },
        };

        yield return new HeaderedRow { Header = "IP family policy", Content = new TextContent { Value = Service.Spec.IpFamilyPolicy } };
        yield return new HeaderedRow { Header = "IP families", Content = new TextContent { Value = string.Join(", ", Service.Spec.IpFamilies ?? []) } };

        yield return new FullWidthRow
        {
            Content = new PortsContent
            {
                Ports =
                [
                    .. Service.Spec.Ports?.Select(p => new PortViewModel(
                        p,
                        this,
                        Cluster,
                        Cluster.App.ForwardedPorts.FirstOrDefault(fp =>
                            fp.Resource == this && fp.TargetPort == p.TargetPort.ToInt()
                        )
                    ))
                        ?? [],
                ],
            },
        };
    }
}
