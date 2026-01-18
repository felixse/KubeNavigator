using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.AppCommands
{
    public interface IAppCommand
    {
        string Name { get; }
        string Context { get; }
        Task ExecuteAsync();
    }
}
