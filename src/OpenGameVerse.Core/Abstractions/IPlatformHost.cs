using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Abstractions;

/// <summary>
/// Platform-specific operations abstraction for OS-level integration
/// </summary>
public interface IPlatformHost
{
    /// <summary>
    /// Gets the current operating system type
    /// </summary>
    PlatformType Platform { get; }

    /// <summary>
    /// Gets all registered game scanners for this platform
    /// </summary>
    IEnumerable<IGameScanner> GetScanners();

    /// <summary>
    /// Launches a game with platform-specific execution
    /// </summary>
    Task<Result<System.Diagnostics.Process?>> LaunchGameAsync(
        GameInstallation installation,
        CancellationToken ct
    );
}
