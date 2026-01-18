using System.Collections.Generic;
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
            new DetailsSection { Items = [.. GetServiceItems()] },
        };

        if (Service.Spec.Type == "LoadBalancer")
        {
            sections.Add(
                new DetailsSection
                {
                    Header = "Load Balancer",
                    Items =
                    [
                        new DetailsTextItem
                        {
                            Title = "Allocate Load Balancer Node Ports",
                            Value =
                                Service.Spec.AllocateLoadBalancerNodePorts ?? false
                                    ? "True"
                                    : "False",
                        },
                        new DetailsTextItem
                        {
                            Title = "External Traffic Policy",
                            Value = Service.Spec.ExternalTrafficPolicy,
                        },
                        new DetailsTextItem
                        {
                            Title = "Hostname",
                            Value = Service.Spec.LoadBalancerIP,
                        },
                    ],
                }
            );
        }

        sections.Add(
            new DetailsSection { Header = "Connection", Items = [.. GetConnectionItems()] }
        );

        sections.AddRange(events);

        return sections;
    }

    private IEnumerable<IDetailsItem> GetServiceItems()
    {
        yield return new DetailsTextItem
        {
            Title = "Created",
            Value = Service.CreationTimestamp().ToString(),
        };

        yield return new DetailsTextItem { Title = "Name", Value = Service.Name() };

        yield return new DetailsLinkItem
        {
            Title = "Namespace",
            ResourceName = Resource.Namespace(),
            ResourceType = ResourceType.Namespace,
        };

        yield return new DetailsCollectionItem
        {
            Title = "Labels",
            Items =
            [
                .. Service.Metadata.Labels?.Select(l => new DetailsCollectionItemElement
                {
                    Value = $"{l.Key}={l.Value}",
                }) ?? [],
            ],
        };

        if (Service.Metadata.Annotations?.Count > 0)
        {
            yield return new DetailsCollectionItem
            {
                Title = "Annotations",
                Items =
                [
                    .. Service.Metadata.Annotations?.Select(l => new DetailsCollectionItemElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            };
        }

        yield return new DetailsCollectionItem
        {
            Title = "Selector",
            Items =
            [
                .. Service.Spec.Selector?.Select(s => new DetailsCollectionItemElement
                {
                    Value = $"{s.Key}={s.Value}",
                }) ?? [],
            ],
        };

        yield return new DetailsTextItem { Title = "Type", Value = Type };

        yield return new DetailsTextItem
        {
            Title = "Session Affinity",
            Value = Service.Spec.SessionAffinity,
        };

        yield return new DetailsTextItem
        {
            Title = "Internal Traffic Policy",
            Value = Service.Spec.InternalTrafficPolicy,
        };
    }

    private IEnumerable<IDetailsItem> GetConnectionItems()
    {
        yield return new DetailsTextItem { Title = "Cluster IP", Value = ClusterIP };

        yield return new DetailsCollectionItem
        {
            Title = "Cluster IPs",
            Items =
            [
                .. Service.Spec.ClusterIPs?.Select(c => new DetailsCollectionItemElement
                {
                    Value = c,
                }) ?? [],
            ],
        };

        yield return new DetailsTextItem
        {
            Title = "IP family policy",
            Value = Service.Spec.IpFamilyPolicy,
        };

        yield return new DetailsTextItem
        {
            Title = "IP families",
            Value = string.Join(", ", Service.Spec.IpFamilies ?? []),
        };

        yield return new DetailsPortsItem
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
        };
    }
}
