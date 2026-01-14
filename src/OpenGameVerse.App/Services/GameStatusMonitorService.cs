using System.Diagnostics;
using Avalonia.Threading;
using OpenGameVerse.App.ViewModels;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.Services;

public sealed class GameStatusMonitorService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly object _lock = new();
    private readonly Dictionary<long, List<WeakReference<GameViewModel>>> _trackedGames = new();
    private readonly Dictionary<long, bool> _lastStatus = new();
    private bool _isPolling;

    public event Action<long, bool>? GameStatusChanged;

    public GameStatusMonitorService(TimeSpan? interval = null)
    {
        _timer = new DispatcherTimer
        {
            Interval = interval ?? TimeSpan.FromSeconds(2)
        };
        _timer.Tick += OnTick;
    }

    public void UpdateTrackedGames(IEnumerable<GameViewModel> games)
    {
        lock (_lock)
        {
            foreach (var game in games)
            {
                if (!_trackedGames.TryGetValue(game.Id, out var list))
                {
                    list = new List<WeakReference<GameViewModel>>();
                    _trackedGames[game.Id] = list;
                }

                if (!list.Any(reference => reference.TryGetTarget(out var target) && ReferenceEquals(target, game)))
                {
                    list.Add(new WeakReference<GameViewModel>(game));
                }
            }
        }
    }

    public void Start()
    {
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTick;
    }

    public Task WaitForGameExitAsync(long gameId, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hasSeenRunning = _lastStatus.TryGetValue(gameId, out var isRunning) && isRunning;

        void Handler(long id, bool running)
        {
            if (id != gameId)
            {
                return;
            }

            if (!hasSeenRunning && running)
            {
                hasSeenRunning = true;
                return;
            }

            if (hasSeenRunning && !running)
            {
                GameStatusChanged -= Handler;
                tcs.TrySetResult();
            }
        }

        GameStatusChanged += Handler;

        if (ct.CanBeCanceled)
        {
            ct.Register(() =>
            {
                GameStatusChanged -= Handler;
                tcs.TrySetCanceled(ct);
            });
        }

        return tcs.Task;
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_isPolling)
        {
            return;
        }

        _isPolling = true;
        try
        {
            IReadOnlyList<GameViewModel> games;
            lock (_lock)
            {
                games = CollectLiveGames();
            }

            if (games.Count == 0)
            {
                return;
            }

            var statusMap = await Task.Run(() => BuildStatusMap(games));

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var game in games)
                {
                    if (statusMap.TryGetValue(game.Id, out var isRunning))
                    {
                        game.IsRunning = isRunning;
                    }
                }

                foreach (var (id, isRunning) in statusMap)
                {
                    if (!_lastStatus.TryGetValue(id, out var wasRunning) || wasRunning != isRunning)
                    {
                        _lastStatus[id] = isRunning;
                        GameStatusChanged?.Invoke(id, isRunning);
                    }
                }
            });
        }
        finally
        {
            _isPolling = false;
        }
    }

    private static Dictionary<long, bool> BuildStatusMap(IReadOnlyList<GameViewModel> games)
    {
        var processes = GetProcessSnapshot();
        var map = new Dictionary<long, bool>(games.Count);

        foreach (var game in games)
        {
            map[game.Id] = IsGameRunning(game, processes);
        }

        return map;
    }

    private static bool IsGameRunning(GameViewModel game, IReadOnlyList<ProcessInfo> processes)
    {
        var executablePath = game.ExecutablePath;
        var installPath = NormalizePath(game.InstallPath);
        var appId = GetSteamAppId(game);
        var useProtocol = IsProtocolUrl(executablePath);

        foreach (var process in processes)
        {
            if (!string.IsNullOrEmpty(executablePath) && !useProtocol)
            {
                if (IsMatchForExecutable(process, executablePath))
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(installPath))
            {
                if (PathContains(process, installPath))
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(appId) && IsSteamAppProcess(process, appId))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetSteamAppId(GameViewModel game)
    {
        if (!string.IsNullOrWhiteSpace(game.PlatformId))
        {
            return game.PlatformId;
        }

        if (string.IsNullOrWhiteSpace(game.ExecutablePath))
        {
            return null;
        }

        var text = game.ExecutablePath;
        if (!text.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = text.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var last = parts[^1];
        return long.TryParse(last, out _) ? last : null;
    }

    private static bool IsProtocolUrl(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && path.Contains("://", StringComparison.Ordinal);
    }

    private static bool IsMatchForExecutable(ProcessInfo process, string executablePath)
    {
        var normalizedExe = NormalizePath(executablePath);
        if (string.IsNullOrEmpty(normalizedExe))
        {
            return false;
        }

        if (PathEquals(process.Path, normalizedExe))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(process.CommandLine)
            && process.CommandLine.Contains(normalizedExe, GetPathComparison()))
        {
            return true;
        }

        return false;
    }

    private static bool IsSteamAppProcess(ProcessInfo process, string appId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(process.Path))
        {
            if (process.Path.Contains(Path.Combine("compatdata", appId), GetPathComparison()))
            {
                return true;
            }
        }

        if (!string.IsNullOrEmpty(process.CommandLine))
        {
            var commandLine = process.CommandLine;
            if (commandLine.Contains(appId, GetPathComparison())
                && commandLine.Contains("steamapps", GetPathComparison()))
            {
                return true;
            }

            if (commandLine.Contains(Path.Combine("compatdata", appId), GetPathComparison()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PathContains(ProcessInfo process, string installPath)
    {
        if (!string.IsNullOrEmpty(process.Path)
            && process.Path.StartsWith(installPath, GetPathComparison()))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(process.CommandLine)
            && process.CommandLine.Contains(installPath, GetPathComparison()))
        {
            return true;
        }

        if (OperatingSystem.IsLinux())
        {
            var winePath = "Z:" + installPath.Replace('/', '\\');
            if (!string.IsNullOrEmpty(process.CommandLine)
                && process.CommandLine.Contains(winePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static bool PathEquals(string? left, string? right)
    {
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
        {
            return false;
        }

        return string.Equals(left, right, GetPathComparison());
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    private static IReadOnlyList<ProcessInfo> GetProcessSnapshot()
    {
        if (OperatingSystem.IsLinux())
        {
            return GetLinuxProcesses();
        }

        return GetWindowsProcesses();
    }

    private static IReadOnlyList<ProcessInfo> GetWindowsProcesses()
    {
        var list = new List<ProcessInfo>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                string? path = null;
                try
                {
                    path = process.MainModule?.FileName;
                }
                catch
                {
                    path = null;
                }

                list.Add(new ProcessInfo(process.Id, NormalizePath(path), null));
            }
            catch
            {
                // Ignore process access failures
            }
        }

        return list;
    }

    private static IReadOnlyList<ProcessInfo> GetLinuxProcesses()
    {
        var list = new List<ProcessInfo>();
        var procRoot = "/proc";

        if (!Directory.Exists(procRoot))
        {
            return list;
        }

        foreach (var dir in Directory.EnumerateDirectories(procRoot))
        {
            var name = Path.GetFileName(dir);
            if (!int.TryParse(name, out var pid))
            {
                continue;
            }

            var exePath = TryReadLink(Path.Combine(dir, "exe"));
            var cmdline = TryReadCmdline(Path.Combine(dir, "cmdline"));

            list.Add(new ProcessInfo(pid, NormalizePath(exePath), cmdline));
        }

        return list;
    }

    private static string? TryReadLink(string path)
    {
        try
        {
            return new FileInfo(path).ResolveLinkTarget(true)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadCmdline(string path)
    {
        try
        {
            var data = File.ReadAllText(path);
            return data.Replace('\0', ' ').Trim();
        }
        catch
        {
            return null;
        }
    }

    private sealed record ProcessInfo(int Id, string? Path, string? CommandLine);

    private IReadOnlyList<GameViewModel> CollectLiveGames()
    {
        var games = new List<GameViewModel>();
        var toRemove = new List<long>();

        foreach (var (id, list) in _trackedGames)
        {
            var hasLive = false;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].TryGetTarget(out var target))
                {
                    games.Add(target);
                    hasLive = true;
                }
                else
                {
                    list.RemoveAt(i);
                }
            }

            if (!hasLive)
            {
                toRemove.Add(id);
            }
        }

        foreach (var id in toRemove)
        {
            _trackedGames.Remove(id);
        }

        return games;
    }
}
