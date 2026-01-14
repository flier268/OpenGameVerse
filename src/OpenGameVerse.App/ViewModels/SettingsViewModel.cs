using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameVerse.App.Services;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public sealed record OptionItem<T>(T Value, string Label);

    private readonly IAppSettingsService _settingsService;

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    [ObservableProperty]
    public partial OptionItem<Language>? SelectedLanguageOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<GameLaunchAction>? SelectedGameLaunchActionOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<GameCloseAction>? SelectedGameCloseActionOption { get; set; }

    [ObservableProperty]
    public partial bool ShowTrayIcon { get; set; }

    [ObservableProperty]
    public partial OptionItem<TrayIconStyle>? SelectedTrayIconStyleOption { get; set; }

    [ObservableProperty]
    public partial bool MinimizeToTrayOnMinimize { get; set; }

    [ObservableProperty]
    public partial bool MinimizeToTrayOnClose { get; set; }

    [ObservableProperty]
    public partial bool DownloadMetadataAfterImport { get; set; }

    [ObservableProperty]
    public partial bool StartInFullscreen { get; set; }

    [ObservableProperty]
    public partial bool StartOnStartup { get; set; }

    [ObservableProperty]
    public partial bool MinimizeToTrayAfterStartup { get; set; }

    [ObservableProperty]
    public partial bool StartMinimized { get; set; }

    [ObservableProperty]
    public partial bool UpdateInstallSizeOnLibraryUpdate { get; set; }

    [ObservableProperty]
    public partial bool UseFuzzyMatchingInFilter { get; set; }

    [ObservableProperty]
    public partial OptionItem<PlaytimeImportMode>? SelectedPlaytimeImportModeOption { get; set; }

    public IReadOnlyList<OptionItem<Language>> LanguageOptions { get; } = new[]
    {
        new OptionItem<Language>(Language.TraditionalChinese, "中文 (繁體)"),
        new OptionItem<Language>(Language.English, "English (en_US)"),
        new OptionItem<Language>(Language.Japanese, "日本語 (ja_JP)")
    };

    public IReadOnlyList<OptionItem<GameLaunchAction>> GameLaunchActionOptions { get; } = new[]
    {
        new OptionItem<GameLaunchAction>(GameLaunchAction.Minimize, "最小化"),
        new OptionItem<GameLaunchAction>(GameLaunchAction.Hide, "隱藏"),
        new OptionItem<GameLaunchAction>(GameLaunchAction.NoChange, "不變更")
    };

    public IReadOnlyList<OptionItem<GameCloseAction>> GameCloseActionOptions { get; } = new[]
    {
        new OptionItem<GameCloseAction>(GameCloseAction.RestoreWhenLaunchedFromUi, "僅在從介面啟動時還原視窗"),
        new OptionItem<GameCloseAction>(GameCloseAction.AlwaysRestore, "永遠還原視窗"),
        new OptionItem<GameCloseAction>(GameCloseAction.KeepMinimized, "維持最小化")
    };

    public IReadOnlyList<OptionItem<TrayIconStyle>> TrayIconStyleOptions { get; } = new[]
    {
        new OptionItem<TrayIconStyle>(TrayIconStyle.Gamepad, "遊戲手把"),
        new OptionItem<TrayIconStyle>(TrayIconStyle.Default, "預設"),
        new OptionItem<TrayIconStyle>(TrayIconStyle.Minimal, "簡約")
    };

    public IReadOnlyList<OptionItem<PlaytimeImportMode>> PlaytimeImportModeOptions { get; } = new[]
    {
        new OptionItem<PlaytimeImportMode>(PlaytimeImportMode.NewOnly, "只針對新匯入的遊戲"),
        new OptionItem<PlaytimeImportMode>(PlaytimeImportMode.AllGames, "所有遊戲"),
        new OptionItem<PlaytimeImportMode>(PlaytimeImportMode.Disabled, "不匯入")
    };

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        var settings = settingsService.CurrentSettings;
        IsDarkTheme = settings.IsDarkTheme;
        SelectedLanguageOption = FindOption(LanguageOptions, settings.Language);
        SelectedGameLaunchActionOption = FindOption(GameLaunchActionOptions, settings.GameLaunchAction);
        SelectedGameCloseActionOption = FindOption(GameCloseActionOptions, settings.GameCloseAction);
        ShowTrayIcon = settings.ShowTrayIcon;
        SelectedTrayIconStyleOption = FindOption(TrayIconStyleOptions, settings.TrayIconStyle);
        MinimizeToTrayOnMinimize = settings.MinimizeToTrayOnMinimize;
        MinimizeToTrayOnClose = settings.MinimizeToTrayOnClose;
        DownloadMetadataAfterImport = settings.DownloadMetadataAfterImport;
        StartInFullscreen = settings.StartInFullscreen;
        StartOnStartup = settings.StartOnStartup;
        MinimizeToTrayAfterStartup = settings.MinimizeToTrayAfterStartup;
        StartMinimized = settings.StartMinimized;
        UpdateInstallSizeOnLibraryUpdate = settings.UpdateInstallSizeOnLibraryUpdate;
        UseFuzzyMatchingInFilter = settings.UseFuzzyMatchingInFilter;
        SelectedPlaytimeImportModeOption = FindOption(PlaytimeImportModeOptions, settings.PlaytimeImportMode);
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var settings = new AppSettings
        {
            IsDarkTheme = IsDarkTheme,
            Language = SelectedLanguageOption?.Value ?? Language.TraditionalChinese,
            GameLaunchAction = SelectedGameLaunchActionOption?.Value ?? GameLaunchAction.Minimize,
            GameCloseAction = SelectedGameCloseActionOption?.Value ?? GameCloseAction.RestoreWhenLaunchedFromUi,
            ShowTrayIcon = ShowTrayIcon,
            TrayIconStyle = SelectedTrayIconStyleOption?.Value ?? TrayIconStyle.Gamepad,
            MinimizeToTrayOnMinimize = MinimizeToTrayOnMinimize,
            MinimizeToTrayOnClose = MinimizeToTrayOnClose,
            DownloadMetadataAfterImport = DownloadMetadataAfterImport,
            StartInFullscreen = StartInFullscreen,
            StartOnStartup = StartOnStartup,
            MinimizeToTrayAfterStartup = MinimizeToTrayAfterStartup,
            StartMinimized = StartMinimized,
            UpdateInstallSizeOnLibraryUpdate = UpdateInstallSizeOnLibraryUpdate,
            UseFuzzyMatchingInFilter = UseFuzzyMatchingInFilter,
            PlaytimeImportMode = SelectedPlaytimeImportModeOption?.Value ?? PlaytimeImportMode.NewOnly
        };

        await _settingsService.UpdateAsync(settings);
    }

    [RelayCommand]
    private void Close(Window? window)
    {
        window?.Close();
    }

    private static OptionItem<T> FindOption<T>(IReadOnlyList<OptionItem<T>> options, T value)
        where T : struct, Enum
    {
        foreach (var option in options)
        {
            if (EqualityComparer<T>.Default.Equals(option.Value, value))
            {
                return option;
            }
        }

        return options[0];
    }
}
