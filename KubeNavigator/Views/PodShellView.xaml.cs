using KubeNavigator.Model;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;
using System;

namespace KubeNavigator.Views;
public sealed partial class PodShellView : UserControl, IShelfItemView
{
    public PodShellView(PodShellViewModel viewModel)
    {
        this.InitializeComponent();
        ViewModel = viewModel;
        ViewModel.TextReceived += ViewModel_TextReceived;
        ViewModel.Closed += ViewModel_Closed;
        Terminal.OnInitialized += Terminal_OnInitialized;
        Terminal.OnTextReceived += Terminal_OnTextReceived;
        Terminal.OnSizeChanged += Terminal_OnSizeChanged;
    }

    private void Terminal_OnTextReceived(object? sender, string e)
    {
        ViewModel.Write(e);
    }

    private async void Terminal_OnInitialized(object? sender, EventArgs e)
    {
        await ViewModel.StartAsync();
    }

    private async void Terminal_OnSizeChanged(object? sender, TerminalSize e)
    {
        await ViewModel.ResizeAsync(e);
    }

    private void ViewModel_Closed(object? sender, EventArgs e)
    {
        Terminal.Close();
        ViewModel.TextReceived -= ViewModel_TextReceived;
        ViewModel.Closed -= ViewModel_Closed;
        Terminal.OnInitialized -= Terminal_OnInitialized;
        Terminal.OnTextReceived -= Terminal_OnTextReceived;
        Terminal.OnSizeChanged -= Terminal_OnSizeChanged;
    }

    private void ViewModel_TextReceived(object? sender, string e)
    {
        Terminal.Write(e);
    }

    public PodShellViewModel ViewModel { get; }

    private void ClearButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Terminal.Clear();
    }
}
