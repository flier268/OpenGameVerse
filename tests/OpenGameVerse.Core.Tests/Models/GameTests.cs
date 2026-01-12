using FluentAssertions;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.Core.Tests.Models;

public class GameTests
{
    [Fact]
    public void Game_ShouldBeCreatedWithRequiredProperties()
    {
        // Arrange & Act
        var game = new Game
        {
            Title = "Test Game",
            InstallPath = "/path/to/game",
            Platform = "Steam"
        };

        // Assert
        game.Title.Should().Be("Test Game");
        game.InstallPath.Should().Be("/path/to/game");
        game.Platform.Should().Be("Steam");
    }

    [Fact]
    public void Game_ShouldAllowOptionalProperties()
    {
        // Arrange & Act
        var game = new Game
        {
            Title = "Test Game",
            InstallPath = "/path/to/game",
            Platform = "Steam",
            ExecutablePath = "/path/to/game/game.exe",
            IconPath = "/path/to/icon.png",
            SizeBytes = 1024 * 1024 * 1024, // 1GB
            LastPlayed = DateTime.UtcNow,
            IgdbId = "12345",
            CoverImagePath = "/path/to/cover.jpg"
        };

        // Assert
        game.ExecutablePath.Should().Be("/path/to/game/game.exe");
        game.IconPath.Should().Be("/path/to/icon.png");
        game.SizeBytes.Should().Be(1024 * 1024 * 1024);
        game.LastPlayed.Should().NotBeNull();
        game.IgdbId.Should().Be("12345");
        game.CoverImagePath.Should().Be("/path/to/cover.jpg");
    }

    [Fact]
    public void GameInstallation_ShouldMapToPlatformId()
    {
        // Arrange & Act
        var installation = new GameInstallation
        {
            Title = "Portal 2",
            InstallPath = "/games/portal2",
            Platform = "Steam",
            PlatformId = "620" // Steam App ID
        };

        // Assert
        installation.PlatformId.Should().Be("620");
        installation.Platform.Should().Be("Steam");
    }
}
