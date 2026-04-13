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
            .Items.OfType<DetailsEditorItem>()
            .ToDictionary(
                item => item.Title,
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
            new DetailsSection { Items = [.. GetConfigMapItems()] },
            new DetailsSection
            {
                Header = "Data",
                Items = [.. GetDataItems()],
                SaveCommand = SaveCommand,
            },
            events,
        ];
    }

    private IEnumerable<IDetailsItem> GetConfigMapItems()
    {
        yield return new DetailsTextItem
        {
            Title = "Created",
            Value = Secret.CreationTimestamp().ToString(),
        };

        yield return new DetailsTextItem { Title = "Name", Value = Secret.Name() };

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
                .. Secret.Metadata.Labels?.Select(l => new DetailsCollectionItemElement
                {
                    Value = $"{l.Key}={l.Value}",
                }) ?? [],
            ],
        };

        if (Secret.Metadata.Annotations?.Count > 0)
        {
            yield return new DetailsCollectionItem
            {
                Title = "Annotations",
                Items =
                [
                    .. Secret.Metadata.Annotations?.Select(l => new DetailsCollectionItemElement
                    {
                        Value = $"{l.Key}={l.Value}",
                    }) ?? [],
                ],
            };
        }

        yield return new DetailsTextItem { Title = "Type", Value = Secret.Type };
    }

    private IEnumerable<IDetailsItem> GetDataItems()
    {
        if (Secret.Data != null)
        {
            foreach (var (key, value) in Secret.Data)
            {
                yield return new DetailsEditorItem(key, Encoding.UTF8.GetString(value));
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 12101, Level = LogLevel.Error, Message = "Data section is null when saving Secret {ResourceName}")]
        public static partial void DataSectionNullOnSave(ILogger logger, string resourceName);
    }
}
