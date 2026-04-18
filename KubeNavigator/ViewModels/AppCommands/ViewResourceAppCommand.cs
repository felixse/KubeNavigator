using System.Linq;
using System.Threading.Tasks;
using KubeNavigator.Models;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels.AppCommands
{
    public class ViewResourceAppCommand : IAppCommand
    {
        private readonly WorkspaceViewModel _workspace;

        public ResourceType ResourceType { get; }

        public string Name { get; }

        public string Context { get; }

        public ViewResourceAppCommand(
            ResourceType resourceType,
            WorkspaceViewModel workspace,
            string? groupName = null
        )
        {
            ResourceType = resourceType;
            _workspace = workspace;

            Name =
                groupName != null
                    ? $"View {ResourceType.PluralDisplayName} [{groupName}]"
                    : $"View {ResourceType.PluralDisplayName}";
            Context = _workspace.Cluster.Name;
        }

        public Task ExecuteAsync()
        {
            var target = _workspace
                .NavigationGroups.SelectMany(x => x.Items)
                .SelectMany(item =>
                    item is CustomResourceGroupViewModel crg
                        ? crg.Resources.Cast<INavigationTarget>()
                        : [item]
                )
                .FirstOrDefault(x =>
                    x is KubernetesResourceTypeListViewModel resourceTypeList
                    && resourceTypeList.ResourceType == ResourceType
                );
            if (target != null)
            {
                _workspace.SelectedItem = target;
            }

            return Task.CompletedTask;
        }
    }
}
