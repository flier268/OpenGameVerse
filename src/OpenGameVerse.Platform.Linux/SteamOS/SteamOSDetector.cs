using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Linux.SteamOS;

/// <summary>
/// Detects SteamOS and Steam Deck hardware
/// </summary>
[SupportedOSPlatform("linux")]
public static class SteamOSDetector
{
    /// <summary>
    /// Check if the current system is running SteamOS
    /// </summary>
    /// <returns>True if running on SteamOS</returns>
    public static bool IsSteamOS()
    {
        try
        {
            // Check /etc/os-release for SteamOS identifier
            if (File.Exists("/etc/os-release"))
            {
                var content = File.ReadAllText("/etc/os-release");

                // Check for SteamOS ID
                if (content.Contains("ID=steamos", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("ID_LIKE=steamos", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("NAME=\"SteamOS\"", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't read the file, assume not SteamOS
        }

        return false;
    }

    /// <summary>
    /// Check if the current hardware is a Steam Deck
    /// </summary>
    /// <returns>True if running on Steam Deck hardware</returns>
    public static bool IsSteamDeck()
    {
        try
        {
            // Check DMI product name for Steam Deck identifiers
            // Jupiter = Original Steam Deck
            // Galileo = Steam Deck OLED
            if (File.Exists("/sys/devices/virtual/dmi/id/product_name"))
            {
                var productName = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name").Trim();

                if (productName.Contains("Jupiter", StringComparison.OrdinalIgnoreCase) ||
                    productName.Contains("Galileo", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Alternative check via board name
            if (File.Exists("/sys/devices/virtual/dmi/id/board_name"))
            {
                var boardName = File.ReadAllText("/sys/devices/virtual/dmi/id/board_name").Trim();

                if (boardName.Contains("Jupiter", StringComparison.OrdinalIgnoreCase) ||
                    boardName.Contains("Galileo", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't read the files, assume not Steam Deck
        }

        return false;
    }

    /// <summary>
    /// Get the Steam Deck variant if running on Steam Deck
    /// </summary>
    /// <returns>Variant name (Jupiter, Galileo) or null if not a Steam Deck</returns>
    public static string? GetSteamDeckVariant()
    {
        if (!IsSteamDeck())
        {
            return null;
        }

        try
        {
            if (File.Exists("/sys/devices/virtual/dmi/id/product_name"))
            {
                var productName = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name").Trim();

                if (productName.Contains("Galileo", StringComparison.OrdinalIgnoreCase))
                {
                    return "Galileo (OLED)";
                }

                if (productName.Contains("Jupiter", StringComparison.OrdinalIgnoreCase))
                {
                    return "Jupiter";
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return "Unknown";
    }

    /// <summary>
    /// Check if the system is in Steam Deck Gaming Mode
    /// </summary>
    /// <returns>True if in Gaming Mode (Big Picture mode)</returns>
    public static bool IsGamingMode()
    {
        try
        {
            // Check if gamescope-session is running (Gaming Mode compositor)
            var processes = System.Diagnostics.Process.GetProcessesByName("gamescope-session");
            if (processes.Length > 0)
            {
                foreach (var p in processes)
                {
                    p.Dispose();
                }
                return true;
            }
        }
        catch
        {
            // If we can't check processes, assume not in Gaming Mode
        }

        return false;
    }

    /// <summary>
    /// Get recommended paths for Steam Deck
    /// </summary>
    /// <returns>Array of recommended Steam paths in priority order</returns>
    public static string[] GetSteamDeckSteamPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new[]
        {
            // Flatpak Steam (default on Steam Deck)
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),

            // Standard Steam installations (fallback)
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".steam", "steam")
        };
    }
}
