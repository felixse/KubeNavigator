using System;
using System.Threading.Tasks;

namespace KubeNavigator.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    
    event EventHandler<AppSettings>? SettingsChanged;
    
    Task LoadAsync();
    Task SaveAsync();
}
