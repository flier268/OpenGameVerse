using Dapper;
using Microsoft.Data.Sqlite;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Common;
using OpenGameVerse.Core.Models;
using System.Runtime.CompilerServices;

namespace OpenGameVerse.Data.Repositories;

/// <summary>
/// Game repository with Dapper.AOT for compile-time SQL mapping
/// </summary>
public sealed class GameRepository : IGameRepository
{
    private readonly string _connectionString;

    public GameRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    [DapperAot]
    public async Task<Result<long>> AddGameAsync(Game game, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = """
                INSERT INTO games (
                    title, normalized_title, install_path, platform,
                    executable_path, icon_path, size_bytes,
                    discovered_at, updated_at, igdb_id, cover_image_path
                ) VALUES (
                    @Title, @NormalizedTitle, @InstallPath, @Platform,
                    @ExecutablePath, @IconPath, @SizeBytes,
                    unixepoch(), unixepoch(), @IgdbId, @CoverImagePath
                )
                RETURNING id;
                """;

            var id = await connection.ExecuteScalarAsync<long>(sql, new
            {
                game.Title,
                NormalizedTitle = game.Title.ToLowerInvariant(),
                game.InstallPath,
                game.Platform,
                game.ExecutablePath,
                game.IconPath,
                game.SizeBytes,
                game.IgdbId,
                game.CoverImagePath
            });

            return Result<long>.Success(id);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Failed to add game: {ex.Message}");
        }
    }

    [DapperAot]
    public async Task<Result> UpdateGameAsync(Game game, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = """
                UPDATE games SET
                    title = @Title,
                    normalized_title = @NormalizedTitle,
                    install_path = @InstallPath,
                    platform = @Platform,
                    executable_path = @ExecutablePath,
                    icon_path = @IconPath,
                    size_bytes = @SizeBytes,
                    last_played = @LastPlayed,
                    updated_at = unixepoch(),
                    igdb_id = @IgdbId,
                    cover_image_path = @CoverImagePath
                WHERE id = @Id;
                """;

            await connection.ExecuteAsync(sql, new
            {
                game.Id,
                game.Title,
                NormalizedTitle = game.Title.ToLowerInvariant(),
                game.InstallPath,
                game.Platform,
                game.ExecutablePath,
                game.IconPath,
                game.SizeBytes,
                LastPlayed = game.LastPlayed.HasValue
                    ? new DateTimeOffset(game.LastPlayed.Value).ToUnixTimeSeconds()
                    : (long?)null,
                game.IgdbId,
                game.CoverImagePath
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update game: {ex.Message}");
        }
    }

    [DapperAot]
    public async Task<Result<Game?>> GetGameByIdAsync(long id, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = "SELECT * FROM games WHERE id = @Id;";
            var game = await connection.QueryFirstOrDefaultAsync<Game>(sql, new { Id = id });

            return Result<Game?>.Success(game);
        }
        catch (Exception ex)
        {
            return Result<Game?>.Failure($"Failed to get game: {ex.Message}");
        }
    }

    [DapperAot]
    public async Task<Result<Game?>> GetGameByPathAsync(string installPath, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = "SELECT * FROM games WHERE install_path = @InstallPath;";
            var game = await connection.QueryFirstOrDefaultAsync<Game>(sql, new { InstallPath = installPath });

            return Result<Game?>.Success(game);
        }
        catch (Exception ex)
        {
            return Result<Game?>.Failure($"Failed to get game by path: {ex.Message}");
        }
    }

    [DapperAot]
    public async IAsyncEnumerable<Game> GetAllGamesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        const string sql = "SELECT * FROM games ORDER BY title;";

        await foreach (var game in connection.QueryUnbufferedAsync<Game>(sql).WithCancellation(ct))
        {
            yield return game;
        }
    }

    [DapperAot]
    public async Task<Result<int>> GetGameCountAsync(CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = "SELECT COUNT(*) FROM games;";
            var count = await connection.ExecuteScalarAsync<int>(sql);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to get game count: {ex.Message}");
        }
    }

    [DapperAot]
    public async Task<Result> DeleteGameAsync(long id, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = "DELETE FROM games WHERE id = @Id;";
            await connection.ExecuteAsync(sql, new { Id = id });

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete game: {ex.Message}");
        }
    }
}
