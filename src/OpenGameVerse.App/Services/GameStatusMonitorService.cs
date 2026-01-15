using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using OpenGameVerse.App.ViewModels;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.Services;

public sealed class GameStatusMonitorService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Lock _lock = new();
    private readonly Dictionary<long, List<WeakReference<GameViewModel>>> _trackedGames = new();
    private readonly Dictionary<long, bool> _lastStatus = new();
    private static readonly Lock CmdlineCacheLock = new();
    private static readonly Dictionary<int, string?> CmdlineCache = new();

    private static readonly HashSet<string> NonGameProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam",
        "steamwebhelper",
        "steamservice",
        "steamcmd",
        "steam.exe",
        "steamwebhelper.exe",
        "steamservice.exe",
        "steamcmd.exe",
        "explorer",
        "explorer.exe",
        "systemd",
        "dbus-daemon",
        "dbus-daemon-launch-helper",
        "pipewire",
        "pipewire-pulse",
        "pulseaudio",
        "wireplumber",
        "gnome-shell",
        "plasmashell",
        "kwin_x11",
        "kwin_wayland",
        "xorg",
        "xwayland",
        "xdg-open"
    };

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
                    list = [];
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

    public async Task<int> StopGameAsync(GameViewModel game, CancellationToken ct)
    {
        var target = game ?? throw new ArgumentNullException(nameof(game));
        var processIds = await Task.Run(() => FindMatchingProcessIds(target), ct);
        if (processIds.Count == 0)
        {
            return 0;
        }

        var stopped = 0;
        foreach (var pid in processIds)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var process = Process.GetProcessById(pid);
                process.Kill();
                stopped++;
            }
            catch
            {
                // Ignore failures to terminate specific processes
            }
        }

        return stopped;
    }

    public Task WaitForGameExitAsync(long gameId, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hasSeenRunning = _lastStatus.TryGetValue(gameId, out var isRunning) && isRunning;

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

        void Handler(long id, bool running)
        {
            if (id != gameId)
            {
                return;
            }

            switch (hasSeenRunning)
            {
                case false when running:
                    hasSeenRunning = true;
                    return;
                case true when !running:
                    GameStatusChanged -= Handler;
                    tcs.TrySetResult();
                    break;
            }
        }
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
            if (IsNonGameProcess(process))
            {
                continue;
            }

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

    private static List<int> FindMatchingProcessIds(GameViewModel game)
    {
        var processes = GetProcessSnapshot();
        var matches = new List<int>();

        foreach (var process in processes)
        {
            if (IsMatchProcess(game, process))
            {
                matches.Add(process.Id);
            }
        }

        return matches;
    }

    private static bool IsMatchProcess(GameViewModel game, ProcessInfo process)
    {
        if (IsNonGameProcess(process))
        {
            return false;
        }

        var executablePath = game.ExecutablePath;
        var installPath = NormalizePath(game.InstallPath);
        var appId = GetSteamAppId(game);
        var useProtocol = IsProtocolUrl(executablePath);

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
            if (IsSteamClientProcess(process))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private static bool IsSteamClientProcess(ProcessInfo process)
    {
        var commandLine = process.GetCommandLine();
        if (string.IsNullOrEmpty(process.Path) && string.IsNullOrEmpty(commandLine))
        {
            return false;
        }

        var path = process.Path ?? string.Empty;
        var cmd = commandLine ?? string.Empty;

        return path.Contains("steam.exe", GetPathComparison())
               || path.Contains("steamwebhelper", GetPathComparison())
               || cmd.Contains("steam.exe", GetPathComparison())
               || cmd.Contains("steamwebhelper", GetPathComparison());
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

        var commandLine = process.GetCommandLine();
        if (!string.IsNullOrEmpty(commandLine)
            && commandLine.Contains(normalizedExe, GetPathComparison()))
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

        var commandLine = process.GetCommandLine();
        if (!string.IsNullOrEmpty(commandLine))
        {
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

        var commandLine = process.GetCommandLine();
        if (!string.IsNullOrEmpty(commandLine)
            && commandLine.Contains(installPath, GetPathComparison()))
        {
            return true;
        }

        if (OperatingSystem.IsLinux())
        {
            var winePath = "Z:" + installPath.Replace('/', '\\');
            if (!string.IsNullOrEmpty(commandLine)
                && commandLine.Contains(winePath, StringComparison.OrdinalIgnoreCase))
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

    private static ReadOnlyCollection<ProcessInfo> GetProcessSnapshot()
    {
        return OperatingSystem.IsLinux() ? GetLinuxProcesses() : GetWindowsProcesses();
    }

    private static ReadOnlyCollection<ProcessInfo> GetWindowsProcesses()
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

                list.Add(new ProcessInfo(process.Id, NormalizePath(path), process.ProcessName));
            }
            catch
            {
                // Ignore process access failures
            }
        }

        return new ReadOnlyCollection<ProcessInfo>(list);
    }

    private static ReadOnlyCollection<ProcessInfo> GetLinuxProcesses()
    {
        var list = new List<ProcessInfo>();
        const string procRoot = "/proc";

        if (!Directory.Exists(procRoot))
        {
            return new ReadOnlyCollection<ProcessInfo>(list);
        }

        var seen = new HashSet<int>();
        foreach (var process in Process.GetProcesses())
        {
            if (IsNonGameName(process.ProcessName))
            {
                continue;
            }

            string? path;
            try
            {
                path = process.MainModule?.FileName;
            }
            catch
            {
                continue;
            }

            if (path is null)
                continue;

            seen.Add(process.Id);
            var cmdlinePath = Path.Combine(procRoot, process.Id.ToString(), "cmdline");
            list.Add(new ProcessInfo(process.Id, NormalizePath(path), process.ProcessName, cmdlinePath));
        }

        PruneCmdlineCache(seen);
        return new ReadOnlyCollection<ProcessInfo>(list);

        static void PruneCmdlineCache(HashSet<int> seenPids)
        {
            lock (CmdlineCacheLock)
            {
                if (CmdlineCache.Count == 0)
                {
                    return;
                }

                var toRemove = new List<int>();
                foreach (var pid in CmdlineCache.Keys)
                {
                    if (!seenPids.Contains(pid))
                    {
                        toRemove.Add(pid);
                    }
                }

                foreach (var pid in toRemove)
                {
                    CmdlineCache.Remove(pid);
                }
            }
        }
    }

    private sealed class ProcessInfo
    {
        private readonly string? _cmdlinePath;

        public ProcessInfo(int id, string? path, string? name, string? cmdlinePath = null)
        {
            Id = id;
            Path = path;
            Name = name;
            _cmdlinePath = cmdlinePath;
        }

        public int Id { get; }
        public string? Path { get; }
        public string? Name { get; }

        public string? GetCommandLine()
        {
            if (_cmdlinePath is null)
            {
                return null;
            }

            lock (CmdlineCacheLock)
            {
                if (CmdlineCache.TryGetValue(Id, out var cached))
                {
                    return cached;
                }
            }

            var cmdline = TryReadCmdline(_cmdlinePath);
            lock (CmdlineCacheLock)
            {
                CmdlineCache[Id] = cmdline;
            }

            return cmdline;
        }
    }

    private static string? TryReadCmdline(string path)
    {
        try
        {
            var data = File.ReadAllText(path);
            return data.TrimEnd('\0');
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNonGameProcess(ProcessInfo process)
    {
        if (IsNonGameName(process.Name))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(process.Path))
        {
            var fileName = Path.GetFileName(process.Path);
            if (IsNonGameName(fileName))
            {
                return true;
            }

            var noExtension = Path.GetFileNameWithoutExtension(fileName);
            if (IsNonGameName(noExtension))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNonGameName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && NonGameProcessNames.Contains(name);
    }


    private ReadOnlyCollection<GameViewModel> CollectLiveGames()
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

        return new ReadOnlyCollection<GameViewModel>(games);
    }
}
