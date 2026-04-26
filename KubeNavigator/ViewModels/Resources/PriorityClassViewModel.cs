using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

public partial class PriorityClassViewModel : KubernetesResourceViewModel
{
    public PriorityClassViewModel(V1PriorityClass resource, ClusterViewModel cluster)
        : base(resource, ResourceType.PriorityClass, cluster) { }

    public V1PriorityClass PriorityClass => (V1PriorityClass)Resource;

    public static readonly ImmutableArray<ResourceColumn> PriorityClassColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new(
            "Value",
            vm => ((PriorityClassViewModel)vm).PriorityValue,
            PropertyName: nameof(PriorityValue)
        ),
        new(
            "Global Default",
            vm => ((PriorityClassViewModel)vm).GlobalDefault,
            PropertyName: nameof(GlobalDefault)
        ),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => PriorityClassColumns;

    public string PriorityValue => PriorityClass.Value.ToString();

    public string GlobalDefault => PriorityClass.GlobalDefault == true ? "Yes" : "No";

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetInfoRows()] },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetInfoRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = PriorityClass.CreationTimestamp().ToString() },
        };

        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = PriorityClass.Name() },
        };

        yield return new HeaderedRow
        {
            Header = "Description",
            Content = new TextContent { Value = PriorityClass.Description ?? "-" },
        };

        yield return new HeaderedRow
        {
            Header = "Value",
            Content = new TextContent { Value = PriorityClass.Value.ToString() },
        };

        yield return new HeaderedRow
        {
            Header = "Global Default",
            Content = new TextContent { Value = PriorityClass.GlobalDefault == true ? "Yes" : "No" },
        };
    }
}
