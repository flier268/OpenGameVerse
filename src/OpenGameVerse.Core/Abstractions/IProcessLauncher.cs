using OpenGameVerse.Core.Common;

namespace OpenGameVerse.Core.Abstractions;

/// <summary>
/// Abstraction for launching external processes (games, launchers)
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Launch a process with optional arguments and environment variables
    /// </summary>
    /// <param name="target">The target to launch (executable path or protocol URL like steam://)</param>
    /// <param name="arguments">Optional command-line arguments</param>
    /// <param name="environmentVariables">Optional environment variables (e.g., WINEPREFIX)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure with error message</returns>
    Task<Result> LaunchAsync(
        string target,
        string? arguments = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default);
}
