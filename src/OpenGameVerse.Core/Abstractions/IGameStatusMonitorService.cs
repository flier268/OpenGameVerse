using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Abstractions;

public interface IGameStatusMonitorService : IDisposable
{
    event Action<long, bool>? GameStatusChanged;

    void UpdateTrackedGames(IEnumerable<GameStatusTarget> games);

    void Start();
    void Stop();

    Task<int> StopGameAsync(GameStatusTarget game, CancellationToken ct);
    Task WaitForGameExitAsync(long gameId, CancellationToken ct);
}
