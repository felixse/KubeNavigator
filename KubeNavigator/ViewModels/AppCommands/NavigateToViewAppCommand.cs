using System.Threading.Tasks;
using KubeNavigator.ViewModels.Navigation;

namespace KubeNavigator.ViewModels.AppCommands
{
    public class NavigateToViewAppCommand : IAppCommand
    {
        private readonly INavigationTarget _target;
        private readonly WorkspaceViewModel _workspace;

        public string Name { get; }

        public string Context { get; }

        public NavigateToViewAppCommand(INavigationTarget target, WorkspaceViewModel workspace)
        {
            _target = target;
            _workspace = workspace;

            Name = $"Open {target.Title}";
            Context = "App";
        }

        public Task ExecuteAsync()
        {
            _workspace.SelectedItem = _target;
            return Task.CompletedTask;
        }
    }
}
