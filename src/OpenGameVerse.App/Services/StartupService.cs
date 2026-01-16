using System.Runtime.Versioning;
using Microsoft.Win32;

namespace OpenGameVerse.App.Services;

public interface IStartupService
{
    void SetStartOnStartup(bool enable);
}

public sealed class StartupService : IStartupService
{
    private const string WindowsRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "OpenGameVerse";

    public void SetStartOnStartup(bool enable)
    {
        if (OperatingSystem.IsWindows())
        {
            SetWindowsStartup(enable);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            SetLinuxStartup(enable);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SetWindowsStartup(bool enable)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(WindowsRunKey, writable: true);
        if (key == null)
        {
            return;
        }

        if (enable)
        {
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    private static void SetLinuxStartup(bool enable)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        var autostartDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "autostart"
        );
        var desktopFile = Path.Combine(autostartDir, "OpenGameVerse.desktop");

        if (!enable)
        {
            if (File.Exists(desktopFile))
            {
                File.Delete(desktopFile);
            }

            return;
        }

        Directory.CreateDirectory(autostartDir);

        var content = string.Join(
            '\n',
            new[]
            {
                "[Desktop Entry]",
                "Type=Application",
                "Name=OpenGameVerse",
                $"Exec=\"{exePath}\"",
                "X-GNOME-Autostart-enabled=true",
                "NoDisplay=false",
            }
        );

        File.WriteAllText(desktopFile, content);
    }
}
