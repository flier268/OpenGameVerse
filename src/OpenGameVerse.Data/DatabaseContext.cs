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
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Get current schema version
        int currentVersion = GetSchemaVersion(connection);

        // List of migrations to apply
        var migrations = new[]
        {
            (version: 1, script: "OpenGameVerse.Data.Migrations.InitialSchema.sql"),
            (version: 2, script: "OpenGameVerse.Data.Migrations.002_AddUserOrganization.sql"),
            (version: 3, script: "OpenGameVerse.Data.Migrations.003_AddCategoriesTable.sql"),
            (version: 4, script: "OpenGameVerse.Data.Migrations.004_AddPlatformId.sql")
        };

        var assembly = Assembly.GetExecutingAssembly();

        foreach (var migration in migrations)
        {
            if (migration.version <= currentVersion)
            {
                continue; // Already applied
            }

            using var stream = assembly.GetManifestResourceStream(migration.script);
            if (stream == null)
            {
                throw new InvalidOperationException($"Migration script not found: {migration.script}");
            }

            using var reader = new StreamReader(stream);
            var migrationSql = reader.ReadToEnd();

            using var command = connection.CreateCommand();
            command.CommandText = migrationSql;

            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.Message.Contains("duplicate column"))
            {
                // Column already exists, skip and continue
                // Update schema version anyway
                using var versionCommand = connection.CreateCommand();
                versionCommand.CommandText = $"INSERT OR IGNORE INTO schema_version (version, applied_at) VALUES ({migration.version}, unixepoch());";
                versionCommand.ExecuteNonQuery();
            }
        }
    }

    private int GetSchemaVersion(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT MAX(version) FROM schema_version;";
            var result = command.ExecuteScalar();
            return result is int version ? version : 0;
        }
        catch
        {
            // Table doesn't exist yet
            return 0;
        }
    }
}
