using OpenGameVerse.Console.DependencyInjection;
using OpenGameVerse.Core.Abstractions;

namespace OpenGameVerse.Console.Commands;

/// <summary>
/// List command to display games in library
/// </summary>
public sealed class ListCommand
{
    private readonly ServiceContainer _services;

    public ListCommand(ServiceContainer services)
    {
        _services = services;
    }

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        var repository = _services.Resolve<IGameRepository>();

        System.Console.WriteLine("OpenGameVerse - Game Library");
        System.Console.WriteLine("====================================");
        System.Console.WriteLine();

        var countResult = await repository.GetGameCountAsync(ct);
        if (!countResult.IsSuccess)
        {
            System.Console.WriteLine($"Error: {countResult.Error}");
            return 1;
        }

        System.Console.WriteLine($"Total games: {countResult.Value}");
        System.Console.WriteLine();

        if (countResult.Value == 0)
        {
            System.Console.WriteLine("No games found. Run 'scan' command first.");
            return 0;
        }

        int count = 0;
        await foreach (var game in repository.GetAllGamesAsync(ct))
        {
            count++;
            System.Console.WriteLine($"{count}. {game.Title}");
            System.Console.WriteLine($"   Platform: {game.Platform}");
            System.Console.WriteLine($"   Path: {game.InstallPath}");
            if (game.SizeBytes > 0)
            {
                var sizeMB = game.SizeBytes / (1024.0 * 1024.0);
                System.Console.WriteLine($"   Size: {sizeMB:F2} MB");
            }
            System.Console.WriteLine();
        }

        return 0;
    }
}
