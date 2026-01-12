using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Common;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Linux;

/// <summary>
/// Linux-specific process launcher implementation
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxProcessLauncher : IProcessLauncher
{
    public async Task<Result> LaunchAsync(
        string target,
        string? arguments = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return Result.Failure("Target path or URL cannot be empty");
        }

        try
        {
            // Handle protocol URLs (steam://, etc.)
            if (IsProtocolUrl(target))
            {
                return await LaunchProtocolUrlAsync(target, ct);
            }

            // Handle direct executable
            return await LaunchExecutableAsync(target, arguments, environmentVariables, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to launch process: {ex.Message}");
        }
    }

    private static bool IsProtocolUrl(string target)
    {
        return target.Contains("://", StringComparison.Ordinal);
    }

    private async Task<Result> LaunchProtocolUrlAsync(string url, CancellationToken ct)
    {
        try
        {
            // Use xdg-open to handle protocol URLs (steam://, etc.)
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);

            if (process == null)
            {
                return Result.Failure("Failed to start xdg-open process");
            }

            // Wait briefly to check if the process started successfully
            await Task.Delay(500, ct);

            if (process.HasExited && process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                return Result.Failure($"xdg-open failed: {error}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to launch protocol URL: {ex.Message}");
        }
    }

    private async Task<Result> LaunchExecutableAsync(
        string executablePath,
        string? arguments,
        Dictionary<string, string>? environmentVariables,
        CancellationToken ct)
    {
        try
        {
            if (!File.Exists(executablePath))
            {
                return Result.Failure($"Executable not found: {executablePath}");
            }

            var workingDirectory = Path.GetDirectoryName(executablePath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            // Add command-line arguments if provided
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                processStartInfo.Arguments = arguments;
            }

            // Add environment variables if provided (for Wine/Proton)
            if (environmentVariables != null)
            {
                foreach (var (key, value) in environmentVariables)
                {
                    processStartInfo.Environment[key] = value;
                }
            }

            using var process = Process.Start(processStartInfo);

            if (process == null)
            {
                return Result.Failure("Failed to start process");
            }

            // Wait briefly to check if the process started successfully
            await Task.Delay(500, ct);

            if (process.HasExited && process.ExitCode != 0)
            {
                return Result.Failure($"Process exited with code {process.ExitCode}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to launch executable: {ex.Message}");
        }
    }
}
