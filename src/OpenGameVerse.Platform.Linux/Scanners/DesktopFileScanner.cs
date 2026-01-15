using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Platform.Linux.Models;
using OpenGameVerse.Platform.Linux.Parsers;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Linux.Scanners;

/// <summary>
/// Scanner for games installed via .desktop files (Flatpak, Snap, native Linux games)
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class DesktopFileScanner : IGameScanner
{
    public string ScannerId => "DesktopFiles";
    public string DisplayName => "Desktop Applications";

    public Task<bool> IsInstalledAsync(CancellationToken ct)
    {
        // .desktop files are always available on Linux
        return Task.FromResult(true);
    }

    public async IAsyncEnumerable<GameInstallation> ScanAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var desktopDirs = GetDesktopFilePaths();

        foreach (var dir in desktopDirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            string[] desktopFiles;
            try
            {
                desktopFiles = Directory.GetFiles(dir, "*.desktop", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                // Skip directories we can't read
                continue;
            }

            foreach (var file in desktopFiles)
            {
                ct.ThrowIfCancellationRequested();

                var entry = await DesktopFileParser.ParseAsync(file, ct);

                // Only include games, exclude known launchers
                if (entry?.IsGame == true && !IsKnownLauncher(entry.Name))
                {
                    var installation = ConvertToGameInstallation(entry);
                    if (installation != null)
                    {
                        yield return installation;
                    }
                }
            }
        }
    }

    private static string[] GetDesktopFilePaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new[]
        {
            // System-wide applications
            "/usr/share/applications",
            "/usr/local/share/applications",

            // User-specific applications
            Path.Combine(home, ".local", "share", "applications"),

            // Flatpak applications
            Path.Combine(home, ".local", "share", "flatpak", "exports", "share", "applications"),
            "/var/lib/flatpak/exports/share/applications",

            // Snap applications
            "/var/lib/snapd/desktop/applications"
        };
    }

    private static bool IsKnownLauncher(string name)
    {
        // Exclude game launchers themselves (we scan their games separately)
        string[] launcherNames =
        {
            "steam",
            "steam-runtime",
            "lutris",
            "heroic",
            "legendary",
            "epic games",
            "gog galaxy",
            "bottles",
            "gamehub",
            "itch"
        };

        return launcherNames.Any(launcher =>
            name.Contains(launcher, StringComparison.OrdinalIgnoreCase));
    }

    private static GameInstallation? ConvertToGameInstallation(DesktopEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Exec))
        {
            return null;
        }

        // Try to determine install path from the executable
        var installPath = entry.Path ?? string.Empty;

        var flatpakInstallPath = TryResolveFlatpakInstallPath(entry);
        if (!string.IsNullOrWhiteSpace(flatpakInstallPath))
        {
            installPath = flatpakInstallPath;
        }

        // If we have a working directory (Path field), use that
        if (!string.IsNullOrWhiteSpace(entry.Path) && Directory.Exists(entry.Path))
        {
            installPath = entry.Path;
        }
        // Otherwise try to get directory from executable
        else if (File.Exists(entry.Exec))
        {
            var execDir = Path.GetDirectoryName(entry.Exec);
            if (!string.IsNullOrWhiteSpace(execDir)
                && Directory.Exists(execDir)
                && !IsSystemBinDirectory(execDir))
            {
                installPath = execDir;
            }
        }

        // If we still don't have a valid install path, use a placeholder
        if (string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath))
        {
            installPath = Path.GetDirectoryName(entry.FilePath) ?? "/usr/share/applications";
        }

        // Calculate size if possible
        long sizeBytes = 0;
        if (Directory.Exists(installPath))
        {
            try
            {
                var dirInfo = new DirectoryInfo(installPath);
                sizeBytes = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            catch
            {
                // Size calculation failed, not critical
            }
        }

        // Resolve icon path if it's not an absolute path
        string? iconPath = entry.Icon;
        if (!string.IsNullOrWhiteSpace(iconPath) && !Path.IsPathRooted(iconPath))
        {
            // Try to find the icon in common icon directories
            iconPath = FindIconPath(iconPath);
        }

        return new GameInstallation
        {
            Title = entry.Name,
            InstallPath = installPath,
            Platform = "Linux",
            ExecutablePath = entry.Exec,
            IconPath = iconPath,
            SizeBytes = sizeBytes
        };
    }

    private static string? TryResolveFlatpakInstallPath(DesktopEntry entry)
    {
        var appId = Path.GetFileNameWithoutExtension(entry.FilePath);
        if (string.IsNullOrWhiteSpace(appId))
        {
            return null;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userPath = Path.Combine(home, ".local", "share", "flatpak", "app", appId);
        if (Directory.Exists(userPath))
        {
            return userPath;
        }

        var systemPath = Path.Combine("/var/lib/flatpak/app", appId);
        if (Directory.Exists(systemPath))
        {
            return systemPath;
        }

        return null;
    }

    private static bool IsSystemBinDirectory(string path)
    {
        var normalized = path.TrimEnd(Path.DirectorySeparatorChar);
        return string.Equals(normalized, "/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/usr/local/bin", StringComparison.Ordinal)
               || string.Equals(normalized, "/snap/bin", StringComparison.Ordinal);
    }

    private static string? FindIconPath(string iconName)
    {
        // Common icon directories
        var iconDirs = new[]
        {
            "/usr/share/icons/hicolor",
            "/usr/share/pixmaps",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "icons", "hicolor")
        };

        var extensions = new[] { ".png", ".svg", ".xpm", ".jpg" };

        foreach (var baseDir in iconDirs)
        {
            if (!Directory.Exists(baseDir))
            {
                continue;
            }

            try
            {
                // Search recursively for the icon
                foreach (var ext in extensions)
                {
                    var iconFile = iconName + ext;
                    var matches = Directory.GetFiles(baseDir, iconFile, SearchOption.AllDirectories);
                    if (matches.Length > 0)
                    {
                        return matches[0];
                    }
                }
            }
            catch
            {
                // Continue if we can't search this directory
            }
        }

        return null;
    }
}
