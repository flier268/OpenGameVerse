using Avalonia;
using Avalonia.Controls;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.Services;

public sealed class WindowBehaviorService
{
    private readonly Window _mainWindow;
    private readonly IAppSettingsService _settingsService;
    private readonly TrayIconService _trayIconService;
    private bool _isExitRequested;

    public WindowBehaviorService(
        Window mainWindow,
        IAppSettingsService settingsService,
        TrayIconService trayIconService
    )
    {
        _mainWindow = mainWindow;
        _settingsService = settingsService;
        _trayIconService = trayIconService;

        _mainWindow.Closing += OnMainWindowClosing;
        _mainWindow.PropertyChanged += OnMainWindowPropertyChanged;
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public void RequestExit()
    {
        _isExitRequested = true;
        _mainWindow.Close();
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        _trayIconService.UpdateSettings(settings);

        if (!settings.ShowTrayIcon)
        {
            _trayIconService.ShowMainWindow();
        }
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        var settings = _settingsService.CurrentSettings;
        if (_isExitRequested || !settings.ShowTrayIcon || !settings.MinimizeToTrayOnClose)
        {
            return;
        }

        e.Cancel = true;
        _trayIconService.HideMainWindow();
    }

    private void OnMainWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(Window.WindowState))
        {
            return;
        }

        var settings = _settingsService.CurrentSettings;
        if (!settings.ShowTrayIcon || !settings.MinimizeToTrayOnMinimize)
        {
            return;
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _trayIconService.HideMainWindow();
        }
    }
}
