using OpenGameVerse.Console.DependencyInjection;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Console.Commands;

/// <summary>
/// Scan command to discover games
/// </summary>
public sealed class ScanCommand
{
    private readonly ServiceContainer _services;

    public ScanCommand(ServiceContainer services)
    {
        _services = services;
    }

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        System.Console.WriteLine("OpenGameVerse - Game Library Scanner");
        System.Console.WriteLine("====================================");
        System.Console.WriteLine();

        var platformHost = _services.Resolve<IPlatformHost>();
        var repository = _services.Resolve<IGameRepository>();

        System.Console.WriteLine($"Platform: {platformHost.Platform}");
        System.Console.WriteLine();

        var scanners = platformHost.GetScanners().ToList();
        System.Console.WriteLine($"Found {scanners.Count} scanner(s)");
        System.Console.WriteLine();

        int totalGamesFound = 0;
        int totalGamesAdded = 0;

        foreach (var scanner in scanners)
        {
            System.Console.Write($"Checking {scanner.DisplayName}... ");

            var isInstalled = await scanner.IsInstalledAsync(ct);
            if (!isInstalled)
            {
                System.Console.WriteLine("Not installed");
                continue;
            }

            System.Console.WriteLine("Installed");
            System.Console.WriteLine($"  Scanning for games...");

            int gamesFound = 0;
            int gamesAdded = 0;

            await foreach (var game in scanner.ScanAsync(ct))
            {
                gamesFound++;
                totalGamesFound++;

                // Check if game already exists
                var existingResult = await repository.GetGameByPathAsync(game.InstallPath, ct);
                if (existingResult.IsSuccess && existingResult.Value != null)
                {
                    System.Console.WriteLine($"  - {game.Title} (already in library)");
                    continue;
                }

                // Add new game
                var dbGame = new Game
                {
                    Title = game.Title,
                    NormalizedTitle = game.Title.ToLowerInvariant(),
                    InstallPath = game.InstallPath,
                    Platform = game.Platform,
                    ExecutablePath = game.ExecutablePath,
                    IconPath = game.IconPath,
                    CoverImagePath = game.CoverImagePath,
                    SizeBytes = game.SizeBytes,
                    DiscoveredAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                var result = await repository.AddGameAsync(dbGame, ct);
                if (result.IsSuccess)
                {
                    gamesAdded++;
                    totalGamesAdded++;
                    System.Console.WriteLine($"  + {game.Title}");
                }
                else
                {
                    System.Console.WriteLine($"  ! Failed to add {game.Title}: {result.Error}");
                }
            }

            System.Console.WriteLine(
                $"  Found {gamesFound} game(s), added {gamesAdded} new game(s)"
            );
            System.Console.WriteLine();
        }

        System.Console.WriteLine("====================================");
        System.Console.WriteLine(
            $"Total: Found {totalGamesFound} game(s), added {totalGamesAdded} new game(s)"
        );

        return 0;
    }
}
