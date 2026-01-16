using System.Text.Json;
using FluentAssertions;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Core.Serialization;

namespace OpenGameVerse.Core.Tests.Serialization;

public class JsonContextTests
{
    [Fact]
    public void JsonContext_ShouldSerializeGame()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            Title = "Test Game",
            InstallPath = "/path/to/game",
            Platform = "Steam",
            SizeBytes = 1024,
            DiscoveredAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        // Act
        var json = JsonSerializer.Serialize(game, OpenGameVerseJsonContext.Default.Game);

        // Assert
        json.Should().Contain("\"title\":\"Test Game\"");
        json.Should().Contain("\"platform\":\"Steam\"");
        json.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void JsonContext_ShouldDeserializeGame()
    {
        // Arrange
        var json = """
            {
                "id": 1,
                "title": "Test Game",
                "installPath": "/path/to/game",
                "platform": "Steam",
                "sizeBytes": 1024,
                "discoveredAt": "2024-01-01T00:00:00Z",
                "updatedAt": "2024-01-01T00:00:00Z"
            }
            """;

        // Act
        var game = JsonSerializer.Deserialize(json, OpenGameVerseJsonContext.Default.Game);

        // Assert
        game.Should().NotBeNull();
        game!.Title.Should().Be("Test Game");
        game.Platform.Should().Be("Steam");
        game.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void JsonContext_ShouldSerializeGameList()
    {
        // Arrange
        var games = new List<Game>
        {
            new Game
            {
                Title = "Game 1",
                InstallPath = "/path1",
                Platform = "Steam",
            },
            new Game
            {
                Title = "Game 2",
                InstallPath = "/path2",
                Platform = "Epic",
            },
        };

        // Act
        var json = JsonSerializer.Serialize(games, OpenGameVerseJsonContext.Default.ListGame);

        // Assert
        json.Should().Contain("Game 1");
        json.Should().Contain("Game 2");
        json.Should().StartWith("[");
        json.Should().EndWith("]");
    }
}
