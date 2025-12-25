using CommunityToolkit.Mvvm.Input;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.Model.Details;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Radios;

namespace KubeNavigator.ViewModels.Resources;

public partial class PodViewModel : KubernetesResourceViewModel
{
    private readonly ILogger<PodViewModel> _logger;

    public PodViewModel(V1Pod resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Pod, cluster)
    {
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<PodViewModel>();
        
        Commands.Insert(0, new ItemCommand { Name = "Show Logs", Symbol = "List", Command = ShowLogsCommand });
        Commands.Insert(1, new ItemCommand { Name = "Open Shell", Symbol = "Play", Command = OpenShellCommand });

        PropertyChanged += OnPropertyChanged;
    }

    [RelayCommand]
    public void ShowLogs()
    {
        Log.OpeningPodLogs(_logger, Pod.Name(), Pod.Namespace());
        Cluster.App.WindowManager.ActiveWindow.ShelfHost.OpenShelfItem(new PodLogsViewModel(this, Cluster, Cluster.App.ThemeManager));
    }

    [RelayCommand]
    public void OpenShell()
    {
        Log.OpeningPodShell(_logger, Pod.Name(), Pod.Namespace());
        Cluster.App.WindowManager.ActiveWindow.ShelfHost.OpenShelfItem(new PodShellViewModel(this, Cluster, Cluster.App.ThemeManager));
    }

    public V1Pod Pod => (V1Pod)Resource;

    public IEnumerable<V1ContainerStatus> ContainerStatuses => Pod.Status.ContainerStatuses ?? Enumerable.Empty<V1ContainerStatus>();

    public int Restarts => Pod.Status.ContainerStatuses?.Sum(c => c.RestartCount) ?? 0;

    public string Node => Pod.Spec.NodeName;

    public string QoS => Pod.Status.QosClass;

    public string Status => Pod.Metadata.DeletionTimestamp is not null ? "Terminating" : Pod.Status.Phase;

    public string ControlledBy => Pod.Metadata.OwnerReferences?.FirstOrDefault()?.Name ?? string.Empty;


    protected override IReadOnlyCollection<IDetailsSection> CreateDetails()
    {
        return [
            new DetailsSection {
                Items = [.. GetPodItems()]
            },
            new GroupedDetailsSection {
                Title = "Containers",
                Groups = [.. Pod.Spec.Containers.Select(c => new DetailsGroup {
                    Title = c.Name,
                    Items = [.. GetContainerItems(c)]
                })]
            }
        ];
    }

    private IEnumerable<IDetailsItem> GetPodItems()
    {
        yield return new DetailsTextItem
        {
            Title = "Created",
            Value = Pod.CreationTimestamp().ToString(),
        };

        yield return new DetailsTextItem
        {
            Title = "Name",
            Value = Pod.Name(),
        };

        yield return new DetailsLinkItem
        {
            Title = "Namespace",
            ResourceName = Resource.Namespace(),
            ResourceType = ResourceType.Namespace,
        };

        yield return new DetailsCollectionItem
        {
            Title = "Labels",
            Items = [.. Pod.Metadata.Labels?.Select(l => new DetailsCollectionItemElement { Value = $"{l.Key}={l.Value}" }) ?? []]
        };

        if (Pod.Metadata.Annotations?.Count > 0)
        {
            yield return new DetailsCollectionItem
            {
                Title = "Annotations",
                Items = [.. Pod.Metadata.Annotations?.Select(l => new DetailsCollectionItemElement { Value = $"{l.Key}={l.Value}" }) ?? []]
            };
        }

        if (Pod.Metadata.OwnerReferences?.Count > 0)
        {
            yield return new DetailsLinkItem
            {
                Title = "Controlled by",
                Prefix = $"{Pod.Metadata.OwnerReferences.First().Kind}: ",
                ResourceName = Pod.Metadata.OwnerReferences.First().Name,
                ResourceType = Pod.Metadata.OwnerReferences.First(o => o.Controller == true).Kind switch
                {
                    "ReplicaSet" => ResourceType.ReplicaSet,
                    "Deployment" => ResourceType.Deployment,
                    "DaemonSet" => ResourceType.DaemonSet,
                    "Node" => ResourceType.Node,
                    _ => new ResourceType("Unknown", "Unknown", "unknown", true, "Unknowns", "Unknown")
                }
            };
        }

        yield return new DetailsTextItem
        {
            Title = "Status",
            Value = Status,
            ValueColor = Status switch
            {
                "Running" => DetailsTextItem.Color.Success,
                "Succeeded" => DetailsTextItem.Color.Success,
                "Failed" => DetailsTextItem.Color.Error,
                "Pending" => DetailsTextItem.Color.Warning,
                "Terminating" => DetailsTextItem.Color.Default,
                _ => DetailsTextItem.Color.Default
            }

        };

        yield return new DetailsLinkItem
        {
            Title = "Node",
            ResourceName = Pod.Spec.NodeName,
            ResourceType = ResourceType.Node,
        };

        yield return new DetailsTextItem
        {
            Title = "Pod IP",
            Value = Pod.Status.PodIP,
        };

        yield return new DetailsCollectionItem
        {
            Title = "Pod IPs",
            Items = [.. Pod.Status.PodIPs?.Select(p => new DetailsCollectionItemElement { Value = p.Ip }) ?? []],
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

        yield return new DetailsTextItem
        {
            Title = "QoS Class",
            Value = Pod.Status.QosClass,
        };

        yield return new DetailsCollectionItem
        {
            Title = "Conditions",
            Items = [.. Pod.Status.Conditions?.Select(c => new DetailsCollectionItemElement { Value = $"{c.Type}: {c.Status}" }) ?? []]
        };

        yield return new DetailsCollectionItem
        {
            Title = "Node Selector",
            Items = [.. Pod.Spec.NodeSelector?.Select(n => new DetailsCollectionItemElement { Value = $"{n.Key}: {n.Value}" }) ?? []],
        };

        yield return new DetailsTableItem("Tolerations", ["Key", "Operator", "Value", "Effect", "Seconds"], Pod.Spec.Tolerations?.Select(t => new[] { t.Key, t.OperatorProperty, t.Value, t.Effect, t.TolerationSeconds.ToString() }) ?? []);

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
            _ => "Waiting"
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
                V1ContainerState { Running: V1ContainerStateRunning } => DetailsTextItem.Color.Success,
                V1ContainerState { Terminated: V1ContainerStateTerminated } => DetailsTextItem.Color.Error,
                _ => DetailsTextItem.Color.Warning
            },
            Value = statusText
        };

        if (status.LastState is not null)
        {
            yield return new DetailsTextItem
            {
                Title = "Last Status",
                Value = Pod.Status.ContainerStatuses.First(s => s.Name == container.Name).LastState switch
                {
                    V1ContainerState { Terminated: V1ContainerStateTerminated } => $"terminated\r\n" +
                    $"Reason: {status.LastState.Terminated.Reason}\r\n" +
                    $"Started at: {status.LastState.Terminated.StartedAt}\r\n" +
                    $"Finished at: {status.LastState.Terminated.FinishedAt}",
                    _ => "Unknown"
                }
            };
        }

        yield return new DetailsTextItem
        {
            Title = "Image",
            Value = container.Image
        };

        if (container.Ports != null)
        {
            yield return new DetailsPortsItem
            {
                Ports = [.. container.Ports.Select(p => new PortViewModel(p, this, Cluster, Cluster.App.ForwardedPorts.FirstOrDefault(fp => fp.Pod == this && fp.Port == p)))]
            };
        }

        yield return new DetailsDictionaryItem { Title = "Environment", Items = container.Env?.Select(e => new DetailsDictionaryEntry { Key = e.Name, Value = GetEnvValueRepresentation(e) }).ToList() ?? [] };

        yield return new DetailsCollectionItem { Title = "Mounts", IsWrapLayout = false, Items = [.. container.VolumeMounts.Select(m => new DetailsCollectionItemElement { Value = m.MountPath, SecondaryValue = $"from {m.Name} ({(m.ReadOnlyProperty == true ? "ro" : "rw")})" })] };

        if (container.LivenessProbe != null)
        {
            var elements = new List<DetailsCollectionItemElement>();
            if (container.LivenessProbe.HttpGet != null)
            {
                elements.Add(new DetailsCollectionItemElement { Value = "http-get" });
                elements.Add(new DetailsCollectionItemElement { Value = $"{container.LivenessProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.LivenessProbe.HttpGet.Host}{container.LivenessProbe.HttpGet.Path}:{container.LivenessProbe.HttpGet.Port}" });
                elements.Add(new DetailsCollectionItemElement { Value = $"port: {container.LivenessProbe.HttpGet.Port}" });
            }

            // todo fill for exec

            elements.Add(new DetailsCollectionItemElement { Value = $"delay={container.LivenessProbe.InitialDelaySeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"timeout={container.LivenessProbe.TimeoutSeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"period={container.LivenessProbe.PeriodSeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"#success={container.LivenessProbe.SuccessThreshold}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"#failure={container.LivenessProbe.FailureThreshold}s" });

            yield return new DetailsCollectionItem { Title = "Liveness", Items = elements };
        }

        if (container.ReadinessProbe != null)
        {
            var elements = new List<DetailsCollectionItemElement>();
            if (container.ReadinessProbe.HttpGet != null)
            {
                elements.Add(new DetailsCollectionItemElement { Value = "http-get" });
                elements.Add(new DetailsCollectionItemElement { Value = $"{container.ReadinessProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.ReadinessProbe.HttpGet.Host}{container.ReadinessProbe.HttpGet.Path}:{container.ReadinessProbe.HttpGet.Port}" });
                elements.Add(new DetailsCollectionItemElement { Value = $"port: {container.ReadinessProbe.HttpGet.Port}" });
            }

            // todo fill for exec

            elements.Add(new DetailsCollectionItemElement { Value = $"delay={container.ReadinessProbe.InitialDelaySeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"timeout={container.ReadinessProbe.TimeoutSeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"period={container.ReadinessProbe.PeriodSeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"#success={container.ReadinessProbe.SuccessThreshold}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"#failure={container.ReadinessProbe.FailureThreshold}s" });

            yield return new DetailsCollectionItem { Title = "Readiness", Items = elements };
        }

        if (container.StartupProbe != null)
        {
            var elements = new List<DetailsCollectionItemElement>();
            if (container.StartupProbe.HttpGet != null)
            {
                elements.Add(new DetailsCollectionItemElement { Value = "http-get" });
                elements.Add(new DetailsCollectionItemElement { Value = $"{container.StartupProbe.HttpGet.Scheme.ToLowerInvariant()}://{container.StartupProbe.HttpGet.Host}{container.StartupProbe.HttpGet.Path}:{container.StartupProbe.HttpGet.Port}" });
                elements.Add(new DetailsCollectionItemElement { Value = $"port: {container.StartupProbe.HttpGet.Port}" });
            }

            // todo fill for exec

            elements.Add(new DetailsCollectionItemElement { Value = $"delay={container.StartupProbe.InitialDelaySeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"timeout={container.StartupProbe.TimeoutSeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"period={container.StartupProbe.PeriodSeconds}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"#success={container.StartupProbe.SuccessThreshold}s" });
            elements.Add(new DetailsCollectionItemElement { Value = $"#failure={container.StartupProbe.FailureThreshold}s" });

            yield return new DetailsCollectionItem { Title = "Startup", Items = elements };
        }

        if (container.Command != null)
        {
            yield return new DetailsTextItem { Title = "Command", Value = string.Join(" ", container.Command) };
        }

        if (container.Args != null)
        {
            yield return new DetailsTextItem { Title = "Args", Value = string.Join(" ", container.Args) };
        }

        if (container.Resources.Requests != null)
        {
            yield return new DetailsCollectionItem { Title = "Requests", Items = [.. container.Resources.Requests.Select(r => new DetailsCollectionItemElement { Value = $"{r.Key}={r.Value}"})] };
        }
    }

    private string GetEnvValueRepresentation(V1EnvVar envVar)
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
            Message = "Opening logs for pod {PodName} in namespace {Namespace}")]
        internal static partial void OpeningPodLogs(ILogger logger, string podName, string @namespace);

        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Information,
            Message = "Opening shell for pod {PodName} in namespace {Namespace}")]
        internal static partial void OpeningPodShell(ILogger logger, string podName, string @namespace);
    }
}
