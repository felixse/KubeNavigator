using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
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
                        Items = [.. Resource.Metadata.Annotations?.Select(a => $"{a.Key}={a.Value}") ?? []]
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
            return age.Humanize(minUnit: TimeUnit.Second, maxUnit: TimeUnit.Day, precision: 2); // todo write this myself to shorten output?
        }

        return string.Empty;
    }

    partial void OnResourceChanged(IKubernetesObject<V1ObjectMeta> value)
    {
        UpdateDetails();
    }
}
