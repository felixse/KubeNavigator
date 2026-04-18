using KubeNavigator.ViewModels;
using Microsoft.UI.Xaml;
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

    private void SettingsTabs_SelectionChanged(
        SelectorBar sender,
        SelectorBarSelectionChangedEventArgs args
    )
    {
        var isGeneral = sender.SelectedItem == GeneralTab;
        GeneralPanel.Visibility = isGeneral ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility = isGeneral ? Visibility.Collapsed : Visibility.Visible;
    }
}
