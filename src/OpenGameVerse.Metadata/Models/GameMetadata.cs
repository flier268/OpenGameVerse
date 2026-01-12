namespace OpenGameVerse.Metadata.Models;

/// <summary>
/// Represents enriched game metadata from IGDB.
/// </summary>
public sealed class GameMetadata
{
    public long IgdbId { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public double? Rating { get; set; }
    public int? RatingCount { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Themes { get; set; } = new();
    public string? CoverImageUrl { get; set; }
    public string? LocalCoverPath { get; set; }
    public string? IgdbUrl { get; set; }
}
