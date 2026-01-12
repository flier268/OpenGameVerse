using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Abstractions;

/// <summary>
/// Scanner for a specific game platform/launcher
/// </summary>
public interface IGameScanner
{
    /// <summary>
    /// Scanner identifier (Steam, Epic, GOG, Flatpak, etc.)
    /// </summary>
    string ScannerId { get; }

    /// <summary>
    /// Display name for this scanner
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Check if this scanner's platform is installed on the system
    /// </summary>
    Task<bool> IsInstalledAsync(CancellationToken ct);

    /// <summary>
    /// Scan for games using IAsyncEnumerable for non-blocking iteration
    /// </summary>
    IAsyncEnumerable<GameInstallation> ScanAsync(CancellationToken ct);
}
