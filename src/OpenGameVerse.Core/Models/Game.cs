namespace OpenGameVerse.Core.Models;

/// <summary>
/// Represents a game in the library
/// </summary>
public sealed class Game
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public string? NormalizedTitle { get; set; }
    public required string InstallPath { get; set; }
    public required string Platform { get; set; } // Steam, Epic, GOG, etc.
    public string? PlatformId { get; set; }
    public string? ExecutablePath { get; set; }
    public string? IconPath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? LastPlayed { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Metadata (Phase 3)
    public string? IgdbId { get; set; }
    public string? CoverImagePath { get; set; }

    // User organization (Phase 6)
    public bool IsFavorite { get; set; }
    public string? CustomCategory { get; set; } // Null = no category, otherwise user-defined category name
    public int SortOrder { get; set; } // For custom ordering within categories
}
