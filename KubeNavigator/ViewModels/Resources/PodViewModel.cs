using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Resources;

public partial class PodViewModel : KubernetesResourceViewModel
{
    private readonly ILogger<PodViewModel> _logger;

    public PodViewModel(V1Pod resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Pod, cluster)
    {
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<PodViewModel>();

        Commands.Insert(
            0,
            new ItemCommand
            {
                Name = "Show Logs",
                Symbol = "List",
                Command = ShowLogsCommand,
            }
        );
        Commands.Insert(
            1,
            new ItemCommand
            {
                Name = "Open Shell",
                Symbol = "Play",
                Command = OpenShellCommand,
            }
        );

        PropertyChanged += OnPropertyChanged;
    }

    [RelayCommand]
    public void ShowLogs()
    {
        Log.OpeningPodLogs(_logger, Pod.Name(), Pod.Namespace());
        Cluster.App.WindowManager.ActiveWindow.ShelfHost.OpenShelfItem(
            new PodLogsViewModel(this, Cluster, Cluster.App.ThemeManager)
        );
    }

    [RelayCommand]
    public void OpenShell()
    {
        Log.OpeningPodShell(_logger, Pod.Name(), Pod.Namespace());
        Cluster.App.WindowManager.ActiveWindow.ShelfHost.OpenShelfItem(
            new PodShellViewModel(this, Cluster, Cluster.App.ThemeManager)
        );
    }

    [ObservableProperty]
    public partial string CpuUsage { get; private set; }

    [ObservableProperty]
    public partial string MemoryUsage { get; private set; }

    public V1Pod Pod => (V1Pod)Resource;

    public static readonly ImmutableArray<ResourceColumn> PodColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Containers",
            vm => ((PodViewModel)vm).ContainerStatuses,
            ResourceColumnType.ContainerStatuses,
            nameof(ContainerStatuses)
        ),
        new("CPU", vm => ((PodViewModel)vm).CpuUsage, PropertyName: nameof(CpuUsage)),
        new("Memory", vm => ((PodViewModel)vm).MemoryUsage, PropertyName: nameof(MemoryUsage)),
        new("Restarts", vm => ((PodViewModel)vm).Restarts, PropertyName: nameof(Restarts)),
        new(
            "Controlled By",
            vm => ((PodViewModel)vm).ControlledBy,
            PropertyName: nameof(ControlledBy)
        ),
        new("Node", vm => ((PodViewModel)vm).Node, PropertyName: nameof(Node)),
        new("QoS", vm => ((PodViewModel)vm).QoS, PropertyName: nameof(QoS)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new("Status", vm => ((PodViewModel)vm).Status, ResourceColumnType.Status, nameof(Status)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => PodColumns;

    public List<V1ContainerStatus> ContainerStatuses =>
        Pod.Status?.ContainerStatuses?.ToList() ?? Enumerable.Empty<V1ContainerStatus>().ToList();

    public int Restarts => Pod.Status.ContainerStatuses?.Sum(c => c.RestartCount) ?? 0;

    public string Node => Pod.Spec.NodeName;

    public string QoS => Pod.Status.QosClass;

    public string Status =>
        Pod.Metadata.DeletionTimestamp is not null ? "Terminating" : Pod.Status.Phase;

    public string ControlledBy =>
        Pod.Metadata.OwnerReferences?.FirstOrDefault()?.Name ?? string.Empty;

    public void RefreshMetrics()
    {
        var metrics = Cluster.Context.GetPodMetrics(Pod.Namespace(), Pod.Name());
        CpuUsage = metrics?.Cpu.Format() ?? string.Empty;
        MemoryUsage = metrics?.Memory.Format() ?? string.Empty;
    }

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetPodRows()] },
            new DetailsSection
            {
                Header = "Containers",
                Rows =
                [
                    .. Pod.Spec.Containers.Select(c => new GroupRow
                    {
                        Header = new DetailsGroupHeader { Title = c.Name },
                        Rows = [.. GetContainerRows(c)],
                    }),
                ],
            },
            new DetailsSection
            {
                Header = "Volumes",
                Rows =
                [
                    .. Pod.Spec.Volumes?.Select(v => new GroupRow
                    {
                        Header = new DetailsGroupHeader { Title = v.Name, Symbol = "\uEDA2" },
                        Rows = [.. GetVolumeRows(v)],
                    })
                        ?? [],
                ],
            },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetPodRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = Pod.CreationTimestamp().ToString() },
        };

        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = Pod.Name() },
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
                    .. Pod.Metadata.Labels?.Select(l => new TextCollectionElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            },
        };

        if (Pod.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Pod.Metadata.Annotations?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            };
        }

        if (Pod.Metadata.OwnerReferences?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Controlled by",
                Content = new LinkContent
                {
                    Prefix = $"{Pod.Metadata.OwnerReferences.First().Kind}: ",
                    ResourceName = Pod.Metadata.OwnerReferences.First().Name,
                    ResourceType = Pod
                        .Metadata.OwnerReferences.First(o => o.Controller == true)
                        .Kind switch
                    {
                        "ReplicaSet" => ResourceType.ReplicaSet,
                        "Deployment" => ResourceType.Deployment,
                        "DaemonSet" => ResourceType.DaemonSet,
                        "Node" => ResourceType.Node,
                        _ => new ResourceType(
                            "Unknown",
                            "Unknown",
                            "Unknown",
                            "unknown",
                            true,
                            "Unknowns",
                            "Unknown"
                        ),
                    },
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Status",
            Content = new TextContent
            {
                Value = Status,
                ValueColor = Status switch
                {
                    "Running" => Category.Success,
                    "Succeeded" => Category.Success,
                    "Failed" => Category.Error,
                    "Pending" => Category.Warning,
                    "Terminating" => Category.Default,
                    _ => Category.Default,
                },
            },
        };

        yield return new HeaderedRow
        {
            Header = "Node",
            Content = new LinkContent
            {
                ResourceName = Pod.Spec.NodeName,
                ResourceType = ResourceType.Node,
            },
        };

        yield return new HeaderedRow
        {
            Header = "Pod IP",
            Content = new TextContent { Value = Pod.Status.PodIP },
        };

        yield return new HeaderedRow
        {
            Header = "Pod IPs",
            Content = new CollectionContent
            {
                Items =
                [
                    .. Pod.Status.PodIPs?.Select(p => new TextCollectionElement { Value = p.Ip })
                        ?? [],
                ],
            },
        };

        yield return new HeaderedRow
        {
            Header = "Service Account",
            Content = new LinkContent
            {
                ResourceName = Pod.Spec.ServiceAccountName,
                ResourceType = ResourceType.ServiceAccount,
            },
        };

        yield return new HeaderedRow
        {
            Header = "Priority Class",
            Content = new LinkContent
            {
                ResourceName = Pod.Spec.PriorityClassName,
                ResourceType = ResourceType.PriorityClass,
            },
        };

        yield return new HeaderedRow
        {
            Header = "QoS Class",
            Content = new TextContent { Value = Pod.Status.QosClass },
        };

        if (Pod.Spec.TerminationGracePeriodSeconds.HasValue)
        {
            yield return new HeaderedRow
            {
                Header = "Termination Grace Period",
                Content = new TextContent
                {
                    Value = Pod.Spec.TerminationGracePeriodSeconds.ToString(),
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Node Selector",
            Content = new CollectionContent
            {
                Items =
                [
                    .. Pod.Spec.NodeSelector?.Select(n => new TextCollectionElement
                    {
                        Value = $"{n.Key}: {n.Value}",
                    }) ?? [],
                ],
            },
        };

        var tolerationsTable = new TableContent
        {
            Columns = ["Key", "Operator", "Value", "Effect", "Seconds"],
            Rows =
                Pod.Spec.Tolerations?.Select(t =>
                    (IEnumerable<ITableCellContent>)
                        new TextContent[]
                        {
                            t.Key,
                            t.OperatorProperty,
                            t.Value,
                            t.Effect,
                            t.TolerationSeconds?.ToString() ?? string.Empty,
                        }
                )
                ?? [],
        };

        yield return new ExpandableRow
        {
            Header = "Tolerations",
            Summary = tolerationsTable.Count.ToString(),
            Content = tolerationsTable,
        };

        yield return new ExpandableRow
        {
            Header = "Affinities",
            Summary = new object?[]
            {
                Pod.Spec.Affinity?.PodAffinity,
                Pod.Spec.Affinity?.NodeAffinity,
                Pod.Spec.Affinity?.PodAntiAffinity,
            }
                .Count(a => a != null)
                .ToString(),
            Content = new EditorContent
            {
                Value =
                    Pod.Spec.Affinity != null
                        ? YamlSerializerFactory.Serializer.Serialize(Pod.Spec.Affinity)
                        : string.Empty,
            },
        };

        var secretNames = Pod
            .Spec.Containers.SelectMany(c =>
                (c.Env ?? [])
                    .Select(e => e.ValueFrom?.SecretKeyRef?.Name)
                    .Concat((c.EnvFrom ?? []).Select(e => e.SecretRef?.Name))
            )
            .Concat(
                (Pod.Spec.Volumes ?? []).SelectMany(v =>
                {
                    if (v.Secret != null)
                    {
                        return [v.Secret.SecretName];
                    }

                    if (v.Projected?.Sources != null)
                    {
                        return v.Projected.Sources.Select(s => s.Secret?.Name);
                    }

                    if (v.Csi?.NodePublishSecretRef != null)
                    {
                        return [v.Csi.NodePublishSecretRef.Name];
                    }

                    return [];
                })
            )
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .Order()
            .ToList();

        if (secretNames.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Secrets",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. secretNames.Select(n => new LinkCollectionElement
                        {
                            ResourceName = n!,
                            ResourceType = ResourceType.Secret,
                        }),
                    ],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Conditions",
            Content = new CollectionContent
            {
                Items =
                [
                    .. Pod.Status.Conditions?.Select(c => new ConditionCollectionElement
                    {
                        Type = c.Type,
                        Status = c.Status,
                        Message = c.Message,
                        Reason = c.Reason,
                        LastHeartbeatTime = c.LastProbeTime,
                        LastTransitionTime = c.LastTransitionTime,
                    }) ?? [],
                ],
            },
        };
    }

    private IEnumerable<IDetailsRow> GetContainerRows(V1Container container)
    {
        if (Pod.Status.ContainerStatuses is null)
        {
            yield break;
        }

        var status = Pod.Status.ContainerStatuses.First(s => s.Name == container.Name);
        var statusText = status.State switch
        {
            V1ContainerState { Running: V1ContainerStateRunning } => "Running",
            V1ContainerState { Terminated: V1ContainerStateTerminated } => "Terminated",
            _ => "Waiting",
        };

        if (status.RestartCount > 0)
        {
            statusText += ", Restarted";
        }

        if (status.Ready)
        {
            statusText += ", Ready";
        }

        yield return new HeaderedRow
        {
            Header = "Status",
            Content = new TextContent
            {
                ValueColor = status.State switch
                {
                    V1ContainerState { Running: V1ContainerStateRunning } => Category.Success,
                    V1ContainerState { Terminated: V1ContainerStateTerminated } => Category.Error,
                    _ => Category.Warning,
                },
                Value = statusText,
            },
        };

        if (status.LastState is not null)
        {
            yield return new HeaderedRow
            {
                Header = "Last Status",
                Content = new TextContent
                {
                    Value = Pod
                        .Status.ContainerStatuses.First(s => s.Name == container.Name)
                        .LastState switch
                    {
                        V1ContainerState { Terminated: V1ContainerStateTerminated } =>
                            $"terminated\r\n"
                                + $"Reason: {status.LastState.Terminated.Reason}\r\n"
                                + $"Started at: {status.LastState.Terminated.StartedAt}\r\n"
                                + $"Finished at: {status.LastState.Terminated.FinishedAt}",
                        _ => "Unknown",
                    },
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Image",
            Content = new TextContent { Value = container.Image },
        };

        if (container.Ports != null)
        {
            yield return new FullWidthRow
            {
                Content = new PortsContent
                {
                    Ports =
                    [
                        .. container.Ports.Select(p => new PortViewModel(
                            p,
                            this,
                            Cluster,
                            Cluster.App.ForwardedPorts.FirstOrDefault(fp =>
                                fp.Resource == this && fp.TargetPort == p.ContainerPort
                            )
                        )) ?? [],
                    ],
                },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Environment",
            Content = new DictionaryContent
            {
                Items =
                    container
                        .Env?.Select(e => new DetailsDictionaryEntry
                        {
                            Key = e.Name,
                            Value = GetEnvValueRepresentation(e),
                        })
                        .ToList()
                    ?? [],
            },
        };

        yield return new HeaderedRow
        {
            Header = "Mounts",
            Content = new CollectionContent
            {
                Layout = CollectionLayout.Stack,
                Items =
                [
                    .. container.VolumeMounts.Select(m => new TextCollectionElement
                    {
                        Value = m.MountPath,
                        SecondaryValue =
                            $"from {m.Name} ({(m.ReadOnlyProperty == true ? "ro" : "rw")})",
                    }),
                ],
            },
        };

        if (container.LivenessProbe != null)
        {
            var elements = new List<TextCollectionElement>();
            if (container.LivenessProbe.HttpGet != null)
            {
                elements.Add(new TextCollectionElement { Value = "http-get" });
                elements.Add(
                    new TextCollectionElement
                    {
                        Value =
                            $"{container.LivenessProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.LivenessProbe.HttpGet.Host}{container.LivenessProbe.HttpGet.Path}:{container.LivenessProbe.HttpGet.Port}",
                    }
                );
                elements.Add(
                    new TextCollectionElement
                    {
                        Value = $"port: {container.LivenessProbe.HttpGet.Port}",
                    }
                );
            }

            // todo fill for exec

            elements.Add(
                new TextCollectionElement
                {
                    Value = $"delay={container.LivenessProbe.InitialDelaySeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"timeout={container.LivenessProbe.TimeoutSeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"period={container.LivenessProbe.PeriodSeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"#success={container.LivenessProbe.SuccessThreshold}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"#failure={container.LivenessProbe.FailureThreshold}s",
                }
            );

            yield return new HeaderedRow
            {
                Header = "Liveness",
                Content = new CollectionContent { Items = [.. elements] },
            };
        }

        if (container.ReadinessProbe != null)
        {
            var elements = new List<TextCollectionElement>();
            if (container.ReadinessProbe.HttpGet != null)
            {
                elements.Add(new TextCollectionElement { Value = "http-get" });
                elements.Add(
                    new TextCollectionElement
                    {
                        Value =
                            $"{container.ReadinessProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.ReadinessProbe.HttpGet.Host}{container.ReadinessProbe.HttpGet.Path}:{container.ReadinessProbe.HttpGet.Port}",
                    }
                );
                elements.Add(
                    new TextCollectionElement
                    {
                        Value = $"port: {container.ReadinessProbe.HttpGet.Port}",
                    }
                );
            }

            // todo fill for exec

            elements.Add(
                new TextCollectionElement
                {
                    Value = $"delay={container.ReadinessProbe.InitialDelaySeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"timeout={container.ReadinessProbe.TimeoutSeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"period={container.ReadinessProbe.PeriodSeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"#success={container.ReadinessProbe.SuccessThreshold}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"#failure={container.ReadinessProbe.FailureThreshold}s",
                }
            );

            yield return new HeaderedRow
            {
                Header = "Readiness",
                Content = new CollectionContent { Items = [.. elements] },
            };
        }

        if (container.StartupProbe != null)
        {
            var elements = new List<TextCollectionElement>();
            if (container.StartupProbe.HttpGet != null)
            {
                elements.Add(new TextCollectionElement { Value = "http-get" });
                elements.Add(
                    new TextCollectionElement
                    {
                        Value =
                            $"{container.StartupProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.StartupProbe.HttpGet.Host}{container.StartupProbe.HttpGet.Path}:{container.StartupProbe.HttpGet.Port}",
                    }
                );
                elements.Add(
                    new TextCollectionElement
                    {
                        Value = $"port: {container.StartupProbe.HttpGet.Port}",
                    }
                );
            }

            // todo fill for exec

            elements.Add(
                new TextCollectionElement
                {
                    Value = $"delay={container.StartupProbe.InitialDelaySeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"timeout={container.StartupProbe.TimeoutSeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"period={container.StartupProbe.PeriodSeconds}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"#success={container.StartupProbe.SuccessThreshold}s",
                }
            );
            elements.Add(
                new TextCollectionElement
                {
                    Value = $"#failure={container.StartupProbe.FailureThreshold}s",
                }
            );

            yield return new HeaderedRow
            {
                Header = "Startup",
                Content = new CollectionContent { Items = [.. elements] },
            };
        }

        if (container.Command != null)
        {
            yield return new HeaderedRow
            {
                Header = "Command",
                Content = new TextContent { Value = string.Join(" ", container.Command) },
            };
        }

        if (container.Args != null)
        {
            yield return new HeaderedRow
            {
                Header = "Args",
                Content = new TextContent { Value = string.Join(" ", container.Args) },
            };
        }

        if (container.Resources.Requests != null)
        {
            yield return new HeaderedRow
            {
                Header = "Requests",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. container.Resources.Requests.Select(r => new TextCollectionElement
                        {
                            Value = $"{r.Key}={r.Value}",
                        }),
                    ],
                },
            };
        }
    }

    private static IEnumerable<IDetailsRow> GetVolumeRows(V1Volume volume)
    {
        if (volume.AwsElasticBlockStore != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "AWS Elastic Block Store" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.AwsElasticBlockStore.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent
                {
                    Value = volume.AwsElasticBlockStore.ReadOnlyProperty.ToString(),
                },
            };
            yield return new HeaderedRow
            {
                Header = "Partition",
                Content = new TextContent
                {
                    Value = volume.AwsElasticBlockStore.Partition.ToString(),
                },
            };
            yield return new HeaderedRow
            {
                Header = "Volume ID",
                Content = new TextContent { Value = volume.AwsElasticBlockStore.VolumeID },
            };
        }
        else if (volume.AzureDisk != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Azure Disk" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.AzureDisk.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.AzureDisk.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Kind",
                Content = new TextContent { Value = volume.AzureDisk.Kind },
            };
            yield return new HeaderedRow
            {
                Header = "Disk Name",
                Content = new TextContent { Value = volume.AzureDisk.DiskName },
            };
            yield return new HeaderedRow
            {
                Header = "Disk URI",
                Content = new TextContent { Value = volume.AzureDisk.DiskURI },
            };
            yield return new HeaderedRow
            {
                Header = "Caching Mode",
                Content = new TextContent { Value = volume.AzureDisk.CachingMode },
            };
        }
        else if (volume.AzureFile != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Azure File" },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.AzureFile.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Share Name",
                Content = new TextContent { Value = volume.AzureFile.ShareName },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Name",
                Content = new TextContent { Value = volume.AzureFile.SecretName },
            };
        }
        else if (volume.Cephfs != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "CephFS" },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Cephfs.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Monitors",
                Content = new TextContent { Value = string.Join(", ", volume.Cephfs.Monitors) },
            };
            yield return new HeaderedRow
            {
                Header = "Path",
                Content = new TextContent { Value = volume.Cephfs.Path },
            };
            yield return new HeaderedRow
            {
                Header = "User",
                Content = new TextContent { Value = volume.Cephfs.User },
            };
            yield return new HeaderedRow
            {
                Header = "Secret File",
                Content = new TextContent { Value = volume.Cephfs.SecretFile },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Cephfs.SecretRef?.Name,
                },
            };
        }
        else if (volume.Cinder != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Cinder" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.Cinder.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Cinder.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Volume ID",
                Content = new TextContent { Value = volume.Cinder.VolumeID },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Cinder.SecretRef?.Name,
                },
            };
        }
        else if (volume.ConfigMap != null) // todo items
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Config Map" },
            };
            yield return new HeaderedRow
            {
                Header = "Name",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.ConfigMap,
                    ResourceName = volume.ConfigMap.Name,
                },
            };
            yield return new HeaderedRow
            {
                Header = "Default Mode",
                Content = new TextContent { Value = volume.ConfigMap.DefaultMode?.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Optional",
                Content = new TextContent { Value = volume.ConfigMap.Optional?.ToString() },
            };
        }
        else if (volume.Csi != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "CSI" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.Csi.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Csi.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Driver",
                Content = new TextContent { Value = volume.Csi.Driver },
            };
            yield return new HeaderedRow
            {
                Header = "Node Publish Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Csi.NodePublishSecretRef?.Name,
                },
            };
            yield return new HeaderedRow
            {
                Header = "Volume Attributes",
                Content = new DictionaryContent
                {
                    Items =
                        volume
                            .Csi.VolumeAttributes?.Select(va => new DetailsDictionaryEntry
                            {
                                Key = va.Key,
                                Value = va.Value,
                            })
                            .ToList()
                        ?? [],
                },
            };
        }
        else if (volume.DownwardAPI != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Downward API" },
            };
            yield return new HeaderedRow
            {
                Header = "Default Mode",
                Content = new DictionaryContent
                {
                    Items = volume.DownwardAPI.DefaultMode.HasValue
                        ?
                        [
                            new DetailsDictionaryEntry
                            {
                                Key = "Default Mode",
                                Value = volume.DownwardAPI.DefaultMode.Value.ToString(),
                            },
                        ]
                        : [],
                },
            };
            yield return new HeaderedRow
            {
                Header = "Items",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. volume.DownwardAPI.Items?.Select(i => new TextCollectionElement
                        {
                            Value = i.Path,
                            SecondaryValue =
                                i.FieldRef != null ? $"FieldRef: {i.FieldRef.FieldPath}"
                                : i.ResourceFieldRef != null
                                    ? $"ResourceFieldRef: {i.ResourceFieldRef.Resource}"
                                : string.Empty,
                        }) ?? [],
                    ],
                },
            };
        }
        else if (volume.EmptyDir != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Empty Dir" },
            };
            yield return new HeaderedRow
            {
                Header = "Medium",
                Content = new TextContent { Value = volume.EmptyDir.Medium },
            };
            if (volume.EmptyDir.SizeLimit != null)
            {
                yield return new HeaderedRow
                {
                    Header = "Size Limit",
                    Content = new TextContent { Value = volume.EmptyDir.SizeLimit.ToString() },
                };
            }
        }
        else if (volume.Ephemeral != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Ephemeral" },
            };
            if (volume.Ephemeral.VolumeClaimTemplate != null)
            {
                yield return new HeaderedRow
                {
                    Header = "Volume Claim Template",
                    Content = new LinkContent
                    {
                        ResourceType = ResourceType.PersistentVolumeClaim,
                        ResourceName = volume.Ephemeral.VolumeClaimTemplate.Metadata?.Name,
                    },
                };
            }
        }
        else if (volume.Fc != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Fibre Channel" },
            };
            yield return new HeaderedRow
            {
                Header = "FSType",
                Content = new TextContent { Value = volume.Fc.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Fc.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Target WWNs",
                Content = new TextContent { Value = string.Join(", ", volume.Fc.TargetWWNs) },
            };
            yield return new HeaderedRow
            {
                Header = "Lun",
                Content = new TextContent { Value = volume.Fc.Lun.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Wwids",
                Content = new TextContent { Value = string.Join(", ", volume.Fc.Wwids) },
            };
        }
        else if (volume.FlexVolume != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Flex Volume" },
            };
            yield return new HeaderedRow
            {
                Header = "Driver",
                Content = new TextContent { Value = volume.FlexVolume.Driver },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.FlexVolume.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.FlexVolume.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Options",
                Content = new DictionaryContent
                {
                    Items =
                        volume
                            .FlexVolume.Options?.Select(o => new DetailsDictionaryEntry
                            {
                                Key = o.Key,
                                Value = o.Value,
                            })
                            .ToList()
                        ?? [],
                },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.FlexVolume.SecretRef?.Name,
                },
            };
        }
        else if (volume.Flocker != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Flocker" },
            };
            yield return new HeaderedRow
            {
                Header = "Dataset Name",
                Content = new TextContent { Value = volume.Flocker.DatasetName },
            };
            yield return new HeaderedRow
            {
                Header = "Dataset UUID",
                Content = new TextContent { Value = volume.Flocker.DatasetUUID },
            };
        }
        else if (volume.GcePersistentDisk != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "GCE Persistent Disk" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.GcePersistentDisk.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent
                {
                    Value = volume.GcePersistentDisk.ReadOnlyProperty.ToString(),
                },
            };
            yield return new HeaderedRow
            {
                Header = "Partition",
                Content = new TextContent { Value = volume.GcePersistentDisk.Partition.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "PD Name",
                Content = new TextContent { Value = volume.GcePersistentDisk.PdName },
            };
        }
        else if (volume.GitRepo != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Git Repo" },
            };
            yield return new HeaderedRow
            {
                Header = "Repository",
                Content = new TextContent { Value = volume.GitRepo.Repository },
            };
            yield return new HeaderedRow
            {
                Header = "Revision",
                Content = new TextContent { Value = volume.GitRepo.Revision },
            };
            yield return new HeaderedRow
            {
                Header = "Directory",
                Content = new TextContent { Value = volume.GitRepo.Directory },
            };
        }
        else if (volume.Glusterfs != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "GlusterFS" },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Glusterfs.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Endpoints",
                Content = new TextContent { Value = volume.Glusterfs.Endpoints },
            };
            yield return new HeaderedRow
            {
                Header = "Path",
                Content = new TextContent { Value = volume.Glusterfs.Path },
            };
        }
        else if (volume.HostPath != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Host Path" },
            };
            yield return new HeaderedRow
            {
                Header = "Path",
                Content = new TextContent { Value = volume.HostPath.Path },
            };
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = volume.HostPath.Type },
            };
        }
        else if (volume.Image != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Image" },
            };
            yield return new HeaderedRow
            {
                Header = "Reference",
                Content = new TextContent { Value = volume.Image.Reference },
            };
            yield return new HeaderedRow
            {
                Header = "Pull Policy",
                Content = new TextContent { Value = volume.Image.PullPolicy },
            };
        }
        else if (volume.Iscsi != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "iSCSI" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.Iscsi.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Iscsi.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Target Portal",
                Content = new TextContent { Value = volume.Iscsi.TargetPortal },
            };
            yield return new HeaderedRow
            {
                Header = "IQN",
                Content = new TextContent { Value = volume.Iscsi.Iqn },
            };
            yield return new HeaderedRow
            {
                Header = "Lun",
                Content = new TextContent { Value = volume.Iscsi.Lun.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "ISCSI Interface",
                Content = new TextContent { Value = volume.Iscsi.IscsiInterface },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Iscsi.SecretRef?.Name,
                },
            };
            yield return new HeaderedRow
            {
                Header = "Portals",
                Content = new TextContent { Value = string.Join(", ", volume.Iscsi.Portals) },
            };
            yield return new HeaderedRow
            {
                Header = "Initiator Name",
                Content = new TextContent { Value = volume.Iscsi.InitiatorName },
            };
        }
        else if (volume.Nfs != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "NFS" },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Nfs.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Server",
                Content = new TextContent { Value = volume.Nfs.Server },
            };
            yield return new HeaderedRow
            {
                Header = "Path",
                Content = new TextContent { Value = volume.Nfs.Path },
            };
        }
        else if (volume.PersistentVolumeClaim != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Persistent Volume Claim" },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent
                {
                    Value = volume.PersistentVolumeClaim.ReadOnlyProperty.ToString(),
                },
            };
            yield return new HeaderedRow
            {
                Header = "Claim Name",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.PersistentVolumeClaim,
                    ResourceName = volume.PersistentVolumeClaim.ClaimName,
                },
            };
        }
        else if (volume.PhotonPersistentDisk != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Photon Persistent Disk" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.PhotonPersistentDisk.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "PD ID",
                Content = new TextContent { Value = volume.PhotonPersistentDisk.PdID },
            };
        }
        else if (volume.PortworxVolume != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Portworx Volume" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.PortworxVolume.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent
                {
                    Value = volume.PortworxVolume.ReadOnlyProperty.ToString(),
                },
            };
            yield return new HeaderedRow
            {
                Header = "Volume ID",
                Content = new TextContent { Value = volume.PortworxVolume.VolumeID },
            };
        }
        else if (volume.Projected != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Projected" },
            };
            yield return new HeaderedRow
            {
                Header = "Sources", // todo support a link collection
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. volume.Projected.Sources?.Select(s => new TextCollectionElement
                        {
                            Value =
                                s.ConfigMap != null ? $"ConfigMap: {s.ConfigMap.Name}"
                                : s.Secret != null ? $"Secret: {s.Secret.Name}"
                                : s.ServiceAccountToken != null ? "ServiceAccountToken"
                                : "Unknown Source",
                        }) ?? [],
                    ],
                },
            };
        }
        else if (volume.Quobyte != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Quobyte" },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Quobyte.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Registry",
                Content = new TextContent { Value = volume.Quobyte.Registry },
            };
            yield return new HeaderedRow
            {
                Header = "Volume",
                Content = new TextContent { Value = volume.Quobyte.Volume },
            };
            yield return new HeaderedRow
            {
                Header = "User",
                Content = new TextContent { Value = volume.Quobyte.User },
            };
            yield return new HeaderedRow
            {
                Header = "Group",
                Content = new TextContent { Value = volume.Quobyte.Group },
            };
            yield return new HeaderedRow
            {
                Header = "Tenant",
                Content = new TextContent { Value = volume.Quobyte.Tenant },
            };
        }
        else if (volume.Rbd != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "RBD (RADOS Block Device)" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.Rbd.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Rbd.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Pool",
                Content = new TextContent { Value = volume.Rbd.Pool },
            };
            yield return new HeaderedRow
            {
                Header = "Image",
                Content = new TextContent { Value = volume.Rbd.Image },
            };
            yield return new HeaderedRow
            {
                Header = "User",
                Content = new TextContent { Value = volume.Rbd.User },
            };
            yield return new HeaderedRow
            {
                Header = "Keyring",
                Content = new TextContent { Value = volume.Rbd.Keyring },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Rbd.SecretRef?.Name,
                },
            };
            yield return new HeaderedRow
            {
                Header = "Ceph Monitors",
                Content = new TextContent { Value = string.Join(", ", volume.Rbd.Monitors) },
            };
        }
        else if (volume.ScaleIO != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "ScaleIO" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.ScaleIO.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.ScaleIO.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Gateway",
                Content = new TextContent { Value = volume.ScaleIO.Gateway },
            };
            yield return new HeaderedRow
            {
                Header = "System",
                Content = new TextContent { Value = volume.ScaleIO.System },
            };
            yield return new HeaderedRow
            {
                Header = "Volume Name",
                Content = new TextContent { Value = volume.ScaleIO.VolumeName },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.ScaleIO.SecretRef?.Name,
                },
            };
            yield return new HeaderedRow
            {
                Header = "SSLEnabled",
                Content = new TextContent { Value = volume.ScaleIO.SslEnabled.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Protection Domain",
                Content = new TextContent { Value = volume.ScaleIO.ProtectionDomain },
            };
            yield return new HeaderedRow
            {
                Header = "Storage Pool",
                Content = new TextContent { Value = volume.ScaleIO.StoragePool },
            };
            yield return new HeaderedRow
            {
                Header = "Storage Mode",
                Content = new TextContent { Value = volume.ScaleIO.StorageMode },
            };
        }
        else if (volume.Secret != null) // todo items
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Secret" },
            };
            yield return new HeaderedRow
            {
                Header = "Name",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Secret.SecretName,
                },
            };
            yield return new HeaderedRow
            {
                Header = "Default Mode",
                Content = new TextContent { Value = volume.Secret.DefaultMode?.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Optional",
                Content = new TextContent { Value = volume.Secret.Optional?.ToString() },
            };
        }
        else if (volume.Storageos != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "StorageOS" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.Storageos.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Read Only",
                Content = new TextContent { Value = volume.Storageos.ReadOnlyProperty.ToString() },
            };
            yield return new HeaderedRow
            {
                Header = "Volume Name",
                Content = new TextContent { Value = volume.Storageos.VolumeName },
            };
            yield return new HeaderedRow
            {
                Header = "Volume Namespace",
                Content = new TextContent { Value = volume.Storageos.VolumeNamespace },
            };
            yield return new HeaderedRow
            {
                Header = "Secret Ref",
                Content = new LinkContent
                {
                    ResourceType = ResourceType.Secret,
                    ResourceName = volume.Storageos.SecretRef?.Name,
                },
            };
        }
        else if (volume.VsphereVolume != null)
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "vSphere Volume" },
            };
            yield return new HeaderedRow
            {
                Header = "FS Type",
                Content = new TextContent { Value = volume.VsphereVolume.FsType },
            };
            yield return new HeaderedRow
            {
                Header = "Volume Path",
                Content = new TextContent { Value = volume.VsphereVolume.VolumePath },
            };
            yield return new HeaderedRow
            {
                Header = "Storage Policy Name",
                Content = new TextContent { Value = volume.VsphereVolume.StoragePolicyName },
            };
            yield return new HeaderedRow
            {
                Header = "Storage Policy ID",
                Content = new TextContent { Value = volume.VsphereVolume.StoragePolicyID },
            };
        }
        else
        {
            yield return new HeaderedRow
            {
                Header = "Type",
                Content = new TextContent { Value = "Unknown or Unsupported Volume Type" },
            };
        }
    }

    private static string GetEnvValueRepresentation(V1EnvVar envVar)
    {
        if (envVar.Value is not null)
        {
            return envVar.Value;
        }
        if (envVar.ValueFrom is null)
        {
            return string.Empty;
        }
        if (envVar.ValueFrom.FieldRef is not null)
        {
            return $"FieldRef: {envVar.ValueFrom.FieldRef.FieldPath}";
        }
        if (envVar.ValueFrom.ResourceFieldRef is not null)
        {
            return $"ResourceFieldRef: {envVar.ValueFrom.ResourceFieldRef.Resource}";
        }
        if (envVar.ValueFrom.ConfigMapKeyRef is not null)
        {
            return $"ConfigMapKeyRef: {envVar.ValueFrom.ConfigMapKeyRef.Key} ({envVar.ValueFrom.ConfigMapKeyRef.Name})";
        }
        if (envVar.ValueFrom.SecretKeyRef is not null)
        {
            return $"SecretKeyRef: {envVar.ValueFrom.SecretKeyRef.Key} ({envVar.ValueFrom.SecretKeyRef.Name})";
        }
        return string.Empty;
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Resource))
        {
            OnPropertyChanged(nameof(Pod));
            OnPropertyChanged(nameof(ContainerStatuses));
            OnPropertyChanged(nameof(Restarts));
            OnPropertyChanged(nameof(Node));
            OnPropertyChanged(nameof(QoS));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ControlledBy));
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Information,
            Message = "Opening logs for pod {PodName} in namespace {Namespace}"
        )]
        internal static partial void OpeningPodLogs(
            ILogger logger,
            string podName,
            string @namespace
        );

        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Information,
            Message = "Opening shell for pod {PodName} in namespace {Namespace}"
        )]
        internal static partial void OpeningPodShell(
            ILogger logger,
            string podName,
            string @namespace
        );
    }
}
