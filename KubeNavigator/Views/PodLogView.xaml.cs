using System;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;

namespace KubeNavigator.Views;

public sealed partial class PodLogView : UserControl, IShelfItemView
{
    private readonly LogViewHelper _helper;

    public PodLogsViewModel ViewModel { get; }

    public PodLogView(PodLogsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        _helper = new LogViewHelper(Terminal, ViewModel.ThemeManager, ViewModel, () => ViewModel.SearchText, ViewModel.Start);
        ViewModel.LineReceived += ViewModel_LineReceived;
        ViewModel.Closed += ViewModel_Closed;
    }

    private void ViewModel_Closed(object? sender, EventArgs e)
    {
        ViewModel.LineReceived -= ViewModel_LineReceived;
        ViewModel.Closed -= ViewModel_Closed;
        _helper.Close();
    }

    private void ViewModel_LineReceived(object? sender, string e)
    {
        _helper.WriteLog(e + Environment.NewLine);
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
