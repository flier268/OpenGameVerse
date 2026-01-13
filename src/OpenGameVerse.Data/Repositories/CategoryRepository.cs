using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using OpenGameVerse.Core.Abstractions;

namespace OpenGameVerse.Data.Repositories;

/// <summary>
/// Data access for game category management
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public CategoryRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Get all categories with game counts
    /// </summary>
    public async IAsyncEnumerable<(string Name, int GameCount)> GetAllCategoriesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Get all categories from the categories table
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c.name, COUNT(DISTINCT g.id) as game_count
            FROM categories c
            LEFT JOIN games g ON (g.custom_category = c.name OR (c.name = 'Uncategorized' AND g.custom_category IS NULL))
            GROUP BY c.name
            ORDER BY c.name
        ";

        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var name = reader.GetString(0);
            var gameCount = reader.GetInt32(1);
            yield return (name, gameCount);
        }
    }

    /// <summary>
    /// Add a new category
    /// </summary>
    public async Task<bool> AddCategoryAsync(string categoryName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return false;
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO categories (name, created_at, updated_at)
            VALUES (@name, unixepoch(), unixepoch())
        ";
        command.Parameters.AddWithValue("@name", categoryName);

        try
        {
            var result = await command.ExecuteNonQueryAsync(ct);
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Delete a category by name
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(string categoryName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoryName) || categoryName == "Uncategorized")
        {
            return false;
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM categories WHERE name = @name";
        command.Parameters.AddWithValue("@name", categoryName);

        try
        {
            var result = await command.ExecuteNonQueryAsync(ct);
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a category exists
    /// </summary>
    public async Task<bool> CategoryExistsAsync(string categoryName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return false;
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM categories WHERE name = @name";
        command.Parameters.AddWithValue("@name", categoryName);

        var result = await command.ExecuteScalarAsync(ct);
        return result is long count && count > 0;
    }
}
