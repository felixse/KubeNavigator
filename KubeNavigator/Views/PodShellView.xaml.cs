using KubeNavigator.Model;
using KubeNavigator.Model.TerminalMessages;
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
        Terminal.Loaded += Terminal_Loaded;
        Terminal.Unloaded += Terminal_Unloaded;
    }

    private void Terminal_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ThemeManager.RegisterTerminal(Terminal);
    }

    private void Terminal_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ThemeManager.UnregisterTerminal(Terminal);
    }

    private void Terminal_OnTextReceived(object? sender, string e)
    {
        ViewModel.Write(e);
    }

    private async void Terminal_OnInitialized(object? sender, EventArgs e)
    {
        var initMessage = new InitializeTerminal
        {
            Theme = ViewModel.ThemeManager.GetEffectiveTheme().ToString().ToLowerInvariant(),
            ReadOnly = false
        };
        Terminal.SendMessage(initMessage);
        
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
        Terminal.Loaded -= Terminal_Loaded;
        Terminal.Unloaded -= Terminal_Unloaded;
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
