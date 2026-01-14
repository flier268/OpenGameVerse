namespace OpenGameVerse.Core.Models;

public sealed class AppSettings
{
    public bool IsDarkTheme { get; set; } = true;
    public Language Language { get; set; } = Language.TraditionalChinese;
    public GameLaunchAction GameLaunchAction { get; set; } = GameLaunchAction.Minimize;
    public GameCloseAction GameCloseAction { get; set; } = GameCloseAction.RestoreWhenLaunchedFromUi;
    public bool ShowTrayIcon { get; set; } = true;
    public TrayIconStyle TrayIconStyle { get; set; } = TrayIconStyle.Gamepad;
    public bool MinimizeToTrayOnMinimize { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool DownloadMetadataAfterImport { get; set; } = true;
    public bool StartInFullscreen { get; set; }
    public bool StartOnStartup { get; set; }
    public bool MinimizeToTrayAfterStartup { get; set; }
    public bool StartMinimized { get; set; }
    public bool UpdateInstallSizeOnLibraryUpdate { get; set; } = true;
    public bool UseFuzzyMatchingInFilter { get; set; } = true;
    public PlaytimeImportMode PlaytimeImportMode { get; set; } = PlaytimeImportMode.NewOnly;
}
