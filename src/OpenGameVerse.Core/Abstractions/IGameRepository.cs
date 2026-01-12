using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Abstractions;

/// <summary>
/// Data access for game library management
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Add a new game to the database
    /// </summary>
    Task<Result<long>> AddGameAsync(Game game, CancellationToken ct);

    /// <summary>
    /// Update an existing game
    /// </summary>
    Task<Result> UpdateGameAsync(Game game, CancellationToken ct);

    /// <summary>
    /// Get a game by its ID
    /// </summary>
    Task<Result<Game?>> GetGameByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// Get a game by its installation path (for deduplication)
    /// </summary>
    Task<Result<Game?>> GetGameByPathAsync(string installPath, CancellationToken ct);

    /// <summary>
    /// Get all games as an async stream
    /// </summary>
    IAsyncEnumerable<Game> GetAllGamesAsync(CancellationToken ct);

    /// <summary>
    /// Get total count of games in the library
    /// </summary>
    Task<Result<int>> GetGameCountAsync(CancellationToken ct);

    /// <summary>
    /// Delete a game by ID
    /// </summary>
    Task<Result> DeleteGameAsync(long id, CancellationToken ct);
}
