namespace OpenGameVerse.Platform.Linux.Models;

/// <summary>
/// Represents a parsed .desktop file entry
/// </summary>
public sealed class DesktopEntry
{
    /// <summary>
    /// Full path to the .desktop file
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Display name of the application
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Executable command (with field codes cleaned)
    /// </summary>
    public string? Exec { get; init; }

    /// <summary>
    /// Icon name or path
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Working directory for the executable
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Semicolon-separated list of categories
    /// </summary>
    public string? Categories { get; init; }

    /// <summary>
    /// Whether this entry is categorized as a game
    /// </summary>
    public bool IsGame { get; init; }

    /// <summary>
    /// Tooltip/description text
    /// </summary>
    public string? Comment { get; init; }
}
