using KubeNavigator.ViewModels.Resources;
using System;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Shelf;

public interface IShelfItem
{
    KubernetesResourceViewModel Resource { get; }

    event EventHandler Closed;

    Task OnCloseAsync();
}
