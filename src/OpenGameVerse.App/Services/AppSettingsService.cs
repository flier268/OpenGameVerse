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
    Task UpdateAsync(AppSettings settings);
}

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;
    private readonly IStartupService? _startupService;

    public AppSettings CurrentSettings { get; private set; } = new();

    public event Action<AppSettings>? SettingsChanged;

    public AppSettingsService(string settingsPath, IStartupService? startupService = null)
    {
        _settingsPath = settingsPath;
        _startupService = startupService;
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
                return;
            }
        }
        catch
        {
            if (TryLoadLegacySettings(out var legacySettings))
            {
                CurrentSettings = legacySettings;
                return;
            }

            CurrentSettings = new AppSettings();
        }
    }

    public async Task UpdateAsync(AppSettings settings)
    {
        CurrentSettings = settings;
        ApplyTheme(CurrentSettings.IsDarkTheme);
        await SaveAsync();
        _startupService?.SetStartOnStartup(CurrentSettings.StartOnStartup);
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

    private bool TryLoadLegacySettings(out AppSettings settings)
    {
        settings = new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("isDarkTheme", out var isDarkTheme))
            {
                settings.IsDarkTheme = isDarkTheme.GetBoolean();
            }

            if (root.TryGetProperty("language", out var language))
            {
                settings.Language = language.GetString() switch
                {
                    "English (en_US)" => Language.English,
                    "日本語 (ja_JP)" => Language.Japanese,
                    _ => Language.TraditionalChinese
                };
            }

            if (root.TryGetProperty("gameLaunchAction", out var launchAction))
            {
                settings.GameLaunchAction = launchAction.GetString() switch
                {
                    "隱藏" => GameLaunchAction.Hide,
                    "不變更" => GameLaunchAction.NoChange,
                    _ => GameLaunchAction.Minimize
                };
            }

            if (root.TryGetProperty("gameCloseAction", out var closeAction))
            {
                settings.GameCloseAction = closeAction.GetString() switch
                {
                    "永遠還原視窗" => GameCloseAction.AlwaysRestore,
                    "維持最小化" => GameCloseAction.KeepMinimized,
                    _ => GameCloseAction.RestoreWhenLaunchedFromUi
                };
            }

            if (root.TryGetProperty("showTrayIcon", out var showTrayIcon))
            {
                settings.ShowTrayIcon = showTrayIcon.GetBoolean();
            }

            if (root.TryGetProperty("trayIconStyle", out var trayIconStyle))
            {
                settings.TrayIconStyle = trayIconStyle.GetString() switch
                {
                    "預設" => TrayIconStyle.Default,
                    "簡約" => TrayIconStyle.Minimal,
                    _ => TrayIconStyle.Gamepad
                };
            }

            if (root.TryGetProperty("minimizeToTrayOnMinimize", out var minimizeOnMinimize))
            {
                settings.MinimizeToTrayOnMinimize = minimizeOnMinimize.GetBoolean();
            }

            if (root.TryGetProperty("minimizeToTrayOnClose", out var minimizeOnClose))
            {
                settings.MinimizeToTrayOnClose = minimizeOnClose.GetBoolean();
            }

            if (root.TryGetProperty("downloadMetadataAfterImport", out var downloadMetadata))
            {
                settings.DownloadMetadataAfterImport = downloadMetadata.GetBoolean();
            }

            if (root.TryGetProperty("startInFullscreen", out var startInFullscreen))
            {
                settings.StartInFullscreen = startInFullscreen.GetBoolean();
            }

            if (root.TryGetProperty("startOnStartup", out var startOnStartup))
            {
                settings.StartOnStartup = startOnStartup.GetBoolean();
            }

            if (root.TryGetProperty("minimizeToTrayAfterStartup", out var minimizeAfterStartup))
            {
                settings.MinimizeToTrayAfterStartup = minimizeAfterStartup.GetBoolean();
            }

            if (root.TryGetProperty("startMinimized", out var startMinimized))
            {
                settings.StartMinimized = startMinimized.GetBoolean();
            }

            if (root.TryGetProperty("updateInstallSizeOnLibraryUpdate", out var updateInstallSize))
            {
                settings.UpdateInstallSizeOnLibraryUpdate = updateInstallSize.GetBoolean();
            }

            if (root.TryGetProperty("useFuzzyMatchingInFilter", out var fuzzyMatching))
            {
                settings.UseFuzzyMatchingInFilter = fuzzyMatching.GetBoolean();
            }

            if (root.TryGetProperty("playtimeImportMode", out var playtimeImportMode))
            {
                settings.PlaytimeImportMode = playtimeImportMode.GetString() switch
                {
                    "所有遊戲" => PlaytimeImportMode.AllGames,
                    "不匯入" => PlaytimeImportMode.Disabled,
                    _ => PlaytimeImportMode.NewOnly
                };
            }

            return true;
        }
        catch
        {
            settings = new AppSettings();
            return false;
        }
    }
}
