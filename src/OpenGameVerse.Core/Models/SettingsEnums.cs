namespace OpenGameVerse.Core.Models;

public enum Language
{
    TraditionalChinese = 0,
    English = 1,
    Japanese = 2,
}

public enum GameLaunchAction
{
    Minimize = 0,
    Hide = 1,
    NoChange = 2,
}

public enum GameCloseAction
{
    RestoreWhenLaunchedFromUi = 0,
    AlwaysRestore = 1,
    KeepMinimized = 2,
}

public enum TrayIconStyle
{
    Gamepad = 0,
    Default = 1,
    Minimal = 2,
}

public enum PlaytimeImportMode
{
    NewOnly = 0,
    AllGames = 1,
    Disabled = 2,
}
