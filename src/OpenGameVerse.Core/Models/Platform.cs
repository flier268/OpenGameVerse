namespace OpenGameVerse.Core.Models;

/// <summary>
/// Represents a game platform/launcher (Steam, Epic, GOG, etc.)
/// </summary>
public sealed class Platform
{
    public long Id { get; set; }
    public required string Name { get; set; } // Steam, Epic, GOG
    public required string DisplayName { get; set; }
    public string? InstallPath { get; set; }
    public bool IsInstalled { get; set; }
    public DateTime? LastScan { get; set; }
}
