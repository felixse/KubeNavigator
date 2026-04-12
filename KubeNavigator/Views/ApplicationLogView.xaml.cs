using System;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class ApplicationLogView : UserControl, IShelfItemView
{
    private readonly LogViewHelper _helper;

    public ApplicationLogViewModel ViewModel { get; }

    public ApplicationLogView(ApplicationLogViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        _helper = new LogViewHelper(Terminal, ViewModel.ThemeManager, ViewModel, () => ViewModel.SearchText, ViewModel.LoadExistingLogs);
        ViewModel.LogReceived += ViewModel_LogReceived;
        ViewModel.Closed += ViewModel_Closed;
    }

    private void ViewModel_Closed(object? sender, EventArgs e)
    {
        ViewModel.LogReceived -= ViewModel_LogReceived;
        ViewModel.Closed -= ViewModel_Closed;
        _helper.Close();
    }

    private void ViewModel_LogReceived(object? sender, string e)
    {
        _helper.WriteLog(e);
    }

    private void OnClearButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _helper.Clear();
    }

    private void OnSearchKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        _helper.HandleSearchKeyDown(e);
    }
}
