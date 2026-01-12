using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using k8s;
using k8s.Models;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Details;

namespace KubeNavigator.ViewModels.Resources;

internal partial class SecretViewModel : KubernetesResourceViewModel
{
    public SecretViewModel(IKubernetesObject<V1ObjectMeta> resource, ClusterViewModel cluster)
        : base(resource, ResourceType.Secret, cluster) { }

    public V1Secret Secret => (V1Secret)Resource;

    public string Keys => string.Join(", ", Secret.Data?.Keys ?? []);

    public string Type => Secret.Type;

    [RelayCommand]
    public async Task SaveAsync()
    {
        var data = Details.Find(x => x is DetailsSection section && section.Header == "Data");

        if (data is DetailsSection dataSection)
        {
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
        else
        {
            // todo log error
        }
    }

    protected override List<IDetailsSection> CreateDetails()
    {
        return
        [
            new DetailsSection { Items = [.. GetConfigMapItems()] },
            new DetailsSection
            {
                Header = "Data",
                Items = [.. GetDataItems()],
                SaveCommand = SaveCommand,
            },
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
}
