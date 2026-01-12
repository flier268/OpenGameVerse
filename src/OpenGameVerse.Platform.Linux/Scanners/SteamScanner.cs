using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Linux.Scanners;

/// <summary>
/// Linux Steam game scanner
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class SteamScanner : IGameScanner
{
    public string ScannerId => "Steam";
    public string DisplayName => "Steam";

    public Task<bool> IsInstalledAsync(CancellationToken ct)
    {
        var steamPath = GetSteamPath();
        return Task.FromResult(steamPath != null && Directory.Exists(steamPath));
    }

    public async IAsyncEnumerable<GameInstallation> ScanAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var steamPath = GetSteamPath();
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

    private string? GetSteamPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var paths = new[]
        {
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, "snap", "steam", "common", ".steam", "steam"),
            Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam")
        };

        return paths.FirstOrDefault(Directory.Exists);
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
                ? GetSteamCoverPath(steamPath, appId)
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

    private static string? GetSteamCoverPath(string steamPath, string appId)
    {
        var cacheDir = Path.Combine(steamPath, "appcache", "librarycache");
        var candidates = new[]
        {
            Path.Combine(cacheDir, $"{appId}_library_600x900.jpg"),
            Path.Combine(cacheDir, $"{appId}_library_600x900.png"),
            Path.Combine(cacheDir, $"{appId}_library_capsule.jpg"),
            Path.Combine(cacheDir, $"{appId}_library_capsule.png")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
