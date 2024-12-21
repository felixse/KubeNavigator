using CommunityToolkit.Mvvm.ComponentModel;
using KubeNavigator.Model;
using KubeNavigator.ViewModels.Resources;
using KubeNavigator.ViewModels.Shelf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels;

public partial class DetailWindowViewModel : ObservableObject, IShelfHost, IWindow
{
    public AppViewModel App { get; }

    public DetailsViewModel Details { get; }

    public IUserConfirmationService UserConfirmationService { get; }

    public IShelfHost ShelfHost => this;

    [ObservableProperty]
    public partial string Title { get; set; }

    public ObservableCollection<IShelfItem> ShelfItems { get; } = [];

    [ObservableProperty]
    public partial IShelfItem? SelectedShelfItem { get; set; }

    public DetailWindowViewModel(KubernetesResourceViewModel resource, IUserConfirmationService userConfirmationService)
    {
        App = resource.Cluster.App;
        Details = new DetailsViewModel(resource, this);

        Details.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DetailsViewModel.SelectedResource))
            {
                Title = Details.SelectedResource.Name;
            }
        };
        Title = Details.SelectedResource.Name;
        UserConfirmationService = userConfirmationService;
    }

    public async Task CloseShelfItemAsync(IShelfItem item)
    {
        if (ShelfItems.Remove(item))
        {
            await item.OnCloseAsync();
        }
    }

    public void OpenShelfItem(IShelfItem item)
    {
        var existing = ShelfItems.FirstOrDefault(t => t.Resource == item.Resource && item.GetType() == t.GetType());
        if (existing != null)
        {
            SelectedShelfItem = existing;
            return;
        }

        ShelfItems.Add(item);
        SelectedShelfItem = item;
    }
}
