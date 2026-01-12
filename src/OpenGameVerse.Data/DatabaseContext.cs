using Microsoft.Data.Sqlite;
using System.Reflection;

namespace OpenGameVerse.Data;

/// <summary>
/// Database context for SQLite initialization and migrations
/// </summary>
public sealed class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Initialize the database and run migrations
    /// </summary>
    public void Initialize()
    {
        EnsureDatabaseExists();
        RunMigrations();
    }

    private void EnsureDatabaseExists()
    {
        // Extract the database file path from connection string
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dbPath = builder.DataSource;

        if (dbPath == ":memory:")
        {
            return; // In-memory database, no file to create
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create database file if it doesn't exist
        if (!File.Exists(dbPath))
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            // File is created on open
        }
    }

    private void RunMigrations()
    {
        // Read embedded migration script
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "OpenGameVerse.Data.Migrations.InitialSchema.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Migration script not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        var migrationSql = reader.ReadToEnd();

        // Execute migration
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = migrationSql;
        command.ExecuteNonQuery();
    }
}
