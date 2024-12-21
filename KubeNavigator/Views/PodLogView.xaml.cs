using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;
using System;

namespace KubeNavigator.Views;

public sealed partial class PodLogView : UserControl, IShelfItemView
{
    public PodLogsViewModel ViewModel { get; }

    public PodLogView(PodLogsViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
        ViewModel.LineReceived += ViewModel_LineReceived;
        ViewModel.Closed += ViewModel_Closed;
        Terminal.OnInitialized += Terminal_OnInitialized;
    }

    private void Terminal_OnInitialized(object? sender, EventArgs e)
    {
        ViewModel.Start();
    }

    private void ViewModel_Closed(object? sender, EventArgs e)
    {
        Terminal.Close();
        ViewModel.LineReceived -= ViewModel_LineReceived;
        ViewModel.Closed -= ViewModel_Closed;
        Terminal.OnInitialized -= Terminal_OnInitialized;
    }

    private void ViewModel_LineReceived(object? sender, string e)
    {
        Terminal.Write(e + Environment.NewLine);
    }

    private void OnClearButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Terminal.Clear();
    }
}
