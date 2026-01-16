using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Platform.Windows.Scanners;

/// <summary>
/// Windows GOG Galaxy scanner
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GogGalaxyScanner : IGameScanner
{
    public string ScannerId => "GOG";
    public string DisplayName => "GOG Galaxy";

    public Task<bool> IsInstalledAsync(CancellationToken ct)
    {
        // TODO: Check GOG Galaxy installation
        return Task.FromResult(false);
    }

    public async IAsyncEnumerable<GameInstallation> ScanAsync(
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        // TODO: Implement GOG Galaxy scanning
        await Task.CompletedTask;
        yield break;
    }
}
