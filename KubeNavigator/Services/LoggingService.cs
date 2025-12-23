using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using SerilogILogger = Serilog.ILogger;

namespace KubeNavigator.Services;

public class LoggingService : IDisposable
{
    private readonly InMemoryLogSink _inMemorySink;
    private readonly SerilogILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public event EventHandler<LogEvent>? LogReceived;

    public LoggingService()
    {
        _inMemorySink = new InMemoryLogSink(maxLogCount: 1000);
        _inMemorySink.LogReceived += OnLogReceived;

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KubeNavigator",
            "Logs"
        );

        Directory.CreateDirectory(logDirectory);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.Sink(_inMemorySink)
            .CreateLogger();

        Log.Logger = _logger;
        
        _loggerFactory = new SerilogLoggerFactory(_logger);
    }

    private void OnLogReceived(object? sender, LogEvent e)
    {
        LogReceived?.Invoke(this, e);
    }

    public SerilogILogger Logger => _logger;
    
    public ILoggerFactory LoggerFactory => _loggerFactory;

    public IReadOnlyList<LogEvent> GetLogs() => _inMemorySink.GetLogs();

    public void ClearLogs() => _inMemorySink.Clear();

    public void Dispose()
    {
        _inMemorySink.LogReceived -= OnLogReceived;
        _loggerFactory.Dispose();
        Log.CloseAndFlush();
    }
}
