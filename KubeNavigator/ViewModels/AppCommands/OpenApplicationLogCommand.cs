using System.Linq;
using System.Threading.Tasks;
using KubeNavigator.ViewModels.Shelf;

namespace KubeNavigator.ViewModels.AppCommands
{
    public class OpenApplicationLogCommand : IAppCommand
    {
        private readonly WorkspaceViewModel _workspace;

        public string Name => "Open Application Logs";

        public string Context => "App";

        public OpenApplicationLogCommand(WorkspaceViewModel workspace)
        {
            _workspace = workspace;
        }

        public Task ExecuteAsync()
        {
            var existing = _workspace.ShelfItems.OfType<ApplicationLogViewModel>().FirstOrDefault();
            if (existing != null)
            {
                _workspace.SelectedShelfItem = existing;
            }
            else
            {
                var log = new ApplicationLogViewModel(
                    _workspace.App.LoggingService,
                    _workspace.App.ThemeManager
                );
                _workspace.ShelfItems.Add(log);
                _workspace.SelectedShelfItem = log;
            }

            return Task.CompletedTask;
        }
    }
}
