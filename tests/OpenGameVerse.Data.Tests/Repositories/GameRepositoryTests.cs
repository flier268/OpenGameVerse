using FluentAssertions;
using Microsoft.Data.Sqlite;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Data;
using OpenGameVerse.Data.Repositories;

namespace OpenGameVerse.Data.Tests.Repositories;

public class GameRepositoryTests : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;
    private readonly GameRepository _repository;

    public GameRepositoryTests()
    {
        // Use in-memory database for testing with a persistent connection
        _connectionString = "Data Source=InMemorySample;Mode=Memory;Cache=Shared";

        // Keep connection open to maintain in-memory database
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        // Initialize database
        var dbContext = new DatabaseContext(_connectionString);
        dbContext.Initialize();

        _repository = new GameRepository(_connectionString);
    }

    [Fact]
    public async Task AddGameAsync_ShouldAddGameToDatabase()
    {
        // Arrange
        var game = new Game
        {
            Title = "Test Game",
            InstallPath = "/path/to/game",
            Platform = "Steam",
            SizeBytes = 1024,
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddGameAsync(game, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGameByIdAsync_ShouldReturnGame_WhenExists()
    {
        // Arrange
        var game = new Game
        {
            Title = "Portal 2",
            InstallPath = "/games/portal2",
            Platform = "Steam",
            SizeBytes = 1024 * 1024 * 1024,
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var addResult = await _repository.AddGameAsync(game, CancellationToken.None);
        var gameId = addResult.Value;

        // Act
        var result = await _repository.GetGameByIdAsync(gameId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Portal 2");
        result.Value.Platform.Should().Be("Steam");
    }

    [Fact]
    public async Task GetGameByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetGameByIdAsync(999, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetGameByPathAsync_ShouldReturnGame_WhenExists()
    {
        // Arrange
        var game = new Game
        {
            Title = "Half-Life 2",
            InstallPath = "/games/halflife2",
            Platform = "Steam",
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddGameAsync(game, CancellationToken.None);

        // Act
        var result = await _repository.GetGameByPathAsync("/games/halflife2", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Half-Life 2");
    }

    [Fact]
    public async Task GetAllGamesAsync_ShouldReturnAllGames()
    {
        // Arrange
        var games = new[]
        {
            new Game { Title = "Game 1", InstallPath = "/path1", Platform = "Steam", DiscoveredAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Game { Title = "Game 2", InstallPath = "/path2", Platform = "Epic", DiscoveredAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Game { Title = "Game 3", InstallPath = "/path3", Platform = "GOG", DiscoveredAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        foreach (var game in games)
        {
            await _repository.AddGameAsync(game, CancellationToken.None);
        }

        // Act
        var result = new List<Game>();
        await foreach (var game in _repository.GetAllGamesAsync(CancellationToken.None))
        {
            result.Add(game);
        }

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(g => g.Title == "Game 1");
        result.Should().Contain(g => g.Title == "Game 2");
        result.Should().Contain(g => g.Title == "Game 3");
    }

    [Fact]
    public async Task GetGameCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var games = new[]
        {
            new Game { Title = "Game 1", InstallPath = "/path1", Platform = "Steam", DiscoveredAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Game { Title = "Game 2", InstallPath = "/path2", Platform = "Epic", DiscoveredAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        foreach (var game in games)
        {
            await _repository.AddGameAsync(game, CancellationToken.None);
        }

        // Act
        var result = await _repository.GetGameCountAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task UpdateGameAsync_ShouldUpdateExistingGame()
    {
        // Arrange
        var game = new Game
        {
            Title = "Original Title",
            InstallPath = "/original/path",
            Platform = "Steam",
            SizeBytes = 1000,
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var addResult = await _repository.AddGameAsync(game, CancellationToken.None);
        game.Id = addResult.Value;

        // Act
        game.Title = "Updated Title";
        game.SizeBytes = 2000;
        var updateResult = await _repository.UpdateGameAsync(game, CancellationToken.None);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();

        var getResult = await _repository.GetGameByIdAsync(game.Id, CancellationToken.None);
        getResult.Value!.Title.Should().Be("Updated Title");
        getResult.Value.SizeBytes.Should().Be(2000);
    }

    [Fact]
    public async Task DeleteGameAsync_ShouldRemoveGame()
    {
        // Arrange
        var game = new Game
        {
            Title = "To Be Deleted",
            InstallPath = "/delete/me",
            Platform = "Steam",
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var addResult = await _repository.AddGameAsync(game, CancellationToken.None);
        var gameId = addResult.Value;

        // Act
        var deleteResult = await _repository.DeleteGameAsync(gameId, CancellationToken.None);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();

        var getResult = await _repository.GetGameByIdAsync(gameId, CancellationToken.None);
        getResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task AddGameAsync_ShouldPreventDuplicatePaths()
    {
        // Arrange
        var game1 = new Game
        {
            Title = "Game 1",
            InstallPath = "/same/path",
            Platform = "Steam",
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var game2 = new Game
        {
            Title = "Game 2",
            InstallPath = "/same/path", // Same path
            Platform = "Epic",
            DiscoveredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result1 = await _repository.AddGameAsync(game1, CancellationToken.None);
        var result2 = await _repository.AddGameAsync(game2, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeFalse(); // Should fail due to unique constraint
        result2.Error.Should().Contain("Failed to add game");
    }

    public void Dispose()
    {
        // Clean up in-memory database connection
        _keepAliveConnection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
