using System.Collections.ObjectModel;
using OpenGameVerse.App.ViewModels;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.Services;

public sealed class GameStatusMonitorCoordinator : IDisposable
{
    private readonly IGameStatusMonitorService _platformMonitor;
    private readonly Lock _lock = new();
    private readonly Dictionary<long, List<WeakReference<GameViewModel>>> _trackedGames = new();
    private readonly SynchronizationContext? _uiContext;

    public GameStatusMonitorCoordinator(
        IGameStatusMonitorService platformMonitor,
        SynchronizationContext? uiContext = null)
    {
        _platformMonitor = platformMonitor ?? throw new ArgumentNullException(nameof(platformMonitor));
        _uiContext = uiContext;
        _platformMonitor.GameStatusChanged += OnGameStatusChanged;
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

            _platformMonitor.UpdateTrackedGames(CollectLiveTargets());
        }
    }

    public void Start()
    {
        _platformMonitor.Start();
    }

    public void Stop()
    {
        _platformMonitor.Stop();
    }

    public void Dispose()
    {
        _platformMonitor.GameStatusChanged -= OnGameStatusChanged;
        _platformMonitor.Dispose();
    }

    public Task<int> StopGameAsync(GameViewModel game, CancellationToken ct)
    {
        var target = game ?? throw new ArgumentNullException(nameof(game));
        return _platformMonitor.StopGameAsync(ToTarget(target), ct);
    }

    public Task WaitForGameExitAsync(long gameId, CancellationToken ct)
    {
        return _platformMonitor.WaitForGameExitAsync(gameId, ct);
    }

    private void OnGameStatusChanged(long id, bool isRunning)
    {
        if (_uiContext is null)
        {
            UpdateViewModels(id, isRunning);
            return;
        }

        _uiContext.Post(_ => UpdateViewModels(id, isRunning), null);
    }

    private void UpdateViewModels(long id, bool isRunning)
    {
        lock (_lock)
        {
            if (!_trackedGames.TryGetValue(id, out var list))
            {
                return;
            }

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].TryGetTarget(out var target))
                {
                    target.IsRunning = isRunning;
                }
                else
                {
                    list.RemoveAt(i);
                }
            }

            if (list.Count == 0)
            {
                _trackedGames.Remove(id);
            }
        }
    }

    private ReadOnlyCollection<GameStatusTarget> CollectLiveTargets()
    {
        var targets = new Dictionary<long, GameStatusTarget>();
        var toRemove = new List<long>();

        foreach (var (id, list) in _trackedGames)
        {
            var hasLive = false;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].TryGetTarget(out var target))
                {
                    hasLive = true;
                    targets.TryAdd(id, ToTarget(target));
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

        return new ReadOnlyCollection<GameStatusTarget>(targets.Values.ToList());
    }

    private static GameStatusTarget ToTarget(GameViewModel game)
    {
        return new GameStatusTarget(
            game.Id,
            game.Title,
            game.ExecutablePath,
            game.InstallPath,
            game.PlatformId);
    }
}
