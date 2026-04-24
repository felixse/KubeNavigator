using System;
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

    private async void ResetViewState_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset View State",
            Content = "This will reset all pinned items, expanded groups, and saved namespace filters. This cannot be undone.",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel?.ResetViewStateCommand.Execute(null);
        }
    }
}
