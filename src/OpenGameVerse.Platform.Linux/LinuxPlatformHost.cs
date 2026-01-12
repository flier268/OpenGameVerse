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

    private readonly List<IGameScanner> _scanners;

    public LinuxPlatformHost()
    {
        _scanners = new List<IGameScanner>
        {
            new SteamScanner()
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
