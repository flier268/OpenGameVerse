using System.Diagnostics;
using System.Runtime.Versioning;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Common;

namespace OpenGameVerse.Platform.Windows;

/// <summary>
/// Windows-specific process launcher implementation
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsProcessLauncher : IProcessLauncher
{
    public async Task<Result<Process?>> LaunchAsync(
        string target,
        string? arguments = null,
        Dictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return Result<Process?>.Failure("Target path or URL cannot be empty");
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
            return Result<Process?>.Failure($"Failed to launch process: {ex.Message}");
        }
    }

    private static bool IsProtocolUrl(string target)
    {
        return target.Contains("://", StringComparison.Ordinal);
    }

    private async Task<Result<Process?>> LaunchProtocolUrlAsync(string url, CancellationToken ct)
    {
        try
        {
            // Use ShellExecute for protocol URLs
            var processStartInfo = new ProcessStartInfo { FileName = url, UseShellExecute = true };

            using var process = Process.Start(processStartInfo);

            if (process == null)
            {
                return Result<Process?>.Failure("Failed to start process for protocol URL");
            }

            // Wait briefly to check if the process started successfully
            await Task.Delay(500, ct);

            return Result<Process?>.Success(null);
        }
        catch (Exception ex)
        {
            return Result<Process?>.Failure($"Failed to launch protocol URL: {ex.Message}");
        }
    }

    private async Task<Result<Process?>> LaunchExecutableAsync(
        string executablePath,
        string? arguments,
        Dictionary<string, string>? environmentVariables,
        CancellationToken ct
    )
    {
        try
        {
            if (!File.Exists(executablePath))
            {
                return Result<Process?>.Failure($"Executable not found: {executablePath}");
            }

            var workingDirectory = Path.GetDirectoryName(executablePath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = workingDirectory ?? string.Empty,
            };

            // Add command-line arguments if provided
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                processStartInfo.Arguments = arguments;
            }

            // Add environment variables if provided
            if (environmentVariables != null)
            {
                foreach (var (key, value) in environmentVariables)
                {
                    processStartInfo.Environment[key] = value;
                }
            }

            var process = Process.Start(processStartInfo);

            if (process == null)
            {
                return Result<Process?>.Failure("Failed to start process");
            }

            // Wait briefly to check if the process started successfully
            await Task.Delay(500, ct);

            if (process.HasExited && process.ExitCode != 0)
            {
                return Result<Process?>.Failure($"Process exited with code {process.ExitCode}");
            }

            return Result<Process?>.Success(process);
        }
        catch (Exception ex)
        {
            return Result<Process?>.Failure($"Failed to launch executable: {ex.Message}");
        }
    }
}
