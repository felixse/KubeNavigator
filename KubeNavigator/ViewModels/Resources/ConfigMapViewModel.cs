using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Resources;

internal partial class ConfigMapViewModel : KubernetesResourceViewModel
{
    private readonly ILogger<ConfigMapViewModel> _logger;

    public ConfigMapViewModel(IKubernetesObject<V1ObjectMeta> resource, ClusterViewModel cluster)
        : base(resource, ResourceType.ConfigMap, cluster)
    {
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<ConfigMapViewModel>();
    }

    public V1ConfigMap ConfigMap => (V1ConfigMap)Resource;

    public static readonly ImmutableArray<ResourceColumn> ConfigMapColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Keys", vm => ((ConfigMapViewModel)vm).Keys, PropertyName: nameof(Keys)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => ConfigMapColumns;

    public string Keys => string.Join(", ", ConfigMap.Data?.Keys ?? []);

    [RelayCommand]
    public async Task SaveAsync(DetailsSection? dataSection)
    {
        if (dataSection == null)
        {
            Log.DataSectionNullOnSave(_logger, Name);
            return;
        }

        var newData = dataSection
            .Rows.OfType<FullWidthRow>()
            .ToDictionary(
                item => item.Header!,
                item =>
                {
                    var editor = item.Content as EditorContent;
                    return editor == null ? null : editor.TextRetriever?.Invoke() ?? editor.Value;
                }
            );
        ConfigMap.Data = newData;

        await Cluster.Context.SaveConfigMapAsync(ConfigMap);
        Cluster.App.WindowManager.ActiveWindow.ShowMessage(
            "Success",
            "ConfigMap saved successfully.",
            NotificationSeverity.Success
        );
    }

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        var sections = new List<IDetailsSection>
        {
            new DetailsSection { Rows = [.. GetConfigMapRows()] },
            new DetailsSection
            {
                Header = "Data",
                Rows = [.. GetDataRows()],
                SaveCommand = SaveCommand,
            },
        };

        if (events is not null)
        {
            sections.Add(events);
        }

        return sections;
    }

    private IEnumerable<IDetailsRow> GetConfigMapRows()
    {
        yield return new HeaderedRow
        {
            Header = "Created",
            Content = new TextContent { Value = ConfigMap.CreationTimestamp().ToString() },
        };
        yield return new HeaderedRow
        {
            Header = "Name",
            Content = new TextContent { Value = ConfigMap.Name() },
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
    }

    private IEnumerable<IDetailsRow> GetDataRows()
    {
        if (ConfigMap.Data != null)
        {
            foreach (var (key, value) in ConfigMap.Data)
            {
                yield return new FullWidthRow
                {
                    Header = key,
                    Content = new EditorContent { Value = value },
                };
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 12001,
            Level = LogLevel.Error,
            Message = "Data section is null when saving ConfigMap {ResourceName}"
        )]
        public static partial void DataSectionNullOnSave(ILogger logger, string resourceName);
    }
}
