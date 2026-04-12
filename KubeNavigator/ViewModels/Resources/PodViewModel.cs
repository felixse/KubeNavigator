using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public V1Pod Pod => (V1Pod)Resource;

    public List<V1ContainerStatus> ContainerStatuses =>
        Pod.Status?.ContainerStatuses?.ToList() ?? Enumerable.Empty<V1ContainerStatus>().ToList();

    public int Restarts => Pod.Status.ContainerStatuses?.Sum(c => c.RestartCount) ?? 0;

    public string Node => Pod.Spec.NodeName;

    public string QoS => Pod.Status.QosClass;

    public string Status =>
        Pod.Metadata.DeletionTimestamp is not null ? "Terminating" : Pod.Status.Phase;

    public string ControlledBy =>
        Pod.Metadata.OwnerReferences?.FirstOrDefault()?.Name ?? string.Empty;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        return
        [
            new DetailsSection { Items = [.. GetPodItems()] },
            new GroupedDetailsSection
            {
                Title = "Containers",
                Groups =
                [
                    .. Pod.Spec.Containers.Select(c => new DetailsGroup
                    {
                        Header = new DetailsGroupHeader { Title = c.Name },
                        Items = [.. GetContainerItems(c)],
                    }),
                ],
            },
            new GroupedDetailsSection
            {
                Title = "Volumes",
                Groups =
                [
                    .. Pod.Spec.Volumes?.Select(v => new DetailsGroup
                    {
                        Header = new DetailsGroupHeader { Title = v.Name, Symbol = "\uEDA2" },
                        Items = [.. GetVolumeItems(v)],
                    })
                        ?? [],
                ],
            },
            events,
        ];
    }

    private IEnumerable<IDetailsItem> GetPodItems()
    {
        yield return new DetailsTextItem
        {
            Title = "Created",
            Value = Pod.CreationTimestamp().ToString(),
        };

        yield return new DetailsTextItem { Title = "Name", Value = Pod.Name() };

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
                .. Pod.Metadata.Labels?.Select(l => new DetailsCollectionItemElement
                {
                    Value = $"{l.Key}={l.Value}",
                }) ?? [],
            ],
        };

        if (Pod.Metadata.Annotations?.Count > 0)
        {
            yield return new DetailsCollectionItem
            {
                Title = "Annotations",
                Items =
                [
                    .. Pod.Metadata.Annotations?.Select(l => new DetailsCollectionItemElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            };
        }

        if (Pod.Metadata.OwnerReferences?.Count > 0)
        {
            yield return new DetailsLinkItem
            {
                Title = "Controlled by",
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
            };
        }

        yield return new DetailsTextItem
        {
            Title = "Status",
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
        };

        yield return new DetailsLinkItem
        {
            Title = "Node",
            ResourceName = Pod.Spec.NodeName,
            ResourceType = ResourceType.Node,
        };

        yield return new DetailsTextItem { Title = "Pod IP", Value = Pod.Status.PodIP };

        yield return new DetailsCollectionItem
        {
            Title = "Pod IPs",
            Items =
            [
                .. Pod.Status.PodIPs?.Select(p => new DetailsCollectionItemElement { Value = p.Ip })
                    ?? [],
            ],
        };

        yield return new DetailsLinkItem
        {
            Title = "Service Account",
            ResourceName = Pod.Spec.ServiceAccountName,
            ResourceType = ResourceType.ServiceAccount,
        };

        yield return new DetailsLinkItem
        {
            Title = "Priority Class",
            ResourceName = Pod.Spec.PriorityClassName,
            ResourceType = ResourceType.PriorityClass,
        };

        yield return new DetailsTextItem { Title = "QoS Class", Value = Pod.Status.QosClass };

        yield return new DetailsConditionsItem
        {
            Title = "Conditions",
            Items =
            [
                .. Pod.Status.Conditions?.Select(c => new DetailsConditionsElement
                {
                    Type = c.Type,
                    Status = c.Status,
                    Message = c.Message,
                    Reason = c.Reason,
                    LastHeartbeatTime = c.LastProbeTime,
                    LastTransitionTime = c.LastTransitionTime,
                }) ?? [],
            ],
        };

        yield return new DetailsCollectionItem
        {
            Title = "Node Selector",
            Items =
            [
                .. Pod.Spec.NodeSelector?.Select(n => new DetailsCollectionItemElement
                {
                    Value = $"{n.Key}: {n.Value}",
                }) ?? [],
            ],
        };

        yield return new DetailsTableItem(
            "Tolerations",
            isExpandable: true,
            ["Key", "Operator", "Value", "Effect", "Seconds"],
            Pod.Spec.Tolerations?.Select(t =>
                new[]
                {
                    t.Key,
                    t.OperatorProperty,
                    t.Value,
                    t.Effect,
                    t.TolerationSeconds?.ToString() ?? string.Empty,
                }
            )
                ?? []
        );

        // todo affinities?
    }

    private IEnumerable<IDetailsItem> GetContainerItems(V1Container container)
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

        yield return new DetailsTextItem
        {
            Title = "Status",
            ValueColor = status.State switch
            {
                V1ContainerState { Running: V1ContainerStateRunning } => Category.Success,
                V1ContainerState { Terminated: V1ContainerStateTerminated } => Category.Error,
                _ => Category.Warning,
            },
            Value = statusText,
        };

        if (status.LastState is not null)
        {
            yield return new DetailsTextItem
            {
                Title = "Last Status",
                Value = Pod
                    .Status.ContainerStatuses.First(s => s.Name == container.Name)
                    .LastState switch
                {
                    V1ContainerState { Terminated: V1ContainerStateTerminated } => $"terminated\r\n"
                        + $"Reason: {status.LastState.Terminated.Reason}\r\n"
                        + $"Started at: {status.LastState.Terminated.StartedAt}\r\n"
                        + $"Finished at: {status.LastState.Terminated.FinishedAt}",
                    _ => "Unknown",
                },
            };
        }

        yield return new DetailsTextItem { Title = "Image", Value = container.Image };

        if (container.Ports != null)
        {
            yield return new DetailsPortsItem
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
            };
        }

        yield return new DetailsDictionaryItem
        {
            Title = "Environment",
            Items =
                container
                    .Env?.Select(e => new DetailsDictionaryEntry
                    {
                        Key = e.Name,
                        Value = GetEnvValueRepresentation(e),
                    })
                    .ToList()
                ?? [],
        };

        yield return new DetailsCollectionItem
        {
            Title = "Mounts",
            IsWrapLayout = false,
            Items =
            [
                .. container.VolumeMounts.Select(m => new DetailsCollectionItemElement
                {
                    Value = m.MountPath,
                    SecondaryValue =
                        $"from {m.Name} ({(m.ReadOnlyProperty == true ? "ro" : "rw")})",
                }),
            ],
        };

        if (container.LivenessProbe != null)
        {
            var elements = new List<DetailsCollectionItemElement>();
            if (container.LivenessProbe.HttpGet != null)
            {
                elements.Add(new DetailsCollectionItemElement { Value = "http-get" });
                elements.Add(
                    new DetailsCollectionItemElement
                    {
                        Value =
                            $"{container.LivenessProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.LivenessProbe.HttpGet.Host}{container.LivenessProbe.HttpGet.Path}:{container.LivenessProbe.HttpGet.Port}",
                    }
                );
                elements.Add(
                    new DetailsCollectionItemElement
                    {
                        Value = $"port: {container.LivenessProbe.HttpGet.Port}",
                    }
                );
            }

            // todo fill for exec

            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"delay={container.LivenessProbe.InitialDelaySeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"timeout={container.LivenessProbe.TimeoutSeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"period={container.LivenessProbe.PeriodSeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"#success={container.LivenessProbe.SuccessThreshold}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"#failure={container.LivenessProbe.FailureThreshold}s",
                }
            );

            yield return new DetailsCollectionItem { Title = "Liveness", Items = elements };
        }

        if (container.ReadinessProbe != null)
        {
            var elements = new List<DetailsCollectionItemElement>();
            if (container.ReadinessProbe.HttpGet != null)
            {
                elements.Add(new DetailsCollectionItemElement { Value = "http-get" });
                elements.Add(
                    new DetailsCollectionItemElement
                    {
                        Value =
                            $"{container.ReadinessProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.ReadinessProbe.HttpGet.Host}{container.ReadinessProbe.HttpGet.Path}:{container.ReadinessProbe.HttpGet.Port}",
                    }
                );
                elements.Add(
                    new DetailsCollectionItemElement
                    {
                        Value = $"port: {container.ReadinessProbe.HttpGet.Port}",
                    }
                );
            }

            // todo fill for exec

            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"delay={container.ReadinessProbe.InitialDelaySeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"timeout={container.ReadinessProbe.TimeoutSeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"period={container.ReadinessProbe.PeriodSeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"#success={container.ReadinessProbe.SuccessThreshold}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"#failure={container.ReadinessProbe.FailureThreshold}s",
                }
            );

            yield return new DetailsCollectionItem { Title = "Readiness", Items = elements };
        }

        if (container.StartupProbe != null)
        {
            var elements = new List<DetailsCollectionItemElement>();
            if (container.StartupProbe.HttpGet != null)
            {
                elements.Add(new DetailsCollectionItemElement { Value = "http-get" });
                elements.Add(
                    new DetailsCollectionItemElement
                    {
                        Value =
                            $"{container.StartupProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.StartupProbe.HttpGet.Host}{container.StartupProbe.HttpGet.Path}:{container.StartupProbe.HttpGet.Port}",
                    }
                );
                elements.Add(
                    new DetailsCollectionItemElement
                    {
                        Value = $"port: {container.StartupProbe.HttpGet.Port}",
                    }
                );
            }

            // todo fill for exec

            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"delay={container.StartupProbe.InitialDelaySeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"timeout={container.StartupProbe.TimeoutSeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"period={container.StartupProbe.PeriodSeconds}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"#success={container.StartupProbe.SuccessThreshold}s",
                }
            );
            elements.Add(
                new DetailsCollectionItemElement
                {
                    Value = $"#failure={container.StartupProbe.FailureThreshold}s",
                }
            );

            yield return new DetailsCollectionItem { Title = "Startup", Items = elements };
        }

        if (container.Command != null)
        {
            yield return new DetailsTextItem
            {
                Title = "Command",
                Value = string.Join(" ", container.Command),
            };
        }

        if (container.Args != null)
        {
            yield return new DetailsTextItem
            {
                Title = "Args",
                Value = string.Join(" ", container.Args),
            };
        }

        if (container.Resources.Requests != null)
        {
            yield return new DetailsCollectionItem
            {
                Title = "Requests",
                Items =
                [
                    .. container.Resources.Requests.Select(r => new DetailsCollectionItemElement
                    {
                        Value = $"{r.Key}={r.Value}",
                    }),
                ],
            };
        }
    }

    private static IEnumerable<IDetailsItem> GetVolumeItems(V1Volume volume)
    {
        if (volume.AwsElasticBlockStore != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "AWS Elastic Block Store" };
            yield return new DetailsTextItem
            {
                Title = "FS Type",
                Value = volume.AwsElasticBlockStore.FsType,
            };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.AwsElasticBlockStore.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Partition",
                Value = volume.AwsElasticBlockStore.Partition.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Volume ID",
                Value = volume.AwsElasticBlockStore.VolumeID,
            };
        }
        else if (volume.AzureDisk != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Azure Disk" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.AzureDisk.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.AzureDisk.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem { Title = "Kind", Value = volume.AzureDisk.Kind };
            yield return new DetailsTextItem
            {
                Title = "Disk Name",
                Value = volume.AzureDisk.DiskName,
            };
            yield return new DetailsTextItem
            {
                Title = "Disk URI",
                Value = volume.AzureDisk.DiskURI,
            };
            yield return new DetailsTextItem
            {
                Title = "Caching Mode",
                Value = volume.AzureDisk.CachingMode,
            };
        }
        else if (volume.AzureFile != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Azure File" };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.AzureFile.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Share Name",
                Value = volume.AzureFile.ShareName,
            };

            yield return new DetailsTextItem
            {
                Title = "Secret Name",
                Value = volume.AzureFile.SecretName,
            };
        }
        else if (volume.Cephfs != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "CephFS" };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Cephfs.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Monitors",
                Value = string.Join(", ", volume.Cephfs.Monitors),
            };
            yield return new DetailsTextItem { Title = "Path", Value = volume.Cephfs.Path };
            yield return new DetailsTextItem { Title = "User", Value = volume.Cephfs.User };
            yield return new DetailsTextItem
            {
                Title = "Secret File",
                Value = volume.Cephfs.SecretFile,
            };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Cephfs.SecretRef?.Name,
            };
        }
        else if (volume.Cinder != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Cinder" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.Cinder.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Cinder.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Volume ID",
                Value = volume.Cinder.VolumeID,
            };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Cinder.SecretRef?.Name,
            };
        }
        else if (volume.ConfigMap != null) // todo items
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Config Map" };
            yield return new DetailsLinkItem
            {
                Title = "Name",
                ResourceType = ResourceType.ConfigMap,
                ResourceName = volume.ConfigMap.Name,
            };
            yield return new DetailsTextItem
            {
                Title = "Default Mode",
                Value = volume.ConfigMap.DefaultMode?.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Optional",
                Value = volume.ConfigMap.Optional?.ToString(),
            };
        }
        else if (volume.Csi != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "CSI" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.Csi.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Csi.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem { Title = "Driver", Value = volume.Csi.Driver };
            yield return new DetailsLinkItem
            {
                Title = "Node Publish Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Csi.NodePublishSecretRef?.Name,
            };
            yield return new DetailsDictionaryItem
            {
                Title = "Volume Attributes",
                Items =
                    volume
                        .Csi.VolumeAttributes?.Select(va => new DetailsDictionaryEntry
                        {
                            Key = va.Key,
                            Value = va.Value,
                        })
                        .ToList()
                    ?? [],
            };
        }
        else if (volume.DownwardAPI != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Downward API" };
            yield return new DetailsDictionaryItem
            {
                Title = "Default Mode",
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
            };
            yield return new DetailsCollectionItem
            {
                Title = "Items",
                Items =
                    volume
                        .DownwardAPI.Items?.Select(i => new DetailsCollectionItemElement
                        {
                            Value = i.Path,
                            SecondaryValue =
                                i.FieldRef != null ? $"FieldRef: {i.FieldRef.FieldPath}"
                                : i.ResourceFieldRef != null
                                    ? $"ResourceFieldRef: {i.ResourceFieldRef.Resource}"
                                : string.Empty,
                        })
                        .ToList()
                    ?? [],
            };
        }
        else if (volume.EmptyDir != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Empty Dir" };
            yield return new DetailsTextItem { Title = "Medium", Value = volume.EmptyDir.Medium };
            if (volume.EmptyDir.SizeLimit != null)
            {
                yield return new DetailsTextItem
                {
                    Title = "Size Limit",
                    Value = volume.EmptyDir.SizeLimit.ToString(),
                };
            }
        }
        else if (volume.Ephemeral != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Ephemeral" };
            if (volume.Ephemeral.VolumeClaimTemplate != null)
            {
                yield return new DetailsLinkItem
                {
                    Title = "Volume Claim Template",
                    ResourceType = ResourceType.PersistentVolumeClaim,
                    ResourceName = volume.Ephemeral.VolumeClaimTemplate.Metadata?.Name,
                };
            }
        }
        else if (volume.Fc != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Fibre Channel" };
            yield return new DetailsTextItem { Title = "FSType", Value = volume.Fc.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Fc.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Target WWNs",
                Value = string.Join(", ", volume.Fc.TargetWWNs),
            };
            yield return new DetailsTextItem { Title = "Lun", Value = volume.Fc.Lun.ToString() };

            yield return new DetailsTextItem
            {
                Title = "Wwids",
                Value = string.Join(", ", volume.Fc.Wwids),
            };
        }
        else if (volume.FlexVolume != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Flex Volume" };
            yield return new DetailsTextItem { Title = "Driver", Value = volume.FlexVolume.Driver };
            yield return new DetailsTextItem
            {
                Title = "FS Type",
                Value = volume.FlexVolume.FsType,
            };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.FlexVolume.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsDictionaryItem
            {
                Title = "Options",
                Items =
                    volume
                        .FlexVolume.Options?.Select(o => new DetailsDictionaryEntry
                        {
                            Key = o.Key,
                            Value = o.Value,
                        })
                        .ToList()
                    ?? [],
            };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.FlexVolume.SecretRef?.Name,
            };
        }
        else if (volume.Flocker != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Flocker" };
            yield return new DetailsTextItem
            {
                Title = "Dataset Name",
                Value = volume.Flocker.DatasetName,
            };
            yield return new DetailsTextItem
            {
                Title = "Dataset UUID",
                Value = volume.Flocker.DatasetUUID,
            };
        }
        else if (volume.GcePersistentDisk != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "GCE Persistent Disk" };
            yield return new DetailsTextItem
            {
                Title = "FS Type",
                Value = volume.GcePersistentDisk.FsType,
            };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.GcePersistentDisk.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Partition",
                Value = volume.GcePersistentDisk.Partition.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "PD Name",
                Value = volume.GcePersistentDisk.PdName,
            };
        }
        else if (volume.GitRepo != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Git Repo" };
            yield return new DetailsTextItem
            {
                Title = "Repository",
                Value = volume.GitRepo.Repository,
            };
            yield return new DetailsTextItem
            {
                Title = "Revision",
                Value = volume.GitRepo.Revision,
            };
            yield return new DetailsTextItem
            {
                Title = "Directory",
                Value = volume.GitRepo.Directory,
            };
        }
        else if (volume.Glusterfs != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "GlusterFS" };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Glusterfs.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Endpoints",
                Value = volume.Glusterfs.Endpoints,
            };
            yield return new DetailsTextItem { Title = "Path", Value = volume.Glusterfs.Path };
        }
        else if (volume.HostPath != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Host Path" };
            yield return new DetailsTextItem { Title = "Path", Value = volume.HostPath.Path };
            yield return new DetailsTextItem { Title = "Type", Value = volume.HostPath.Type };
        }
        else if (volume.Image != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Image" };
            yield return new DetailsTextItem
            {
                Title = "Reference",
                Value = volume.Image.Reference,
            };
            yield return new DetailsTextItem
            {
                Title = "Pull Policy",
                Value = volume.Image.PullPolicy,
            };
        }
        else if (volume.Iscsi != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "iSCSI" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.Iscsi.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Iscsi.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Target Portal",
                Value = volume.Iscsi.TargetPortal,
            };
            yield return new DetailsTextItem { Title = "IQN", Value = volume.Iscsi.Iqn };
            yield return new DetailsTextItem { Title = "Lun", Value = volume.Iscsi.Lun.ToString() };
            yield return new DetailsTextItem
            {
                Title = "ISCSI Interface",
                Value = volume.Iscsi.IscsiInterface,
            };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Iscsi.SecretRef?.Name,
            };
            yield return new DetailsTextItem
            {
                Title = "Portals",
                Value = string.Join(", ", volume.Iscsi.Portals),
            };
            yield return new DetailsTextItem
            {
                Title = "Initiator Name",
                Value = volume.Iscsi.InitiatorName,
            };
        }
        else if (volume.Nfs != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "NFS" };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Nfs.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem { Title = "Server", Value = volume.Nfs.Server };
            yield return new DetailsTextItem { Title = "Path", Value = volume.Nfs.Path };
        }
        else if (volume.PersistentVolumeClaim != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Persistent Volume Claim" };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.PersistentVolumeClaim.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsLinkItem
            {
                Title = "Claim Name",
                ResourceType = ResourceType.PersistentVolumeClaim,
                ResourceName = volume.PersistentVolumeClaim.ClaimName,
            };
        }
        else if (volume.PhotonPersistentDisk != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Photon Persistent Disk" };
            yield return new DetailsTextItem
            {
                Title = "FS Type",
                Value = volume.PhotonPersistentDisk.FsType,
            };
            yield return new DetailsTextItem
            {
                Title = "PD ID",
                Value = volume.PhotonPersistentDisk.PdID,
            };
        }
        else if (volume.PortworxVolume != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Portworx Volume" };
            yield return new DetailsTextItem
            {
                Title = "FS Type",
                Value = volume.PortworxVolume.FsType,
            };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.PortworxVolume.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Volume ID",
                Value = volume.PortworxVolume.VolumeID,
            };
        }
        else if (volume.Projected != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Projected" };
            yield return new DetailsCollectionItem // todo support a link collection
            {
                Title = "Sources",
                Items =
                    volume
                        .Projected.Sources?.Select(s => new DetailsCollectionItemElement
                        {
                            Value =
                                s.ConfigMap != null ? $"ConfigMap: {s.ConfigMap.Name}"
                                : s.Secret != null ? $"Secret: {s.Secret.Name}"
                                : s.ServiceAccountToken != null ? "ServiceAccountToken"
                                : "Unknown Source",
                        })
                        .ToList()
                    ?? [],
            };
        }
        else if (volume.Quobyte != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Quobyte" };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Quobyte.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Registry",
                Value = volume.Quobyte.Registry,
            };
            yield return new DetailsTextItem { Title = "Volume", Value = volume.Quobyte.Volume };
            yield return new DetailsTextItem { Title = "User", Value = volume.Quobyte.User };
            yield return new DetailsTextItem { Title = "Group", Value = volume.Quobyte.Group };
            yield return new DetailsTextItem { Title = "Tenant", Value = volume.Quobyte.Tenant };
        }
        else if (volume.Rbd != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "RBD (RADOS Block Device)" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.Rbd.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Rbd.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem { Title = "Pool", Value = volume.Rbd.Pool };
            yield return new DetailsTextItem { Title = "Image", Value = volume.Rbd.Image };
            yield return new DetailsTextItem { Title = "User", Value = volume.Rbd.User };
            yield return new DetailsTextItem { Title = "Keyring", Value = volume.Rbd.Keyring };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Rbd.SecretRef?.Name,
            };
            yield return new DetailsTextItem
            {
                Title = "Ceph Monitors",
                Value = string.Join(", ", volume.Rbd.Monitors),
            };
        }
        else if (volume.ScaleIO != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "ScaleIO" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.ScaleIO.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.ScaleIO.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem { Title = "Gateway", Value = volume.ScaleIO.Gateway };
            yield return new DetailsTextItem { Title = "System", Value = volume.ScaleIO.System };
            yield return new DetailsTextItem
            {
                Title = "Volume Name",
                Value = volume.ScaleIO.VolumeName,
            };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.ScaleIO.SecretRef?.Name,
            };
            yield return new DetailsTextItem
            {
                Title = "SSLEnabled",
                Value = volume.ScaleIO.SslEnabled.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Protection Domain",
                Value = volume.ScaleIO.ProtectionDomain,
            };
            yield return new DetailsTextItem
            {
                Title = "Storage Pool",
                Value = volume.ScaleIO.StoragePool,
            };
            yield return new DetailsTextItem
            {
                Title = "Storage Mode",
                Value = volume.ScaleIO.StorageMode,
            };
        }
        else if (volume.Secret != null) // todo items
        {
            yield return new DetailsTextItem { Title = "Type", Value = "Secret" };
            yield return new DetailsLinkItem
            {
                Title = "Name",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Secret.SecretName,
            };
            yield return new DetailsTextItem
            {
                Title = "Default Mode",
                Value = volume.Secret.DefaultMode?.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Optional",
                Value = volume.Secret.Optional?.ToString(),
            };
        }
        else if (volume.Storageos != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "StorageOS" };
            yield return new DetailsTextItem { Title = "FS Type", Value = volume.Storageos.FsType };
            yield return new DetailsTextItem
            {
                Title = "Read Only",
                Value = volume.Storageos.ReadOnlyProperty.ToString(),
            };
            yield return new DetailsTextItem
            {
                Title = "Volume Name",
                Value = volume.Storageos.VolumeName,
            };
            yield return new DetailsTextItem
            {
                Title = "Volume Namespace",
                Value = volume.Storageos.VolumeNamespace,
            };
            yield return new DetailsLinkItem
            {
                Title = "Secret Ref",
                ResourceType = ResourceType.Secret,
                ResourceName = volume.Storageos.SecretRef?.Name,
            };
        }
        else if (volume.VsphereVolume != null)
        {
            yield return new DetailsTextItem { Title = "Type", Value = "vSphere Volume" };
            yield return new DetailsTextItem
            {
                Title = "FS Type",
                Value = volume.VsphereVolume.FsType,
            };
            yield return new DetailsTextItem
            {
                Title = "Volume Path",
                Value = volume.VsphereVolume.VolumePath,
            };
            yield return new DetailsTextItem
            {
                Title = "Storage Policy Name",
                Value = volume.VsphereVolume.StoragePolicyName,
            };
            yield return new DetailsTextItem
            {
                Title = "Storage Policy ID",
                Value = volume.VsphereVolume.StoragePolicyID,
            };
        }
        else
        {
            yield return new DetailsTextItem
            {
                Title = "Type",
                Value = "Unknown or Unsupported Volume Type",
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
