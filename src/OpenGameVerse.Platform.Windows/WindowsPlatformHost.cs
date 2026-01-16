using System.Runtime.Versioning;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Platform.Windows.Scanners;

namespace OpenGameVerse.Platform.Windows;

/// <summary>
/// Windows platform host implementation
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPlatformHost : IPlatformHost
{
    public PlatformType Platform => PlatformType.Windows;

    private readonly IProcessLauncher _processLauncher;
    private readonly List<IGameScanner> _scanners;

    public WindowsPlatformHost()
    {
        _processLauncher = new WindowsProcessLauncher();
        _scanners = new List<IGameScanner>
        {
            new SteamScanner(),
            new EpicGamesScanner(),
            new GogGalaxyScanner(),
        };
    }

    public IEnumerable<IGameScanner> GetScanners()
    {
        return _scanners;
    }

    public async Task<Result<System.Diagnostics.Process?>> LaunchGameAsync(
        GameInstallation installation,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(installation.ExecutablePath))
        {
            return Result<System.Diagnostics.Process?>.Failure(
                "No executable path configured for this game"
            );
        }

        // Launch the game (no special environment variables needed on Windows)
        return await _processLauncher.LaunchAsync(installation.ExecutablePath, ct: ct);
    }
}
