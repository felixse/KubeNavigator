using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

internal partial class EventViewModel : KubernetesResourceViewModel
{
    public EventViewModel(IKubernetesObject<V1ObjectMeta> resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Event, cluster) { }

    public Eventsv1Event Event => (Eventsv1Event)Resource;

    public static readonly ImmutableArray<ResourceColumn> EventColumns =
    [
        new("Type", vm => ((EventViewModel)vm).Type, PropertyName: nameof(Type)),
        new("Message", vm => ((EventViewModel)vm).Message, PropertyName: nameof(Message)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new(
            "Involved Object",
            vm => ((EventViewModel)vm).InvolvedObject,
            PropertyName: nameof(InvolvedObject)
        ),
        new("Source", vm => ((EventViewModel)vm).Source, PropertyName: nameof(Source)),
        new("Count", vm => ((EventViewModel)vm).Count, PropertyName: nameof(Count)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
        new(
            "Last Seen",
            vm => ((EventViewModel)vm).LastSeen,
            ResourceColumnType.Age,
            nameof(LastSeen)
        ),
    ];

    public override ImmutableArray<ResourceColumn> Columns => EventColumns;

    public string Type => Event.Type;

    public string Message => Event.Note;

    public string InvolvedObject => $"{Event.Regarding?.Kind}: {Event.Regarding?.Name}";

    public string? Source => Event.DeprecatedSource?.Component;

    public int Count => Event.DeprecatedCount ?? 0;

    public DateTime? LastSeen => Event.DeprecatedLastTimestamp;

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        return
        [
            new DetailsSection { Rows = [.. GetEventRows()] },
            new DetailsSection { Header = "Involved object", Rows = [.. GetInvolvedObjectRows()] },
        ];
    }

    private IEnumerable<IDetailsRow> GetEventRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = Event.CreationTimestamp().ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = Event.Name() },
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
            Header = "Message",
            Content = new TextContent { Value = Event.Note },
        };
        yield return new HeaderedRow
        {
            Header = "Reason",
            Content = new TextContent { Value = Event.Reason },
        };
        yield return new HeaderedRow
        {
            Header = "Source",
            Content = new TextContent { Value = Event.DeprecatedSource?.Component },
        };
        yield return new HeaderedRow
        {
            Header = "First seen",
            Content = new TextContent { Value = Event.DeprecatedFirstTimestamp.ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Last seen",
            Content = new TextContent { Value = Event.DeprecatedLastTimestamp.ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Count",
            Content = new TextContent { Value = Event.DeprecatedCount?.ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Type",
            Content = new TextContent { Value = Event.Type },
        };
    }

    private IEnumerable<IDetailsRow> GetInvolvedObjectRows()
    {
        if (Event.Regarding == null)
        {
            yield break;
        }

        if (ResourceType.FromKind(Event.Regarding.Kind) is ResourceType resourceType)
        {
            yield return new HeaderedRow
            {
                Header = "Name",
                Content = new LinkContent
                {
                    ResourceName = Event.Regarding?.Name,
                    ResourceType = resourceType,
                },
            };
        }
        else
        {
            yield return new HeaderedRow
            {
                Header = "Name",
                Content = new TextContent { Value = Event.Regarding?.Name },
            };
        }

        yield return new HeaderedRow
        {
            Header = "Namespace",
            Content = new TextContent { Value = Event.Namespace() },
        };
        yield return new HeaderedRow
        {
            Header = "Kind",
            Content = new TextContent { Value = Event.Regarding.Kind },
        };
        yield return new HeaderedRow
        {
            Header = "Field Path",
            Content = new TextContent { Value = Event.Regarding.FieldPath },
        };
    }
}
