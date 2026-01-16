using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Metadata.Models;

namespace OpenGameVerse.Metadata.Abstractions;

/// <summary>
/// Interface for metadata enrichment service.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Enriches a game with metadata from IGDB.
    /// </summary>
    /// <param name="game">Game to enrich</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Enriched metadata</returns>
    Task<Result<GameMetadata?>> EnrichGameAsync(Game game, CancellationToken ct = default);

    /// <summary>
    /// Downloads and caches cover art for a game.
    /// </summary>
    /// <param name="metadata">Game metadata with cover URL</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Local path to cached cover image</returns>
    Task<Result<string>> DownloadCoverArtAsync(
        GameMetadata metadata,
        CancellationToken ct = default
    );
}
