using System.Runtime.Versioning;
using OpenGameVerse.Platform.Linux.Models;

namespace OpenGameVerse.Platform.Linux.Parsers;

/// <summary>
/// Parser for .desktop files (freedesktop.org Desktop Entry Specification)
/// </summary>
[SupportedOSPlatform("linux")]
public static class DesktopFileParser
{
    /// <summary>
    /// Parse a .desktop file and extract game-relevant information
    /// </summary>
    /// <param name="filePath">Path to the .desktop file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>DesktopEntry if successfully parsed, null if invalid or missing required fields</returns>
    public static async Task<DesktopEntry?> ParseAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, ct);

            bool inDesktopEntry = false;
            string? name = null;
            string? exec = null;
            string? icon = null;
            string? path = null;
            string? categories = null;
            string? comment = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                // Check for section headers
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    inDesktopEntry = trimmed.Equals(
                        "[Desktop Entry]",
                        StringComparison.OrdinalIgnoreCase
                    );
                    continue;
                }

                // Only parse lines within [Desktop Entry] section
                if (!inDesktopEntry)
                {
                    continue;
                }

                // Parse key=value pairs
                var equalIndex = trimmed.IndexOf('=');
                if (equalIndex < 0)
                {
                    continue;
                }

                var key = trimmed[..equalIndex].Trim();
                var value = trimmed[(equalIndex + 1)..].Trim();

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                switch (key)
                {
                    case "Name":
                        name = value;
                        break;
                    case "Exec":
                        exec = CleanExecValue(value);
                        break;
                    case "Icon":
                        icon = value;
                        break;
                    case "Path":
                        path = value;
                        break;
                    case "Categories":
                        categories = value;
                        break;
                    case "Comment":
                        comment = value;
                        break;
                }
            }

            // Name and Exec are required
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(exec))
            {
                return null;
            }

            // Check if this is a game
            bool isGame = categories?.Contains("Game", StringComparison.OrdinalIgnoreCase) ?? false;

            return new DesktopEntry
            {
                FilePath = filePath,
                Name = name,
                Exec = exec,
                Icon = icon,
                Path = path,
                Categories = categories,
                IsGame = isGame,
                Comment = comment,
            };
        }
        catch
        {
            // Return null for any parsing errors
            return null;
        }
    }

    /// <summary>
    /// Clean the Exec field by removing field codes (%U, %F, %k, etc.) and extracting the executable path
    /// </summary>
    private static string CleanExecValue(string exec)
    {
        if (string.IsNullOrWhiteSpace(exec))
        {
            return string.Empty;
        }

        // Common field codes to remove (from freedesktop.org spec)
        string[] fieldCodes =
        {
            "%f",
            "%F",
            "%u",
            "%U",
            "%d",
            "%D",
            "%n",
            "%N",
            "%i",
            "%c",
            "%k",
            "%v",
            "%m",
        };

        var cleaned = exec;

        // Remove all field codes
        foreach (var code in fieldCodes)
        {
            cleaned = cleaned.Replace(code, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        // Handle quoted paths (remove surrounding quotes)
        cleaned = cleaned.Trim();
        if (cleaned.StartsWith('"') && cleaned.Contains('"', StringComparison.Ordinal))
        {
            var endQuoteIndex = cleaned.IndexOf('"', 1);
            if (endQuoteIndex > 0)
            {
                cleaned = cleaned.Substring(1, endQuoteIndex - 1);
            }
        }

        // Remove trailing arguments (everything after first unquoted space)
        var spaceIndex = cleaned.IndexOf(' ');
        if (spaceIndex > 0)
        {
            cleaned = cleaned[..spaceIndex];
        }

        return cleaned.Trim();
    }
}
