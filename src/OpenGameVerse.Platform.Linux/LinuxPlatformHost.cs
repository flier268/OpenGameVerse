using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Platform.Linux.Scanners;

namespace OpenGameVerse.Platform.Linux;

/// <summary>
/// Linux platform host implementation
/// </summary>
public sealed class LinuxPlatformHost : IPlatformHost
{
    public PlatformType Platform => PlatformType.Linux;

    private readonly IProcessLauncher _processLauncher;
    private readonly List<IGameScanner> _scanners;

    public LinuxPlatformHost()
    {
        _processLauncher = new LinuxProcessLauncher();
        _scanners = new List<IGameScanner>
        {
            new SteamScanner(),
            new DesktopFileScanner()
        };
    }

    public IEnumerable<IGameScanner> GetScanners()
    {
        return _scanners;
    }

    public async Task<Result> LaunchGameAsync(GameInstallation installation, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(installation.ExecutablePath))
        {
            return Result.Failure("No executable path configured for this game");
        }

        // Prepare environment variables for Wine/Proton if needed
        var environmentVariables = PrepareEnvironmentVariables(installation);

        // Launch the game
        return await _processLauncher.LaunchAsync(
            installation.ExecutablePath,
            environmentVariables: environmentVariables,
            ct: ct);
    }

    private Dictionary<string, string>? PrepareEnvironmentVariables(GameInstallation installation)
    {
        // Steam games with steam:// protocol don't need environment variables
        if (installation.ExecutablePath?.StartsWith("steam://", StringComparison.OrdinalIgnoreCase) == true)
        {
            return null;
        }

        // Check if this is a Windows executable (.exe)
        if (installation.ExecutablePath?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Try to detect Wine prefix
            var winePrefix = DetectWinePrefix(installation.InstallPath);
            if (!string.IsNullOrEmpty(winePrefix))
            {
                return new Dictionary<string, string>
                {
                    ["WINEPREFIX"] = winePrefix,
                    ["WINEDEBUG"] = "-all"  // Suppress Wine debug output
                };
            }
        }

        return null;
    }

    private string? DetectWinePrefix(string installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        // Check standard default Wine prefix
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultPrefix = Path.Combine(home, ".wine");
        if (Directory.Exists(defaultPrefix))
        {
            return defaultPrefix;
        }

        // Check for game-specific prefix in parent directory
        var parentDir = Path.GetDirectoryName(installPath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            var prefixPath = Path.Combine(parentDir, "prefix");
            if (Directory.Exists(prefixPath))
            {
                return prefixPath;
            }
        }

        // Check for prefix in install directory itself
        var installPrefix = Path.Combine(installPath, "prefix");
        if (Directory.Exists(installPrefix))
        {
            return installPrefix;
        }

        return null;
    }
}
