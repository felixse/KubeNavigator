using KubeNavigator.Model.TerminalMessages;
using KubeNavigator.ViewModels.Shelf;
using Microsoft.UI.Xaml.Controls;
using System;

namespace KubeNavigator.Views;

public sealed partial class ApplicationLogView : UserControl, IShelfItemView
{
    public ApplicationLogViewModel ViewModel { get; }

    public ApplicationLogView(ApplicationLogViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        ViewModel.Closed += ViewModel_Closed;
        ViewModel.LogReceived += ViewModel_LogReceived;
        Terminal.OnInitialized += Terminal_OnInitialized;
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

    private void Terminal_OnInitialized(object? sender, EventArgs e)
    {
        var initMessage = new InitializeTerminal
        {
            Theme = ViewModel.ThemeManager.GetEffectiveTheme().ToString().ToLowerInvariant(),
            ReadOnly = true
        };
        Terminal.SendMessage(initMessage);
        
        ViewModel.LoadExistingLogs();
    }

    private void ViewModel_LogReceived(object? sender, string e)
    {
        Terminal.Write(e);
    }

    private void ViewModel_Closed(object? sender, EventArgs e)
    {
        Terminal.Close();
        ViewModel.Closed -= ViewModel_Closed;
        ViewModel.LogReceived -= ViewModel_LogReceived;
        Terminal.OnInitialized -= Terminal_OnInitialized;
        Terminal.Loaded -= Terminal_Loaded;
        Terminal.Unloaded -= Terminal_Unloaded;
    }

    private void OnClearButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Terminal.Clear();
    }
}
