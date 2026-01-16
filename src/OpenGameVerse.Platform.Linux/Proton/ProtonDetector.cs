using System.Runtime.Versioning;

namespace OpenGameVerse.Platform.Linux.Proton;

/// <summary>
/// Detects installed Proton compatibility tools
/// </summary>
/// <remarks>
/// This is informational only - Steam handles Proton launching automatically via steam:// protocol
/// </remarks>
[SupportedOSPlatform("linux")]
public static class ProtonDetector
{
    /// <summary>
    /// Detect all installed Proton versions
    /// </summary>
    /// <returns>List of Proton installation paths</returns>
    public static List<string> DetectProtonInstallations()
    {
        var protonPaths = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Check Steam compatibility tools directories
        var compatToolsDirs = new[]
        {
            Path.Combine(home, ".steam", "steam", "compatibilitytools.d"),
            Path.Combine(home, ".local", "share", "Steam", "compatibilitytools.d"),
            Path.Combine(
                home,
                ".var",
                "app",
                "com.valvesoftware.Steam",
                ".steam",
                "steam",
                "compatibilitytools.d"
            ),
            Path.Combine(
                home,
                ".var",
                "app",
                "com.valvesoftware.Steam",
                ".local",
                "share",
                "Steam",
                "compatibilitytools.d"
            ),
        };

        foreach (var compatDir in compatToolsDirs)
        {
            if (!Directory.Exists(compatDir))
            {
                continue;
            }

            try
            {
                var subdirs = Directory.GetDirectories(compatDir);
                foreach (var dir in subdirs)
                {
                    // Check if this looks like a Proton installation
                    if (
                        Path.GetFileName(dir).Contains("Proton", StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileName(dir)
                            .Contains("GE-Proton", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        protonPaths.Add(dir);
                    }
                }
            }
            catch
            {
                // Continue if we can't read this directory
            }
        }

        // Check Steam library common directories for official Proton installations
        var steamLibraryPaths = GetSteamLibraryPaths();
        foreach (var libraryPath in steamLibraryPaths)
        {
            var commonDir = Path.Combine(libraryPath, "steamapps", "common");
            if (!Directory.Exists(commonDir))
            {
                continue;
            }

            try
            {
                var subdirs = Directory.GetDirectories(commonDir, "Proton*");
                protonPaths.AddRange(subdirs);
            }
            catch
            {
                // Continue if we can't read this directory
            }
        }

        return protonPaths.Distinct().ToList();
    }

    /// <summary>
    /// Check if Proton is available on the system
    /// </summary>
    public static bool IsProtonAvailable()
    {
        return DetectProtonInstallations().Count > 0;
    }

    private static List<string> GetSteamLibraryPaths()
    {
        var libraries = new List<string>();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var steamPaths = new[]
        {
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),
            Path.Combine(
                home,
                ".var",
                "app",
                "com.valvesoftware.Steam",
                ".local",
                "share",
                "Steam"
            ),
        };

        foreach (var steamPath in steamPaths)
        {
            var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFoldersPath))
            {
                libraries.Add(steamPath);
                // Could parse libraryfolders.vdf here for additional libraries
                // For simplicity, we just add the main Steam path
                break;
            }
        }

        return libraries;
    }
}
