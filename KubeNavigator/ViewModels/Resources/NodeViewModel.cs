using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class NodeViewModel : KubernetesResourceViewModel
{
    public NodeViewModel(V1Node resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Node, cluster)
    {
        RefreshMetrics();
    }

    public V1Node Node => (V1Node)Resource;

    public static readonly ImmutableArray<ResourceColumn> NodeColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("CPU", vm => ((NodeViewModel)vm).Cpu, PropertyName: nameof(Cpu)),
        new("Memory", vm => ((NodeViewModel)vm).Memory, PropertyName: nameof(Memory)),
        new("Roles", vm => ((NodeViewModel)vm).Roles, PropertyName: nameof(Roles)),
        new("Taints", vm => ((NodeViewModel)vm).Taints, PropertyName: nameof(Taints)),
        new("Version", vm => ((NodeViewModel)vm).Version, PropertyName: nameof(Version)),
        new("Internal IP", vm => ((NodeViewModel)vm).InternalIP, PropertyName: nameof(InternalIP)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new("Schedulable", vm => ((NodeViewModel)vm).Schedulable, PropertyName: nameof(Schedulable)),
        new(
            "Conditions",
            vm => ((NodeViewModel)vm).Conditions,
            ResourceColumnType.Conditions,
            nameof(Conditions)
        ),
    ];

    public override ImmutableArray<ResourceColumn> Columns => NodeColumns;

    [ObservableProperty]
    public partial string Cpu { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Memory { get; private set; } = string.Empty;

    public void RefreshMetrics()
    {
        var metrics = Cluster.Context.GetNodeMetrics(Node.Name());
        Cpu = metrics?.Cpu.Format() ?? string.Empty;
        Memory = metrics?.Memory.Format() ?? string.Empty;
    }

    public string Roles
    {
        get
        {
            var labels = Node.Metadata?.Labels;
            if (labels is null)
                return string.Empty;

            var roles = labels
                .Where(kv =>
                    kv.Key.StartsWith("node-role.kubernetes.io/", StringComparison.Ordinal)
                )
                .Select(kv => kv.Key["node-role.kubernetes.io/".Length..])
                .Where(r => !string.IsNullOrEmpty(r));

            return string.Join(", ", roles);
        }
    }

    public string Taints =>
        Node.Spec?.Taints is { Count: > 0 } taints
            ? taints.Count.ToString()
            : "0";

    public string Version => Node.Status?.NodeInfo?.KubeletVersion ?? string.Empty;

    public string InternalIP =>
        Node.Status?.Addresses?.FirstOrDefault(a => a.Type == "InternalIP")?.Address
        ?? string.Empty;

    public string Schedulable => Node.Spec?.Unschedulable == true ? "No" : "Yes";

    public List<string> Conditions =>
        Node.Status?.Conditions is null
            ? []
            : Node.Status.Conditions.Where(c => c.Status == "True").Select(c => c.Type).ToList();

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection
            {
                Rows =
                [
                    new HeaderedRow
                    {
                        Header = "Created",
                        Content = new TextContent
                        {
                            Value = Resource.CreationTimestamp().ToString(),
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Name",
                        Content = new TextContent { Value = Resource.Name() },
                    },
                    new HeaderedRow
                    {
                        Header = "Labels",
                        Content = new CollectionContent
                        {
                            Items =
                            [
                                .. Resource.Metadata.Labels?.Select(l => new TextCollectionElement
                                {
                                    Value = $"{l.Key}={l.Value}",
                                }) ?? [],
                            ],
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Annotations",
                        Content = new CollectionContent
                        {
                            Items =
                            [
                                .. Resource.Metadata.Annotations?.Select(a =>
                                    new TextCollectionElement
                                    {
                                        Value = $"{a.Key}={a.Value}",
                                    }
                                ) ?? [],
                            ],
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Addresses",
                        Content = new CollectionContent
                        {
                            Items =
                            [
                                .. Node.Status?.Addresses?.Select(a => new TextCollectionElement
                                {
                                    Value = $"{a.Type}: {a.Address}",
                                }) ?? [],
                            ],
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "OS",
                        Content = new TextContent
                        {
                            Value = $"{Node.Status?.NodeInfo?.OperatingSystem ?? string.Empty} ({Node.Status?.NodeInfo?.Architecture ?? string.Empty})",
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "OS Image",
                        Content = new TextContent
                        {
                            Value = Node.Status?.NodeInfo?.OsImage ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Kernel Version",
                        Content = new TextContent
                        {
                            Value = Node.Status?.NodeInfo?.KernelVersion ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Container Runtime",
                        Content = new TextContent
                        {
                            Value = Node.Status?.NodeInfo?.ContainerRuntimeVersion ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Kubelet Version",
                        Content = new TextContent
                        {
                            Value = Node.Status?.NodeInfo?.KubeletVersion ?? string.Empty,
                        },
                    },
                    new HeaderedRow
                    {
                        Header = "Conditions",
                        Content = new CollectionContent
                        {
                            Items =
                            [
                                .. Node.Status?.Conditions?.Select(c =>
                                    new ConditionCollectionElement
                                    {
                                        Type = c.Type,
                                        Status = c.Status,
                                        LastTransitionTime = c.LastTransitionTime,
                                        LastHeartbeatTime = c.LastHeartbeatTime,
                                        Reason = c.Reason,
                                        Message = c.Message,
                                    }
                                ) ?? [],
                            ],
                        },
                    },
                ],
            },
            CreateResourceDictionarySection("Capacity", Node.Status?.Capacity),
            CreateResourceDictionarySection("Allocatable", Node.Status?.Allocatable),
        };

        var podRows = await GetPodRowsAsync();
        sections.Add(
            new DetailsSection
            {
                Header = "Pods",
                Rows =
                [
                    new FullWidthRow
                    {
                        Content = new TableContent
                        {
                            Columns = ["Name", "Namespace", "Ready", "CPU", "Memory", "Status"],
                            Rows = podRows,
                        },
                    },
                ],
            }
        );

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private static DetailsSection CreateResourceDictionarySection(
        string header,
        IDictionary<string, ResourceQuantity>? resources
    )
    {
        return new DetailsSection
        {
            Header = header,
            Rows =
            [
                .. (resources ?? new Dictionary<string, ResourceQuantity>()).Select(kv =>
                    new HeaderedRow
                    {
                        Header = kv.Key,
                        Content = new TextContent { Value = FormatResourceQuantity(kv.Key, kv.Value) },
                    }
                ),
            ],
        };
    }

    private static string FormatResourceQuantity(string key, ResourceQuantity quantity)
    {
        if (key == "cpu")
        {
            return CpuQuantity.FromResourceQuantity(quantity).Format();
        }

        if (key is "memory" or "ephemeral-storage" or "hugepages-1Gi" or "hugepages-2Mi")
        {
            return MemoryQuantity.FromResourceQuantity(quantity).Format();
        }

        return quantity.ToString();
    }

    private async Task<IEnumerable<IEnumerable<ITableCellContent>>> GetPodRowsAsync()
    {
        var pods = await Cluster.GetResourcesAsync(ResourceType.Pod);
        var nodeName = Node.Name();

        return pods.Where(p =>
                p.Resource is V1Pod pod && pod.Spec?.NodeName == nodeName
            )
            .Select(p =>
            {
                var pod = (V1Pod)p.Resource;
                var ready =
                    pod.Status?.ContainerStatuses != null
                        ? $"{pod.Status.ContainerStatuses.Count(c => c.Ready)}/{pod.Status.ContainerStatuses.Count}"
                        : "0/0";
                var metrics = Cluster.Context.GetPodMetrics(pod.Namespace(), pod.Name());
                var cpu = metrics?.Cpu.Format() ?? "-";
                var memory = metrics?.Memory.Format() ?? "-";
                var status = pod.Metadata.DeletionTimestamp is not null
                    ? "Terminating"
                    : pod.Status?.Phase ?? string.Empty;
                return (IEnumerable<ITableCellContent>)
                    new ITableCellContent[]
                    {
                        new LinkContent
                        {
                            ResourceName = pod.Name(),
                            ResourceType = ResourceType.Pod,
                        },
                        new LinkContent
                        {
                            ResourceName = pod.Namespace(),
                            ResourceType = ResourceType.Namespace,
                        },
                        (TextContent)ready,
                        (TextContent)cpu,
                        (TextContent)memory,
                        (StatusCellContent)status,
                    };
            });
    }
}
