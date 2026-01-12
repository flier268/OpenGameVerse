namespace OpenGameVerse.Metadata.Models;

/// <summary>
/// Represents a cover image from the IGDB API.
/// </summary>
public sealed class IgdbCover
{
    public long Id { get; set; }
    public long Game { get; set; }
    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// Gets the full URL for the cover image at the specified size.
    /// </summary>
    /// <param name="size">Size format (e.g., "cover_big", "1080p", "720p")</param>
    /// <returns>Full image URL</returns>
    public string GetImageUrl(string size = "cover_big")
    {
        if (string.IsNullOrEmpty(ImageId))
            return string.Empty;

        return $"https://images.igdb.com/igdb/image/upload/t_{size}/{ImageId}.jpg";
    }
}
