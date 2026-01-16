using OpenGameVerse.Core.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace OpenGameVerse.Metadata.Services;

/// <summary>
/// Service for caching and processing cover art images.
/// </summary>
public sealed class ImageCache
{
    private readonly string _cacheDirectory;
    private const int CoverWidth = 264; // Standard cover size
    private const int CoverHeight = 352;

    public ImageCache(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Caches an image, resizing and converting to WebP format.
    /// </summary>
    /// <param name="imageData">Raw image data</param>
    /// <param name="fileName">File name (without extension)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to cached image</returns>
    public async Task<Result<string>> CacheImageAsync(
        byte[] imageData,
        string fileName,
        CancellationToken ct = default
    )
    {
        try
        {
            var cachedPath = Path.Combine(_cacheDirectory, $"{fileName}.webp");

            // Check if already cached
            if (File.Exists(cachedPath))
            {
                return Result<string>.Success(cachedPath);
            }

            // Load, resize, and save as WebP
            using var image = Image.Load(imageData);

            image.Mutate(x =>
                x.Resize(
                    new ResizeOptions
                    {
                        Size = new Size(CoverWidth, CoverHeight),
                        Mode = ResizeMode.Max,
                    }
                )
            );

            var encoder = new WebpEncoder { Quality = 85, Method = WebpEncodingMethod.BestQuality };

            await image.SaveAsync(cachedPath, encoder, ct);

            return Result<string>.Success(cachedPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to cache image: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the path to a cached image if it exists.
    /// </summary>
    /// <param name="fileName">File name (without extension)</param>
    /// <returns>Path to cached image, or null if not found</returns>
    public string? GetCachedImagePath(string fileName)
    {
        var cachedPath = Path.Combine(_cacheDirectory, $"{fileName}.webp");
        return File.Exists(cachedPath) ? cachedPath : null;
    }

    /// <summary>
    /// Clears the entire cache directory.
    /// </summary>
    public Result ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, recursive: true);
                Directory.CreateDirectory(_cacheDirectory);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to clear cache: {ex.Message}");
        }
    }
}
