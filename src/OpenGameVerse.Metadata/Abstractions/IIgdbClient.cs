using OpenGameVerse.Core.Common;
using OpenGameVerse.Metadata.Models;

namespace OpenGameVerse.Metadata.Abstractions;

/// <summary>
/// Interface for IGDB API client.
/// </summary>
public interface IIgdbClient
{
    /// <summary>
    /// Searches for games by title.
    /// </summary>
    /// <param name="query">Search query (game title)</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching games</returns>
    Task<Result<IgdbGame[]>> SearchGamesAsync(
        string query,
        int limit = 10,
        CancellationToken ct = default
    );

    /// <summary>
    /// Gets detailed information about a game by ID.
    /// </summary>
    /// <param name="gameId">IGDB game ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Game details</returns>
    Task<Result<IgdbGame?>> GetGameByIdAsync(long gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets cover information for a game.
    /// </summary>
    /// <param name="coverId">IGDB cover ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cover details</returns>
    Task<Result<IgdbCover?>> GetCoverAsync(long coverId, CancellationToken ct = default);

    /// <summary>
    /// Downloads a cover image.
    /// </summary>
    /// <param name="imageUrl">Image URL</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Image data as byte array</returns>
    Task<Result<byte[]>> DownloadImageAsync(string imageUrl, CancellationToken ct = default);
}
