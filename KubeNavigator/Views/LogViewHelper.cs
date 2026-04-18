using System;
using System.ComponentModel;
using KubeNavigator.Models.TerminalMessages;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace KubeNavigator.Views;

internal sealed class LogViewHelper
{
    private readonly TerminalView _terminal;
    private readonly ThemeManager _themeManager;
    private readonly INotifyPropertyChanged _viewModel;
    private readonly Func<string?> _getSearchText;
    private readonly Action _onTerminalReady;

    public LogViewHelper(
        TerminalView terminal,
        ThemeManager themeManager,
        INotifyPropertyChanged viewModel,
        Func<string?> getSearchText,
        Action onTerminalReady
    )
    {
        _terminal = terminal;
        _themeManager = themeManager;
        _viewModel = viewModel;
        _getSearchText = getSearchText;
        _onTerminalReady = onTerminalReady;

        _terminal.OnInitialized += Terminal_OnInitialized;
        _terminal.Loaded += Terminal_Loaded;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public void WriteLog(string text) => _terminal.Write(text);

    public void Clear() => _terminal.Clear();

    public void HandleSearchKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            if (ctrlState.HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                _terminal.SearchPrevious(_getSearchText());
            }
            else
            {
                _terminal.SearchNext(_getSearchText());
            }

            e.Handled = true;
        }
    }

    public void Close()
    {
        _terminal.Close();
        _terminal.OnInitialized -= Terminal_OnInitialized;
        _terminal.Loaded -= Terminal_Loaded;
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _themeManager.UnregisterTerminal(_terminal);
    }

    private void Terminal_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _themeManager.RegisterTerminal(_terminal);
    }

    private void Terminal_OnInitialized(object? sender, EventArgs e)
    {
        var initMessage = new InitializeTerminal
        {
            Theme = _themeManager.GetEffectiveTheme().ToString().ToLowerInvariant(),
            ReadOnly = true,
        };
        _terminal.SendMessage(initMessage);
        _onTerminalReady();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SearchText")
        {
            _terminal.Search(_getSearchText());
        }
    }
}
