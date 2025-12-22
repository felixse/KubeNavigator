using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KubeNavigator.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KubeNavigator");
    
    private static readonly string SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            else
            {
                Settings = new AppSettings();
                await SaveAsync();
            }
            
            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }
        catch (Exception e)
        {
            Settings = new AppSettings();
            Settings.PropertyChanged += OnSettingsPropertyChanged;
        }
    }

    private async void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        await SaveAsync();
        SettingsChanged?.Invoke(this, Settings);
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception)
        {
        }
    }
}
