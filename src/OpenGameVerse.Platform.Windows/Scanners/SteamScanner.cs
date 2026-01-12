using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Windows.Scanners;

/// <summary>
/// Windows Steam game scanner
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SteamScanner : IGameScanner
{
    public string ScannerId => "Steam";
    public string DisplayName => "Steam";

    public Task<bool> IsInstalledAsync(CancellationToken ct)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (key?.GetValue("InstallPath") is string path)
            {
                return Task.FromResult(Directory.Exists(path));
            }

            // Try 32-bit registry
            using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key32?.GetValue("InstallPath") is string path32)
            {
                return Task.FromResult(Directory.Exists(path32));
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async IAsyncEnumerable<GameInstallation> ScanAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var steamPath = GetSteamInstallPath();
        if (steamPath == null) yield break;

        var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath)) yield break;

        // Parse VDF file
        VProperty? vdfData = null;
        try
        {
            var vdfText = await File.ReadAllTextAsync(libraryFoldersPath, ct);
            vdfData = VdfConvert.Deserialize(vdfText);
        }
        catch
        {
            yield break;
        }

        if (vdfData == null) yield break;

        // Iterate through library folders
        foreach (var child in vdfData.Value.Children())
        {
            if (child is not VProperty property) continue;

            var pathToken = property.Value["path"];
            if (pathToken?.ToString() is not string libraryPath) continue;

            // Scan this library's steamapps directory
            var steamAppsPath = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(steamAppsPath)) continue;

            // Find .acf manifest files
            var acfFiles = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");

            foreach (var acfFile in acfFiles)
            {
                ct.ThrowIfCancellationRequested();

                var installation = await ParseAcfManifestAsync(acfFile, libraryPath, steamPath, ct);
                if (installation != null)
                {
                    yield return installation;
                }
            }
        }
    }

    private string? GetSteamInstallPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (key?.GetValue("InstallPath") is string path)
            {
                return path;
            }

            using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key32?.GetValue("InstallPath") is string path32)
            {
                return path32;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<GameInstallation?> ParseAcfManifestAsync(
        string acfPath,
        string libraryPath,
        string steamPath,
        CancellationToken ct)
    {
        try
        {
            var acfText = await File.ReadAllTextAsync(acfPath, ct);
            var acfData = VdfConvert.Deserialize(acfText);

            var appState = acfData.Value;
            var name = appState["name"]?.ToString();
            var installDir = appState["installdir"]?.ToString();
            var appId = appState["appid"]?.ToString();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(installDir))
            {
                return null;
            }

            var installPath = Path.Combine(libraryPath, "steamapps", "common", installDir);

            if (!Directory.Exists(installPath))
            {
                return null;
            }

            // Calculate directory size
            long sizeBytes = 0;
            try
            {
                var dirInfo = new DirectoryInfo(installPath);
                sizeBytes = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            catch
            {
                // Size calculation failed, not critical
            }

            var coverImagePath = !string.IsNullOrWhiteSpace(appId)
                ? GetSteamCoverPath(steamPath, libraryPath, appId)
                : null;
            var executablePath = !string.IsNullOrWhiteSpace(appId)
                ? $"steam://run/{appId}"
                : null;

            return new GameInstallation
            {
                Title = name,
                InstallPath = installPath,
                Platform = "Steam",
                PlatformId = appId,
                ExecutablePath = executablePath,
                CoverImagePath = coverImagePath,
                SizeBytes = sizeBytes
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetSteamCoverPath(string steamPath, string libraryPath, string appId)
    {
        string[] cacheDirs =
        {
            Path.Combine(steamPath, "appcache", "librarycache"),
            Path.Combine(steamPath, "steamapps", "librarycache"),
            Path.Combine(libraryPath, "steamapps", "librarycache")
        };

        string[] candidates =
        {
            $"{appId}_library_600x900.jpg",
            $"{appId}_library_600x900.png",
            $"{appId}_library_600x900.webp",
            $"{appId}_library_capsule.jpg",
            $"{appId}_library_capsule.png",
            $"{appId}_library_capsule.webp"
        };

        foreach (var cacheDir in cacheDirs)
        {
            if (!Directory.Exists(cacheDir))
            {
                continue;
            }

            foreach (var candidate in candidates)
            {
                var path = Path.Combine(cacheDir, candidate);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            var appDirPath = Path.Combine(cacheDir, appId);
            var nestedMatch = FindBestCoverInDir(appDirPath);
            if (!string.IsNullOrEmpty(nestedMatch))
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private static string? FindBestCoverInDir(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        string[] preferredTokens =
        {
            "library_600x900",
            "library_capsule",
            "capsule",
            "library"
        };

        try
        {
            var imageFiles = Directory.EnumerateFiles(directory)
                .Where(IsImageFile)
                .ToList();

            foreach (var token in preferredTokens)
            {
                var match = imageFiles.FirstOrDefault(file =>
                    Path.GetFileName(file).Contains(token, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match))
                {
                    return match;
                }
            }

            return imageFiles
                .OrderByDescending(file => new FileInfo(file).Length)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }
}
