using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Metadata.Abstractions;
using OpenGameVerse.Metadata.Models;

namespace OpenGameVerse.Metadata.Services;

/// <summary>
/// Service for enriching games with metadata from IGDB.
/// </summary>
public sealed class MetadataService : IMetadataService
{
    private readonly IIgdbClient _igdbClient;
    private readonly ImageCache _imageCache;

    public MetadataService(IIgdbClient igdbClient, ImageCache imageCache)
    {
        _igdbClient = igdbClient ?? throw new ArgumentNullException(nameof(igdbClient));
        _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
    }

    public async Task<Result<GameMetadata?>> EnrichGameAsync(Game game, CancellationToken ct = default)
    {
        try
        {
            // If game already has IGDB ID, fetch directly
            if (!string.IsNullOrEmpty(game.IgdbId) && long.TryParse(game.IgdbId, out var igdbId))
            {
                var directResult = await _igdbClient.GetGameByIdAsync(igdbId, ct);
                if (directResult.IsSuccess && directResult.Value != null)
                {
                    var directMetadata = MapToMetadata(directResult.Value);
                    await PopulateCoverAsync(directResult.Value, directMetadata, ct);
                    return Result<GameMetadata?>.Success(directMetadata);
                }
            }

            // Search by title
            var searchResult = await _igdbClient.SearchGamesAsync(game.Title, limit: 5, ct);

            if (!searchResult.IsSuccess || searchResult.Value == null || searchResult.Value.Length == 0)
            {
                return Result<GameMetadata?>.Success(null);
            }

            // Take best match (first result)
            var bestMatch = searchResult.Value[0];

            var metadata = MapToMetadata(bestMatch);

            await PopulateCoverAsync(bestMatch, metadata, ct);

            return Result<GameMetadata?>.Success(metadata);
        }
        catch (Exception ex)
        {
            return Result<GameMetadata?>.Failure($"Failed to enrich game: {ex.Message}");
        }
    }

    public async Task<Result<string>> DownloadCoverArtAsync(GameMetadata metadata, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(metadata.CoverImageUrl))
            {
                return Result<string>.Failure("No cover URL available");
            }

            // Check if already cached
            var fileName = $"cover_{metadata.IgdbId}";
            var cachedPath = _imageCache.GetCachedImagePath(fileName);

            if (cachedPath != null)
            {
                return Result<string>.Success(cachedPath);
            }

            // Download image
            var downloadResult = await _igdbClient.DownloadImageAsync(metadata.CoverImageUrl, ct);

            if (!downloadResult.IsSuccess)
            {
                return Result<string>.Failure(downloadResult.Error ?? "Failed to download image");
            }

            // Cache and process
            var cacheResult = await _imageCache.CacheImageAsync(downloadResult.Value!, fileName, ct);

            if (!cacheResult.IsSuccess)
            {
                return Result<string>.Failure(cacheResult.Error ?? "Failed to cache image");
            }

            metadata.LocalCoverPath = cacheResult.Value;

            return cacheResult;
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to download cover art: {ex.Message}");
        }
    }

    private static GameMetadata MapToMetadata(IgdbGame igdbGame)
    {
        var metadata = new GameMetadata
        {
            IgdbId = igdbGame.Id,
            Title = igdbGame.Name ?? "Unknown",
            Summary = igdbGame.Summary,
            Rating = igdbGame.Rating,
            RatingCount = igdbGame.RatingCount,
            IgdbUrl = igdbGame.Url
        };

        if (igdbGame.FirstReleaseDate.HasValue)
        {
            metadata.ReleaseDate = DateTimeOffset.FromUnixTimeSeconds(igdbGame.FirstReleaseDate.Value).DateTime;
        }

        return metadata;
    }

    private async Task PopulateCoverAsync(IgdbGame igdbGame, GameMetadata metadata, CancellationToken ct)
    {
        if (!igdbGame.Cover.HasValue)
        {
            return;
        }

        var coverResult = await _igdbClient.GetCoverAsync(igdbGame.Cover.Value, ct);
        if (coverResult.IsSuccess && coverResult.Value != null)
        {
            metadata.CoverImageUrl = coverResult.Value.GetImageUrl("cover_big");
        }
    }
}
