using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Collections;
using KubeNavigator.ViewModels.Filters;

namespace KubeNavigator.ViewModels;

public partial class PortForwardsViewModel : ListViewModel
{
    public ObservableCollection<ForwardedPortViewModel> ForwardedPorts { get; } = [];

    public PortForwardsViewModel(
        WorkspaceViewModel workspace,
        ObservableCollection<ForwardedPortViewModel> forwardedPorts
    )
        : base(
            workspace,
            title: "Port Forwards",
            isNamespaceScoped: false,
            namespaceFilters: [],
            additionalFilters: []
        )
    {
        ForwardedPorts = forwardedPorts;
        Items = new AdvancedCollectionView(ForwardedPorts);
    }

    protected override Task DeleteItemsAsync(IReadOnlyCollection<ISelectable> items)
    {
        throw new System.NotImplementedException();
    }

    public override Task OnNavigatedTo()
    {
        return Task.CompletedTask;
    }

    public override Task OnNavigatedFrom()
    {
        return Task.CompletedTask;
    }
}
