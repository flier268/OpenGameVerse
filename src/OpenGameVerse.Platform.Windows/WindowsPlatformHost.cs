using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Platform.Windows.Scanners;

namespace OpenGameVerse.Platform.Windows;

/// <summary>
/// Windows platform host implementation
/// </summary>
public sealed class WindowsPlatformHost : IPlatformHost
{
    public PlatformType Platform => PlatformType.Windows;

    private readonly List<IGameScanner> _scanners;

    public WindowsPlatformHost()
    {
        _scanners = new List<IGameScanner>
        {
            new SteamScanner(),
            new EpicGamesScanner(),
            new GogGalaxyScanner()
        };
    }

    public IEnumerable<IGameScanner> GetScanners()
    {
        return _scanners;
    }

    public Task<Result> LaunchGameAsync(GameInstallation installation, CancellationToken ct)
    {
        // Phase 1: Not implemented
        return Task.FromResult(Result.Failure("Game launching not implemented in Phase 1"));
    }
}
