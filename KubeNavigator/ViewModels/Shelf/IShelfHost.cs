using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Shelf;

public interface IShelfHost
{
    AppViewModel App { get; }

    void OpenShelfItem(IShelfItem item);

    Task CloseShelfItemAsync(IShelfItem item);
}
