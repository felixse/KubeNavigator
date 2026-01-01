using System;
using System.Threading.Tasks;
using KubeNavigator.ViewModels.Resources;

namespace KubeNavigator.ViewModels.Shelf;

public interface IShelfItem
{
    KubernetesResourceViewModel? Resource { get; }

    event EventHandler Closed;

    Task OnCloseAsync();
}
