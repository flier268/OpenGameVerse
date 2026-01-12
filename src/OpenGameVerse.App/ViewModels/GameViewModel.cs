using CommunityToolkit.Mvvm.ComponentModel;
using OpenGameVerse.Core.Models;

namespace OpenGameVerse.App.ViewModels;

/// <summary>
/// ViewModel for displaying a single game
/// </summary>
public partial class GameViewModel : ViewModelBase
{
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _platform = string.Empty;

    [ObservableProperty]
    private string _installPath = string.Empty;

    [ObservableProperty]
    private string? _coverImagePath;

    [ObservableProperty]
    private long _sizeBytes;

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
            SizeBytes = game.SizeBytes
        };
    }
}
