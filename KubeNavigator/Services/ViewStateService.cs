using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.Services;

public partial class ViewStateService
{
    private readonly ILogger<ViewStateService> _logger;

    private static readonly string ViewStateFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KubeNavigator"
    );

    private static readonly string ViewStateFilePath = Path.Combine(
        ViewStateFolder,
        "viewstate.json"
    );

    public ViewState State { get; private set; } = new();

    public ViewStateService(ILogger<ViewStateService> logger)
    {
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(ViewStateFilePath))
            {
                Log.LoadingViewState(_logger, ViewStateFilePath);
                var json = await File.ReadAllTextAsync(ViewStateFilePath);
                State =
                    JsonSerializer.Deserialize(json, SerializerContext.Default.ViewState)
                    ?? new ViewState();
                Log.ViewStateLoaded(_logger, ViewStateFilePath);
            }
            else
            {
                Log.ViewStateFileNotFound(_logger, ViewStateFilePath);
                State = new ViewState();
            }
        }
        catch (Exception ex)
        {
            Log.LoadViewStateFailed(_logger, ViewStateFilePath, ex);
            State = new ViewState();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(ViewStateFolder);
            var json = JsonSerializer.Serialize(State, SerializerContext.Default.ViewState);
            await File.WriteAllTextAsync(ViewStateFilePath, json);
            Log.ViewStateSaved(_logger, ViewStateFilePath);
        }
        catch (Exception ex)
        {
            Log.SaveViewStateFailed(_logger, ViewStateFilePath, ex);
        }
    }

    public async Task ResetAsync()
    {
        State = new ViewState();
        await SaveAsync();
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 8001,
            Level = LogLevel.Information,
            Message = "Loading view state from {FilePath}"
        )]
        public static partial void LoadingViewState(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 8002,
            Level = LogLevel.Information,
            Message = "View state loaded from {FilePath}"
        )]
        public static partial void ViewStateLoaded(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 8003,
            Level = LogLevel.Information,
            Message = "View state file not found at {FilePath}, using defaults"
        )]
        public static partial void ViewStateFileNotFound(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 8004,
            Level = LogLevel.Error,
            Message = "Failed to load view state from {FilePath}"
        )]
        public static partial void LoadViewStateFailed(
            ILogger logger,
            string filePath,
            Exception exception
        );

        [LoggerMessage(
            EventId = 8005,
            Level = LogLevel.Debug,
            Message = "View state saved to {FilePath}"
        )]
        public static partial void ViewStateSaved(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 8006,
            Level = LogLevel.Error,
            Message = "Failed to save view state to {FilePath}"
        )]
        public static partial void SaveViewStateFailed(
            ILogger logger,
            string filePath,
            Exception exception
        );
    }
}
