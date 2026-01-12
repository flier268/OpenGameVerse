namespace OpenGameVerse.Core.Models;

/// <summary>
/// Represents a game installation discovered during scanning
/// (temporary model used during scanning, before persisting to database)
/// </summary>
public sealed class GameInstallation
{
    public required string Title { get; set; }
    public required string InstallPath { get; set; }
    public required string Platform { get; set; } // Steam, Epic, GOG
    public string? PlatformId { get; set; } // App ID, catalog item ID, etc.
    public string? ExecutablePath { get; set; }
    public string? IconPath { get; set; }
    public long SizeBytes { get; set; }
}
