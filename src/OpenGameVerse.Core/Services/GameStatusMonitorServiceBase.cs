using System.Collections.ObjectModel;
using System.Diagnostics;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Services;

public abstract class GameStatusMonitorServiceBase : IGameStatusMonitorService
{
    private readonly TimeSpan _interval;
    private readonly Lock _lock = new();
    private readonly Dictionary<long, GameStatusTarget> _trackedGames = new();
    private readonly Dictionary<long, bool> _lastStatus = new();
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private int _isPolling;

    private static readonly HashSet<string> NonGameProcessNames = new(
        StringComparer.OrdinalIgnoreCase
    )
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
        "xdg-open",
    };

    public event Action<long, bool>? GameStatusChanged;

    protected GameStatusMonitorServiceBase(TimeSpan? interval = null)
    {
        _interval = interval ?? TimeSpan.FromSeconds(3);
    }

    protected abstract StringComparison PathComparison { get; }

    protected virtual bool IncludeWinePath => false;

    protected virtual bool IsInstallPathTooBroad(string installPath)
    {
        return false;
    }

    protected abstract ReadOnlyCollection<ProcessInfo> GetProcessSnapshot();

    public void UpdateTrackedGames(IEnumerable<GameStatusTarget> games)
    {
        ArgumentNullException.ThrowIfNull(games);

        lock (_lock)
        {
            _trackedGames.Clear();
            foreach (var game in games)
            {
                _trackedGames[game.Id] = game;
            }
        }
    }

    public void Start()
    {
        if (_cts != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _pollingTask = Task.Run(() => PollAsync(_cts.Token));
    }

    public void Stop()
    {
        if (_cts == null)
        {
            return;
        }

        var cts = _cts;
        _cts = null;
        cts.Cancel();
        cts.Dispose();
        _pollingTask = null;
    }

    public void Dispose()
    {
        Stop();
    }

    public async Task<int> StopGameAsync(GameStatusTarget game, CancellationToken ct)
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

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                await PollOnceAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during shutdown
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _isPolling, 1) == 1)
        {
            return;
        }

        try
        {
            IReadOnlyList<GameStatusTarget> games;
            lock (_lock)
            {
                games = CollectLiveGames();
            }

            if (games.Count == 0)
            {
                return;
            }

            Dictionary<long, bool> statusMap;
            try
            {
                statusMap = await Task.Run(() => BuildStatusMap(games), ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            foreach (var (id, isRunning) in statusMap)
            {
                if (!_lastStatus.TryGetValue(id, out var wasRunning) || wasRunning != isRunning)
                {
                    _lastStatus[id] = isRunning;
                    GameStatusChanged?.Invoke(id, isRunning);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isPolling, 0);
        }
    }

    private Dictionary<long, bool> BuildStatusMap(IReadOnlyList<GameStatusTarget> games)
    {
        var processes = GetProcessSnapshot();
        var map = new Dictionary<long, bool>(games.Count);

        foreach (var game in games)
        {
            map[game.Id] = IsGameRunning(game, processes);
        }

        return map;
    }

    private bool IsGameRunning(GameStatusTarget game, IReadOnlyList<ProcessInfo> processes)
    {
        var executablePath = game.ExecutablePath;
        var installPath = NormalizePath(game.InstallPath);
        var appId = GetSteamAppId(game);
        var flatpakAppId = GetFlatpakAppId(game);
        var flatpakProcessNames = GetFlatpakProcessNames(flatpakAppId, game.Title);
        var useProtocol = IsProtocolUrl(executablePath);

        foreach (var process in processes)
        {
            if (IsNonGameProcess(process))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(executablePath) && !useProtocol)
            {
                if (IsFlatpakExecutable(executablePath))
                {
                    if (IsFlatpakAppProcess(process, flatpakAppId, flatpakProcessNames))
                    {
                        return true;
                    }
                }
                else if (IsMatchForExecutable(process, executablePath))
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(installPath) && !IsInstallPathTooBroad(installPath))
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

    private List<int> FindMatchingProcessIds(GameStatusTarget game)
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

    private bool IsMatchProcess(GameStatusTarget game, ProcessInfo process)
    {
        if (IsNonGameProcess(process))
        {
            return false;
        }

        var executablePath = game.ExecutablePath;
        var installPath = NormalizePath(game.InstallPath);
        var appId = GetSteamAppId(game);
        var flatpakAppId = GetFlatpakAppId(game);
        var flatpakProcessNames = GetFlatpakProcessNames(flatpakAppId, game.Title);
        var useProtocol = IsProtocolUrl(executablePath);

        if (!string.IsNullOrEmpty(executablePath) && !useProtocol)
        {
            if (IsFlatpakExecutable(executablePath))
            {
                if (IsFlatpakAppProcess(process, flatpakAppId, flatpakProcessNames))
                {
                    return true;
                }
            }
            else if (IsMatchForExecutable(process, executablePath))
            {
                return true;
            }
        }

        if (!string.IsNullOrEmpty(installPath))
        {
            if (!IsInstallPathTooBroad(installPath) && PathContains(process, installPath))
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

    private bool IsSteamClientProcess(ProcessInfo process)
    {
        var commandLine = process.CommandLine;
        if (string.IsNullOrEmpty(process.Path) && string.IsNullOrEmpty(commandLine))
        {
            return false;
        }

        var path = process.Path ?? string.Empty;
        var cmd = commandLine ?? string.Empty;

        return path.Contains("steam.exe", PathComparison)
            || path.Contains("steamwebhelper", PathComparison)
            || cmd.Contains("steam.exe", PathComparison)
            || cmd.Contains("steamwebhelper", PathComparison);
    }

    private static string? GetSteamAppId(GameStatusTarget game)
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

    private static string? GetFlatpakAppId(GameStatusTarget game)
    {
        var installPath = NormalizePath(game.InstallPath);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        const string marker = "/flatpak/app/";
        var index = installPath.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var remaining = installPath[(index + marker.Length)..];
        var parts = remaining.Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries
        );
        return parts.Length > 0 ? parts[0] : null;
    }

    private static bool IsProtocolUrl(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && path.Contains("://", StringComparison.Ordinal);
    }

    private static bool IsFlatpakExecutable(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return string.Equals(Path.GetFileName(path), "flatpak", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsFlatpakAppProcess(ProcessInfo process, string? appId)
    {
        return IsFlatpakAppProcess(process, appId, Array.Empty<string>());
    }

    private bool IsFlatpakAppProcess(
        ProcessInfo process,
        string? appId,
        IReadOnlyCollection<string> processNames
    )
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            if (processNames.Count == 0)
            {
                return false;
            }
        }

        var commandLine = process.CommandLine;
        if (!string.IsNullOrEmpty(commandLine) && !string.IsNullOrWhiteSpace(appId))
        {
            if (commandLine.Contains(appId, PathComparison))
            {
                return true;
            }
        }

        if (!string.IsNullOrEmpty(process.Path) && !string.IsNullOrWhiteSpace(appId))
        {
            var flatpakAppRoot = "/flatpak/app/" + appId + "/";
            if (process.Path.Contains(flatpakAppRoot, PathComparison))
            {
                return true;
            }
        }

        if (processNames.Count > 0)
        {
            var name = NormalizeProcessName(process.Name);
            if (!string.IsNullOrEmpty(name))
            {
                foreach (var candidate in processNames)
                {
                    if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        if (
                            !string.IsNullOrEmpty(process.Path)
                            && process.Path.StartsWith("/app/", StringComparison.Ordinal)
                        )
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static IReadOnlyCollection<string> GetFlatpakProcessNames(string? appId, string? title)
    {
        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(appId))
        {
            var lastSegment = appId
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            if (!string.IsNullOrWhiteSpace(lastSegment))
            {
                names.Add(lastSegment);
                names.Add(lastSegment.ToLowerInvariant());
            }
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var trimmed = title.Trim();
            if (trimmed.Length > 0)
            {
                names.Add(trimmed);
                names.Add(trimmed.ToLowerInvariant());
            }
        }

        return names;
    }

    private static string? NormalizeProcessName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private bool IsMatchForExecutable(ProcessInfo process, string executablePath)
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

        var commandLine = process.CommandLine;
        if (
            !string.IsNullOrEmpty(commandLine)
            && commandLine.Contains(normalizedExe, PathComparison)
        )
        {
            return true;
        }

        return false;
    }

    private bool IsSteamAppProcess(ProcessInfo process, string appId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(process.Path))
        {
            if (process.Path.Contains(Path.Combine("compatdata", appId), PathComparison))
            {
                return true;
            }
        }

        var commandLine = process.CommandLine;
        if (!string.IsNullOrEmpty(commandLine))
        {
            if (
                commandLine.Contains(appId, PathComparison)
                && commandLine.Contains("steamapps", PathComparison)
            )
            {
                return true;
            }

            if (commandLine.Contains(Path.Combine("compatdata", appId), PathComparison))
            {
                return true;
            }
        }

        return false;
    }

    private bool PathContains(ProcessInfo process, string installPath)
    {
        if (
            !string.IsNullOrEmpty(process.Path)
            && process.Path.StartsWith(installPath, PathComparison)
        )
        {
            return true;
        }

        var commandLine = process.CommandLine;
        if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains(installPath, PathComparison))
        {
            return true;
        }

        if (IncludeWinePath)
        {
            var winePath = "Z:" + installPath.Replace('/', '\\');
            if (
                !string.IsNullOrEmpty(commandLine)
                && commandLine.Contains(winePath, StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    protected static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private bool PathEquals(string? left, string? right)
    {
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
        {
            return false;
        }

        return string.Equals(left, right, PathComparison);
    }

    protected sealed class ProcessInfo
    {
        public ProcessInfo(int id, string? path, string? name, string? commandLine = null)
        {
            Id = id;
            Path = path;
            Name = name;
            CommandLine = commandLine;
        }

        public int Id { get; }
        public string? Path { get; }
        public string? Name { get; }
        public string? CommandLine { get; }
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

    private ReadOnlyCollection<GameStatusTarget> CollectLiveGames()
    {
        var games = new List<GameStatusTarget>(_trackedGames.Count);
        foreach (var game in _trackedGames.Values)
        {
            games.Add(game);
        }

        return new ReadOnlyCollection<GameStatusTarget>(games);
    }
}
