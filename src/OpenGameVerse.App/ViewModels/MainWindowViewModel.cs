using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Metadata.Abstractions;

namespace OpenGameVerse.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlatformHost _platformHost;
    private readonly IMetadataService? _metadataService;

    [ObservableProperty]
    private string _title = "OpenGameVerse - Game Library";

    [ObservableProperty]
    private ObservableCollection<GameViewModel> _games = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private int _totalGames;

    public MainWindowViewModel(IGameRepository gameRepository, IPlatformHost platformHost, IMetadataService? metadataService = null)
    {
        _gameRepository = gameRepository;
        _platformHost = platformHost;
        _metadataService = metadataService;
    }

    public async Task InitializeAsync()
    {
        await LoadGamesAsync();
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        try
        {
            StatusMessage = "Loading games...";
            Games.Clear();

            var countResult = await _gameRepository.GetGameCountAsync(CancellationToken.None);
            if (countResult.IsSuccess)
            {
                TotalGames = countResult.Value;
            }

            await foreach (var game in _gameRepository.GetAllGamesAsync(CancellationToken.None))
            {
                var gameVm = GameViewModel.FromModel(game);
                Games.Add(gameVm);

                // Enrich with metadata if available (fire and forget)
                if (_metadataService != null)
                {
                    _ = EnrichGameMetadataAsync(game, gameVm);
                }
            }

            StatusMessage = $"Loaded {Games.Count} games";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading games: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ScanGamesAsync()
    {
        if (IsScanning) return;

        try
        {
            IsScanning = true;
            StatusMessage = "Scanning for games...";

            int gamesFound = 0;
            int gamesAdded = 0;

            var scanners = _platformHost.GetScanners().ToList();

            foreach (var scanner in scanners)
            {
                var isInstalled = await scanner.IsInstalledAsync(CancellationToken.None);
                if (!isInstalled)
                {
                    continue;
                }

                StatusMessage = $"Scanning {scanner.DisplayName}...";

                await foreach (var gameInstallation in scanner.ScanAsync(CancellationToken.None))
                {
                    gamesFound++;

                    // Check if game already exists
                    var existingResult = await _gameRepository.GetGameByPathAsync(
                        gameInstallation.InstallPath, CancellationToken.None);

                    if (existingResult.IsSuccess && existingResult.Value != null)
                    {
                        continue; // Already in library
                    }

                    // Add new game
                    var game = new Game
                    {
                        Title = gameInstallation.Title,
                        NormalizedTitle = gameInstallation.Title.ToLowerInvariant(),
                        InstallPath = gameInstallation.InstallPath,
                        Platform = gameInstallation.Platform,
                        ExecutablePath = gameInstallation.ExecutablePath,
                        IconPath = gameInstallation.IconPath,
                        SizeBytes = gameInstallation.SizeBytes,
                        DiscoveredAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var result = await _gameRepository.AddGameAsync(game, CancellationToken.None);
                    if (result.IsSuccess)
                    {
                        gamesAdded++;
                        Games.Add(GameViewModel.FromModel(game));
                    }
                }
            }

            StatusMessage = $"Scan complete: Found {gamesFound}, added {gamesAdded} new games";
            TotalGames = Games.Count;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadGamesAsync();
    }

    private async Task EnrichGameMetadataAsync(Game game, GameViewModel gameVm)
    {
        if (_metadataService == null) return;

        try
        {
            var metadataResult = await _metadataService.EnrichGameAsync(game, CancellationToken.None);

            if (!metadataResult.IsSuccess || metadataResult.Value == null)
                return;

            var metadata = metadataResult.Value;

            // Update view model with metadata
            if (!string.IsNullOrEmpty(metadata.Summary))
            {
                // Can add Summary property to GameViewModel later
            }

            // Download cover art if available
            if (!string.IsNullOrEmpty(metadata.CoverImageUrl))
            {
                var coverResult = await _metadataService.DownloadCoverArtAsync(metadata, CancellationToken.None);

                if (coverResult.IsSuccess && !string.IsNullOrEmpty(coverResult.Value))
                {
                    gameVm.CoverImagePath = coverResult.Value;

                    // Update game in database
                    game.IgdbId = metadata.IgdbId.ToString();
                    game.CoverImagePath = coverResult.Value;
                    await _gameRepository.UpdateGameAsync(game, CancellationToken.None);
                }
            }
        }
        catch
        {
            // Silently fail for metadata enrichment
        }
    }
}
