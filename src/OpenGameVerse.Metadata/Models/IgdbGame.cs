namespace OpenGameVerse.Metadata.Models;

/// <summary>
/// Represents a game from the IGDB API.
/// </summary>
public sealed class IgdbGame
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Summary { get; set; }
    public long? Cover { get; set; }
    public long? FirstReleaseDate { get; set; }
    public double? Rating { get; set; }
    public int? RatingCount { get; set; }
    public long[]? Genres { get; set; }
    public long[]? Themes { get; set; }
    public long[]? Platforms { get; set; }
    public string? Url { get; set; }
}
