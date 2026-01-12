using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Linux.Proton;

/// <summary>
/// Manages Wine prefix detection for non-Steam Windows games
/// </summary>
[SupportedOSPlatform("linux")]
public static class WinePrefixManager
{
    /// <summary>
    /// Detect the default Wine prefix
    /// </summary>
    /// <returns>Path to the default Wine prefix, or null if not found</returns>
    public static string? GetDefaultWinePrefix()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultPrefix = Path.Combine(home, ".wine");

        if (Directory.Exists(defaultPrefix) && IsValidWinePrefix(defaultPrefix))
        {
            return defaultPrefix;
        }

        return null;
    }

    /// <summary>
    /// Detect Wine prefix for a specific game installation path
    /// </summary>
    /// <param name="installPath">Game installation directory</param>
    /// <returns>Wine prefix path if found, otherwise null</returns>
    public static string? DetectWinePrefixForGame(string installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        // Check for prefix in parent directory (common Lutris pattern)
        var parentDir = Path.GetDirectoryName(installPath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            var parentPrefix = Path.Combine(parentDir, "prefix");
            if (Directory.Exists(parentPrefix) && IsValidWinePrefix(parentPrefix))
            {
                return parentPrefix;
            }
        }

        // Check for prefix in install directory itself
        var installPrefix = Path.Combine(installPath, "prefix");
        if (Directory.Exists(installPrefix) && IsValidWinePrefix(installPrefix))
        {
            return installPrefix;
        }

        // Check for .wine-prefix directory (another common pattern)
        if (!string.IsNullOrEmpty(parentDir))
        {
            var dotWinePrefix = Path.Combine(parentDir, ".wine-prefix");
            if (Directory.Exists(dotWinePrefix) && IsValidWinePrefix(dotWinePrefix))
            {
                return dotWinePrefix;
            }
        }

        // Fall back to default prefix
        return GetDefaultWinePrefix();
    }

    /// <summary>
    /// Check if a directory is a valid Wine prefix
    /// </summary>
    /// <param name="path">Directory path to check</param>
    /// <returns>True if the directory appears to be a valid Wine prefix</returns>
    public static bool IsValidWinePrefix(string path)
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        // A valid Wine prefix should have these directories
        var requiredDirs = new[]
        {
            Path.Combine(path, "drive_c"),
            Path.Combine(path, "dosdevices")
        };

        return requiredDirs.All(Directory.Exists);
    }

    /// <summary>
    /// Detect all Wine prefixes on the system
    /// </summary>
    /// <returns>List of detected Wine prefix paths</returns>
    public static List<string> DetectAllWinePrefixes()
    {
        var prefixes = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Check default prefix
        var defaultPrefix = GetDefaultWinePrefix();
        if (defaultPrefix != null)
        {
            prefixes.Add(defaultPrefix);
        }

        // Check common Lutris prefix locations
        var lutrisDir = Path.Combine(home, "Games", "lutris");
        if (Directory.Exists(lutrisDir))
        {
            try
            {
                var subdirs = Directory.GetDirectories(lutrisDir);
                foreach (var dir in subdirs)
                {
                    if (IsValidWinePrefix(dir))
                    {
                        prefixes.Add(dir);
                    }
                }
            }
            catch
            {
                // Continue if we can't read the directory
            }
        }

        // Check PlayOnLinux prefix locations
        var polDir = Path.Combine(home, ".PlayOnLinux", "wineprefix");
        if (Directory.Exists(polDir))
        {
            try
            {
                var subdirs = Directory.GetDirectories(polDir);
                foreach (var dir in subdirs)
                {
                    if (IsValidWinePrefix(dir))
                    {
                        prefixes.Add(dir);
                    }
                }
            }
            catch
            {
                // Continue if we can't read the directory
            }
        }

        // Check Bottles prefix locations
        var bottlesDir = Path.Combine(home, ".var", "app", "com.usebottles.bottles", "data", "bottles", "bottles");
        if (Directory.Exists(bottlesDir))
        {
            try
            {
                var subdirs = Directory.GetDirectories(bottlesDir);
                foreach (var dir in subdirs)
                {
                    if (IsValidWinePrefix(dir))
                    {
                        prefixes.Add(dir);
                    }
                }
            }
            catch
            {
                // Continue if we can't read the directory
            }
        }

        return prefixes.Distinct().ToList();
    }
}
