using CommunityToolkit.Mvvm.ComponentModel;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.ViewModels;

/// <summary>
/// ViewModel for displaying a single game
/// </summary>
public partial class GameViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial long Id { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Platform { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InstallPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? CoverImagePath { get; set; }

    [ObservableProperty]
    public partial long SizeBytes { get; set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial string? CustomCategory { get; set; }

    [ObservableProperty]
    public partial int SortOrder { get; set; }

    public string SizeDisplay => SizeBytes > 0
        ? $"{SizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
        : "Unknown";

    public static GameViewModel FromModel(Game game)
    {
        return new GameViewModel
        {
            Id = game.Id,
            Title = game.Title,
            Platform = game.Platform,
            InstallPath = game.InstallPath,
            CoverImagePath = game.CoverImagePath,
            SizeBytes = game.SizeBytes,
            IsFavorite = game.IsFavorite,
            CustomCategory = game.CustomCategory,
            SortOrder = game.SortOrder
        };
    }
}
