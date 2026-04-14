using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.ViewModels.Resources;

internal partial class SecretViewModel : KubernetesResourceViewModel
{
    private readonly ILogger<SecretViewModel> _logger;

    public SecretViewModel(IKubernetesObject<V1ObjectMeta> resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Secret, cluster)
    {
        _logger = cluster.App.LoggingService.LoggerFactory.CreateLogger<SecretViewModel>();
    }

    public V1Secret Secret => (V1Secret)Resource;

    public static readonly ImmutableArray<ResourceColumn> SecretColumns =
    [
        new("Name", vm => vm.Name, PropertyName: nameof(Name)),
        new("Namespace", vm => vm.Namespace, PropertyName: nameof(Namespace)),
        new("Keys", vm => ((SecretViewModel)vm).Keys, PropertyName: nameof(Keys)),
        new("Type", vm => ((SecretViewModel)vm).Type, PropertyName: nameof(Type)),
        new("Age", vm => vm.Age, ResourceColumnType.Age, nameof(Age)),
    ];

    public override ImmutableArray<ResourceColumn> Columns => SecretColumns;

    public string Keys => string.Join(", ", Secret.Data?.Keys ?? []);

    public string Type => Secret.Type;

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
            .Select(r => r.Content)
            .OfType<EditorContent>()
            .ToDictionary(
                item => item.Title!,
                item => Encoding.UTF8.GetBytes(item.TextRetriever?.Invoke() ?? item.Value)
            );
        Secret.Data = newData;

        await Cluster.Context.SaveSecretAsync(Secret);
        Cluster.App.WindowManager.ActiveWindow.ShowMessage(
            "Success",
            "Secret saved successfully.",
            NotificationSeverity.Success
        );
    }

    public override async Task<List<IDetailsSection>> CreateDetailsAsync()
    {
        var events = await GetEventsSectionAsync();
        return
        [
            new DetailsSection { Rows = [.. GetSecretRows()] },
            new DetailsSection
            {
                Header = "Data",
                Rows = [.. GetDataRows()],
                SaveCommand = SaveCommand,
            },
            events,
        ];
    }

    private IEnumerable<IDetailsRow> GetSecretRows()
    {
        yield return new HeaderedRow { Header = "Created", Content = new TextContent { Value = Secret.CreationTimestamp().ToString() } };

        yield return new HeaderedRow { Header = "Name", Content = new TextContent { Value = Secret.Name() } };

        yield return new HeaderedRow { Header = "Namespace", Content = new LinkContent { ResourceName = Resource.Namespace(), ResourceType = ResourceType.Namespace } };

        yield return new HeaderedRow
        {
            Header = "Labels",
            Content = new CollectionContent
            {
                Items =
                [
                    .. Secret.Metadata.Labels?.Select(l => new TextCollectionElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            },
        };

        if (Secret.Metadata.Annotations?.Count > 0)
        {
            yield return new HeaderedRow
            {
                Header = "Annotations",
                Content = new CollectionContent
                {
                    Items =
                    [
                        .. Secret.Metadata.Annotations?.Select(l => new TextCollectionElement
                        {
                            Value = $"{l.Key}={l.Value}",
                        }) ?? [],
                    ],
                },
            };
        }

        yield return new HeaderedRow { Header = "Type", Content = new TextContent { Value = Secret.Type } };
    }

    private IEnumerable<IDetailsRow> GetDataRows()
    {
        if (Secret.Data != null)
        {
            foreach (var (key, value) in Secret.Data)
            {
                yield return new FullWidthRow
                {
                    Content = new EditorContent { Title = key, Value = Encoding.UTF8.GetString(value) },
                };
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 12101, Level = LogLevel.Error, Message = "Data section is null when saving Secret {ResourceName}")]
        public static partial void DataSectionNullOnSave(ILogger logger, string resourceName);
    }
}
