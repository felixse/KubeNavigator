using System;
using System.Collections.Generic;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

internal partial class EventViewModel : KubernetesResourceViewModel
{
    public EventViewModel(IKubernetesObject<V1ObjectMeta> resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Event, cluster)
    {
        LastSeen = FormatDuration(Event.DeprecatedLastTimestamp);
    }

    public Eventsv1Event Event => (Eventsv1Event)Resource;

    public string Type => Event.Type;

    public string Message => Event.Note;

    public string InvolvedObject => $"{Event.Regarding?.Kind}: {Event.Regarding?.Name}";

    public string? Source => Event.DeprecatedSource?.Component;

    public int Count => Event.DeprecatedCount ?? 0;

    [ObservableProperty]
    public partial string LastSeen { get; private set; }

    public override void RefreshTimestamps()
    {
        base.RefreshTimestamps();
        LastSeen = FormatDuration(Event.DeprecatedLastTimestamp);
    }

    public override List<IDetailsSection> CreateDetails()
    {
        return
        [
            new DetailsSection { Items = [.. GetEventItems()] },
            new DetailsSection
            {
                Header = "Involved object",
                Items = [.. GetInvolvedObjectItems()],
            },
        ];
    }

    private IEnumerable<IDetailsItem> GetEventItems()
    {
        yield return new DetailsTextItem
        {
            Title = "Created",
            Value = Event.CreationTimestamp().ToString(),
        };

        yield return new DetailsTextItem { Title = "Name", Value = Event.Name() };

        yield return new DetailsLinkItem
        {
            Title = "Namespace",
            ResourceName = Resource.Namespace(),
            ResourceType = ResourceType.Namespace,
        };

        yield return new DetailsTextItem { Title = "Message", Value = Event.Note };

        yield return new DetailsTextItem { Title = "Reason", Value = Event.Reason };

        yield return new DetailsTextItem
        {
            Title = "Source",
            Value = Event.DeprecatedSource?.Component,
        };

        yield return new DetailsTextItem
        {
            Title = "First seen",
            Value = Event.DeprecatedFirstTimestamp.ToString(),
        };

        yield return new DetailsTextItem
        {
            Title = "Last seen",
            Value = Event.DeprecatedLastTimestamp.ToString(),
        };

        yield return new DetailsTextItem
        {
            Title = "Count",
            Value = Event.DeprecatedCount?.ToString(),
        };

        yield return new DetailsTextItem { Title = "Type", Value = Event.Type };
    }

    private IEnumerable<IDetailsItem> GetInvolvedObjectItems()
    {
        if (Event.Regarding == null)
        {
            yield break;
        }

        if (ResourceType.FromKind(Event.Regarding.Kind) is ResourceType resourceType)
        {
            yield return new DetailsLinkItem
            {
                Title = "Name",
                ResourceName = Event.Regarding?.Name,
                ResourceType = resourceType,
            };
        }
        else
        {
            yield return new DetailsTextItem { Title = "Name", Value = Event.Regarding?.Name };
        }

        yield return new DetailsTextItem { Title = "Namespace", Value = Event.Namespace() };

        yield return new DetailsTextItem { Title = "Kind", Value = Event.Regarding.Kind };

        yield return new DetailsTextItem
        {
            Title = "Field Path",
            Value = Event.Regarding.FieldPath,
        };
    }
}
