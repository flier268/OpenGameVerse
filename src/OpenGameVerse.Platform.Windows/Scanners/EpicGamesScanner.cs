using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Windows.Scanners;

/// <summary>
/// Windows Epic Games Launcher scanner
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class EpicGamesScanner : IGameScanner
{
    public string ScannerId => "Epic";
    public string DisplayName => "Epic Games";

    public Task<bool> IsInstalledAsync(CancellationToken ct)
    {
        // TODO: Check Epic Games Launcher installation
        return Task.FromResult(false);
    }

    public async IAsyncEnumerable<GameInstallation> ScanAsync([EnumeratorCancellation] CancellationToken ct)
    {
        // TODO: Implement Epic Games scanning
        await Task.CompletedTask;
        yield break;
    }
}
