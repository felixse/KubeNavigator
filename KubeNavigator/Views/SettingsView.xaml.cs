using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KubeNavigator.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsViewModel? ViewModel { get; set; }
    public SettingsView()
    {
        this.InitializeComponent();
    }
}
