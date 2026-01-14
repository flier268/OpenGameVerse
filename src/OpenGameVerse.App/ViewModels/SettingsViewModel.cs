using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameVerse.App.Services;

namespace OpenGameVerse.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _settingsService;

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; }

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
        IsDarkTheme = settingsService.CurrentSettings.IsDarkTheme;
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        await _settingsService.SetThemeAsync(IsDarkTheme);
    }

    [RelayCommand]
    private void Close(Window? window)
    {
        window?.Close();
    }
}
