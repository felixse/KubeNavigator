using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KubeNavigator.Services;
using KubeNavigator.ViewModels.Resources;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KubeNavigator.ViewModels.Shelf;

public partial class ApplicationLogViewModel : ObservableObject, IShelfItem
{
    private readonly LoggingService _loggingService;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private LogEventLevel _minimumLevel = LogEventLevel.Verbose;

    public ThemeManager ThemeManager { get; }

    public event EventHandler<string>? LogReceived;

    public ApplicationLogViewModel(LoggingService loggingService, ThemeManager themeManager)
    {
        _loggingService = loggingService;
        ThemeManager = themeManager;
        _loggingService.LogReceived += OnLogReceived;
    }

    public void LoadExistingLogs()
    {
        foreach (var logEvent in _loggingService.GetLogs())
        {
            var formattedLog = FormatLogEvent(logEvent);
            LogReceived?.Invoke(this, formattedLog);
        }
    }

    private void OnLogReceived(object? sender, LogEvent e)
    {
        var formattedLog = FormatLogEvent(e);
        LogReceived?.Invoke(this, formattedLog);
    }

    private string FormatLogEvent(LogEvent logEvent)
    {
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => "[VRB]",
            LogEventLevel.Debug => "[DBG]",
            LogEventLevel.Information => "[INF]",
            LogEventLevel.Warning => "[WRN]",
            LogEventLevel.Error => "[ERR]",
            LogEventLevel.Fatal => "[FTL]",
            _ => "[???]"
        };
        var message = logEvent.RenderMessage();
        var exception = logEvent.Exception != null ? $"\n{logEvent.Exception}" : string.Empty;
        
        var levelColor = logEvent.Level switch
        {
            LogEventLevel.Verbose => "\x1b[90m",
            LogEventLevel.Debug => "\x1b[36m",
            LogEventLevel.Information => "\x1b[32m",
            LogEventLevel.Warning => "\x1b[33m",
            LogEventLevel.Error => "\x1b[31m",
            LogEventLevel.Fatal => "\x1b[35m",
            _ => "\x1b[0m"
        };
        
        return $"\x1b[90m{timestamp}\x1b[0m {levelColor}{level}\x1b[0m {message}{exception}\r\n";
    }

    [RelayCommand]
    private void Clear()
    {
        _loggingService.ClearLogs();
    }

    public string Title => "Application Logs";

    public KubernetesResourceViewModel? Resource => null;

    public event EventHandler? Closed;

    public Task OnCloseAsync()
    {
        _loggingService.LogReceived -= OnLogReceived;
        Closed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
