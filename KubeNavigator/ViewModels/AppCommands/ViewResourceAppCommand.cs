using System.Linq;
using System.Threading.Tasks;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels.AppCommands
{
    public class ViewResourceAppCommand : IAppCommand
    {
        private readonly ResourceType _resourceType;
        private readonly WorkspaceViewModel _workspace;

        public string Name { get; }

        public string Context { get; }

        public ViewResourceAppCommand(ResourceType resourceType, WorkspaceViewModel workspace)
        {
            _resourceType = resourceType;
            _workspace = workspace;

            Name = $"View {_resourceType.PluralDisplayName}";
            Context = _workspace.Cluster.Name;
        }

        public Task ExecuteAsync()
        {
            var target = _workspace
                .NavigationGroups.SelectMany(x => x.Items)
                .SelectMany(item => item is CustomResourceGroupViewModel crg
                    ? crg.Resources.Cast<INavigationTarget>()
                    : [item])
                .FirstOrDefault(x =>
                    x is KubernetesResourceTypeListViewModel resourceTypeList
                    && resourceTypeList.ResourceType == _resourceType
                );
            if (target != null)
            {
                _workspace.SelectedItem = target;
            }

            return Task.CompletedTask;
        }
    }
}
