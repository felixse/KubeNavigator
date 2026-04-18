using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KubeNavigator.Services;

public partial class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KubeNavigator"
    );

    private static readonly string SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");

    public AppSettings Settings { get; private set; } = new();

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                Log.LoadingSettings(_logger, SettingsFilePath);
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                Settings =
                    JsonSerializer.Deserialize(json, SerializerContext.Default.AppSettings)
                    ?? new AppSettings();
                Log.SettingsLoaded(_logger, SettingsFilePath);
            }
            else
            {
                Log.SettingsFileNotFound(_logger, SettingsFilePath);
                Settings = new AppSettings();
                await SaveAsync();
            }

            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }
        catch (Exception ex)
        {
            Log.LoadSettingsFailed(_logger, SettingsFilePath, ex);
            Settings = new AppSettings();
            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }
    }

    private async void OnSettingsPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        Log.SettingsPropertyChanged(_logger, e.PropertyName ?? "unknown");
        await SaveAsync();
        SettingsChanged?.Invoke(this, Settings);
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(Settings, SerializerContext.Default.AppSettings);
            await File.WriteAllTextAsync(SettingsFilePath, json);
            Log.SettingsSaved(_logger, SettingsFilePath);
        }
        catch (Exception ex)
        {
            Log.SaveSettingsFailed(_logger, SettingsFilePath, ex);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 7001,
            Level = LogLevel.Information,
            Message = "Loading settings from {FilePath}"
        )]
        public static partial void LoadingSettings(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 7002,
            Level = LogLevel.Information,
            Message = "Settings loaded from {FilePath}"
        )]
        public static partial void SettingsLoaded(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 7003,
            Level = LogLevel.Information,
            Message = "Settings file not found at {FilePath}, creating defaults"
        )]
        public static partial void SettingsFileNotFound(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 7004,
            Level = LogLevel.Error,
            Message = "Failed to load settings from {FilePath}"
        )]
        public static partial void LoadSettingsFailed(
            ILogger logger,
            string filePath,
            Exception exception
        );

        [LoggerMessage(
            EventId = 7005,
            Level = LogLevel.Debug,
            Message = "Settings property changed: {PropertyName}"
        )]
        public static partial void SettingsPropertyChanged(ILogger logger, string propertyName);

        [LoggerMessage(
            EventId = 7006,
            Level = LogLevel.Debug,
            Message = "Settings saved to {FilePath}"
        )]
        public static partial void SettingsSaved(ILogger logger, string filePath);

        [LoggerMessage(
            EventId = 7007,
            Level = LogLevel.Error,
            Message = "Failed to save settings to {FilePath}"
        )]
        public static partial void SaveSettingsFailed(
            ILogger logger,
            string filePath,
            Exception exception
        );
    }
}
