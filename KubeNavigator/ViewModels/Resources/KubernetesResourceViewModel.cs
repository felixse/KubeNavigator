using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Model;
using KubeNavigator.Model.Details;
using KubeNavigator.ViewModels.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Resources;

public partial class KubernetesResourceViewModel : ObservableObject, ISelectable
{
    public string Name => Resource.Name();
    public string Namespace => Resource.Namespace();

    [ObservableProperty]
    public partial string Age { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial IReadOnlyCollection<IDetailsSection> Details { get; private set; } = [];

    [ObservableProperty]
    public partial IKubernetesObject<V1ObjectMeta> Resource { get; set; }
    public ResourceType ResourceType { get; }
    public ClusterViewModel Cluster { get; }

    public List<ItemCommand> Commands { get; } = [];
    public KubernetesResourceViewModel(IKubernetesObject<V1ObjectMeta> resource, ResourceType resourceType, ClusterViewModel cluster)
    {
        Commands.Add(new ItemCommand { Name = "Edit", Symbol = "Edit", Command = EditCommand });
        Commands.Add(new ItemCommand { Name = "Delete", Symbol = "Delete", Command = DeleteCommand });
        Cluster = cluster;
        Resource = resource;
        ResourceType = resourceType;
        Age = CalculateAge();
    }

    [RelayCommand]
    public void Edit()
    {
        Cluster.App.WindowManager.ActiveWindow.ShelfHost.OpenShelfItem(new EditKubernetesResourceViewModel(this));
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        await Cluster.DeleteResourcesAsync(ResourceType, [this]);
    }

    public void UpdateDetails()
    {
        Details = CreateDetails(); // todo remove details property, let the detailsviewmodel handle this instead
    }

    protected virtual IReadOnlyCollection<IDetailsSection> CreateDetails()
    {
        return [
            new DetailsSection{
                Items = [
                    new DetailsTextItem {
                        Title = "Created",
                        Value = Resource.CreationTimestamp().ToString()
                    },
                    new DetailsTextItem {
                        Title = "Name",
                        Value = Resource.Name()
                    },
                    new DetailsLinkItem {
                        Title = "Namespace",
                        ResourceName = Resource.Namespace(),
                        ResourceType = ResourceType.Namespace
                    },
                    new DetailsCollectionItem {
                        Title = "Annotations",
                        Items = [.. Resource.Metadata.Annotations?.Select(a => new DetailsCollectionItemElement { Value = $"{a.Key}={a.Value}" }) ?? []]
                    }
                ]
            }
        ];
    }

    public string CalculateAge()
    {
        var nullableAge = DateTime.UtcNow - Resource.CreationTimestamp();
        if (nullableAge is TimeSpan age)
        {
            return FormatDuration(age);
        }

        return string.Empty;
    }

    partial void OnResourceChanged(IKubernetesObject<V1ObjectMeta> value)
    {
        UpdateDetails();
    }

    private static string FormatDuration(TimeSpan duration, bool compact = true)
    {
        var totalSeconds = (int)Math.Floor(duration.TotalSeconds);
        var separator = compact ? "" : " ";

        if (totalSeconds < 0)
        {
            return "0s";
        }
        else if (totalSeconds < 60 * 2)
        {
            return $"{totalSeconds}s";
        }

        var totalMinutes = (int)Math.Floor(duration.TotalMinutes);

        if (totalMinutes < 10)
        {
            var seconds = duration.Seconds;
            return GetMeaningfulValues([totalMinutes, seconds], ["m", "s"], separator);
        }
        else if (totalMinutes < 60 * 3)
        {
            if (!compact)
            {
                return GetMeaningfulValues([totalMinutes, duration.Seconds], ["m", "s"], separator);
            }
            return $"{totalMinutes}m";
        }

        var totalHours = (int)Math.Floor(duration.TotalHours);

        if (totalHours < 8)
        {
            var minutes = duration.Minutes;
            return GetMeaningfulValues([totalHours, minutes], ["h", "m"], separator);
        }
        else if (totalHours < 48)
        {
            if (compact)
            {
                return $"{totalHours}h";
            }
            else
            {
                return GetMeaningfulValues([totalHours, duration.Minutes], ["h", "m"], separator);
            }
        }

        var totalDays = (int)Math.Floor(duration.TotalDays);

        if (totalDays < 8)
        {
            var hours = duration.Hours;
            if (compact)
            {
                return GetMeaningfulValues([totalDays, hours], ["d", "h"], separator);
            }
            else
            {
                return GetMeaningfulValues([totalDays, hours, duration.Minutes], ["d", "h", "m"], separator);
            }
        }

        var totalYears = (int)Math.Floor(duration.TotalDays / 365.25);

        if (totalYears < 2)
        {
            if (compact)
            {
                return $"{totalDays}d";
            }
            else
            {
                return GetMeaningfulValues([totalDays, duration.Hours, duration.Minutes], ["d", "h", "m"], separator);
            }
        }
        else if (totalYears < 8)
        {
            var days = totalDays - (int)(totalYears * 365.25);
            if (compact)
            {
                return GetMeaningfulValues([totalYears, days], ["y", "d"], separator);
            }
        }

        if (compact)
        {
            return $"{totalYears}y";
        }

        var remainingDays = totalDays - (int)(totalYears * 365.25);
        var remainingHours = duration.Hours;
        var remainingMinutes = duration.Minutes;
        return GetMeaningfulValues([totalYears, remainingDays, remainingHours, remainingMinutes], ["y", "d", "h", "m"], separator);
    }

    private static string GetMeaningfulValues(int[] values, string[] suffixes, string separator = " ")
    {
        var parts = new List<string>();
        for (int i = 0; i < values.Length && i < suffixes.Length; i++)
        {
            if (values[i] > 0)
            {
                parts.Add($"{values[i]}{suffixes[i]}");
            }
        }
        return string.Join(separator, parts);
    }
}
