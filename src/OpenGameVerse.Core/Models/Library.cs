namespace OpenGameVerse.Core.Models;

/// <summary>
/// Represents a game library folder (e.g., Steam library folder)
/// </summary>
public sealed class Library
{
    public long Id { get; set; }
    public required string Platform { get; set; } // Steam, Epic, GOG
    public required string LibraryPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DiscoveredAt { get; set; }
}
