using System.Text.Json;
using Avalonia.Styling;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Core.Serialization;
using SukiUI;

namespace OpenGameVerse.App.Services;

public interface IAppSettingsService
{
    AppSettings CurrentSettings { get; }
    event Action<AppSettings>? SettingsChanged;
    void Load();
    void ApplyThemeFromSettings();
    Task SetThemeAsync(bool isDarkTheme);
}

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;

    public AppSettings CurrentSettings { get; private set; } = new();

    public event Action<AppSettings>? SettingsChanged;

    public AppSettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return;
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize(
                json,
                OpenGameVerseJsonContext.Default.AppSettings);

            if (settings != null)
            {
                CurrentSettings = settings;
            }
        }
        catch
        {
            CurrentSettings = new AppSettings();
        }
    }

    public async Task SetThemeAsync(bool isDarkTheme)
    {
        CurrentSettings.IsDarkTheme = isDarkTheme;
        ApplyTheme(isDarkTheme);
        await SaveAsync();
        SettingsChanged?.Invoke(CurrentSettings);
    }

    public void ApplyThemeFromSettings()
    {
        ApplyTheme(CurrentSettings.IsDarkTheme);
    }

    private async Task SaveAsync()
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(
            CurrentSettings,
            OpenGameVerseJsonContext.Default.AppSettings);

        await File.WriteAllTextAsync(_settingsPath, json);
    }

    private static void ApplyTheme(bool isDarkTheme)
    {
        var theme = isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        SukiTheme.GetInstance().ChangeBaseTheme(theme);
    }
}
