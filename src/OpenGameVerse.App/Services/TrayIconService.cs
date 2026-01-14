using Avalonia.Controls;
using Avalonia.Platform;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly Action _exitAction;
    private TrayIcon? _trayIcon;
    private bool _isDisposed;

    public TrayIconService(Window mainWindow, Action exitAction)
    {
        _mainWindow = mainWindow;
        _exitAction = exitAction;
        InitializeTrayIcon();
    }

    public void UpdateSettings(AppSettings settings)
    {
        if (_trayIcon == null)
        {
            return;
        }

        _trayIcon.IsVisible = settings.ShowTrayIcon;
        _trayIcon.ToolTipText = GetTooltip(settings.TrayIconStyle);
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void HideMainWindow()
    {
        _mainWindow.Hide();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _trayIcon?.Dispose();
        _trayIcon = null;
        _isDisposed = true;
    }

    private void InitializeTrayIcon()
    {
        var icon = new WindowIcon(AssetLoader.Open(new Uri("avares://OpenGameVerse.App/Assets/avalonia-logo.ico")));

        var menu = new NativeMenu();
        var openItem = new NativeMenuItem("顯示 OpenGameVerse");
        openItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new NativeMenuItem("退出");
        exitItem.Click += (_, _) => _exitAction();

        menu.Items.Add(openItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "OpenGameVerse",
            Menu = menu,
            IsVisible = false
        };
    }

    private static string GetTooltip(TrayIconStyle style)
    {
        return style switch
        {
            TrayIconStyle.Gamepad => "OpenGameVerse",
            TrayIconStyle.Default => "OpenGameVerse",
            TrayIconStyle.Minimal => "OpenGameVerse (Minimal)",
            _ => "OpenGameVerse"
        };
    }
}
