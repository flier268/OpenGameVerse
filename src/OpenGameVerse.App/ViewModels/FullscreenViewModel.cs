using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Metadata.Abstractions;
using OpenGameVerse.App.Services;

namespace OpenGameVerse.App.ViewModels;

public partial class FullscreenViewModel : ViewModelBase
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlatformHost _platformHost;
    private readonly IMetadataService? _metadataService;
    private readonly Window _parentWindow;
    private readonly IAppSettingsService _settingsService;
    private readonly GameStatusMonitorService _gameStatusMonitor;

    [ObservableProperty]
    public partial ObservableCollection<GameViewModel> Games { get; set; } = new();

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial int TotalGames { get; set; }

    public FullscreenViewModel(
        IGameRepository gameRepository,
        IPlatformHost platformHost,
        Window parentWindow,
        IAppSettingsService settingsService,
        GameStatusMonitorService gameStatusMonitor,
        IMetadataService? metadataService = null)
    {
        _gameRepository = gameRepository;
        _platformHost = platformHost;
        _parentWindow = parentWindow;
        _settingsService = settingsService;
        _gameStatusMonitor = gameStatusMonitor;
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
                if (_metadataService != null && _settingsService.CurrentSettings.DownloadMetadataAfterImport)
                {
                    _ = EnrichGameMetadataAsync(game, gameVm);
                }
            }

            StatusMessage = $"Loaded {Games.Count} games - Ready to play!";

            _gameStatusMonitor.UpdateTrackedGames(Games);
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
                        var existingGame = existingResult.Value;
                        if (!string.IsNullOrWhiteSpace(gameInstallation.PlatformId)
                            && string.IsNullOrWhiteSpace(existingGame.PlatformId))
                        {
                            existingGame.PlatformId = gameInstallation.PlatformId;
                            await _gameRepository.UpdateGameAsync(existingGame, CancellationToken.None);
                        }
                        if (!string.IsNullOrWhiteSpace(gameInstallation.CoverImagePath))
                        {
                            var needsCover = string.IsNullOrWhiteSpace(existingGame.CoverImagePath)
                                || !File.Exists(existingGame.CoverImagePath);
                            if (needsCover)
                            {
                                existingGame.CoverImagePath = gameInstallation.CoverImagePath;
                                await _gameRepository.UpdateGameAsync(existingGame, CancellationToken.None);

                                var existingVm = Games.FirstOrDefault(vm =>
                                    string.Equals(vm.InstallPath, existingGame.InstallPath, StringComparison.OrdinalIgnoreCase));
                                if (existingVm != null)
                                {
                                    existingVm.CoverImagePath = gameInstallation.CoverImagePath;
                                }
                            }
                        }

                        if (_settingsService.CurrentSettings.UpdateInstallSizeOnLibraryUpdate
                            && existingGame.SizeBytes != gameInstallation.SizeBytes)
                        {
                            existingGame.SizeBytes = gameInstallation.SizeBytes;
                            await _gameRepository.UpdateGameAsync(existingGame, CancellationToken.None);

                            var existingVm = Games.FirstOrDefault(vm =>
                                string.Equals(vm.InstallPath, existingGame.InstallPath, StringComparison.OrdinalIgnoreCase));
                            if (existingVm != null)
                            {
                                existingVm.SizeBytes = gameInstallation.SizeBytes;
                            }
                        }

                        continue; // Already in library
                    }

                    // Add new game
                    var game = new Game
                    {
                        Title = gameInstallation.Title,
                        NormalizedTitle = gameInstallation.Title.ToLowerInvariant(),
                        InstallPath = gameInstallation.InstallPath,
                        Platform = gameInstallation.Platform,
                        PlatformId = gameInstallation.PlatformId,
                        ExecutablePath = gameInstallation.ExecutablePath,
                        IconPath = gameInstallation.IconPath,
                        CoverImagePath = gameInstallation.CoverImagePath,
                        SizeBytes = gameInstallation.SizeBytes,
                        DiscoveredAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var result = await _gameRepository.AddGameAsync(game, CancellationToken.None);
                    if (result.IsSuccess)
                    {
                        game.Id = result.Value;
                        gamesAdded++;

                        var gameVm = GameViewModel.FromModel(game);
                        Games.Add(gameVm);

                        if (_metadataService != null && _settingsService.CurrentSettings.DownloadMetadataAfterImport)
                        {
                            _ = EnrichGameMetadataAsync(game, gameVm);
                        }
                    }
                }
            }

            StatusMessage = $"Scan complete: Found {gamesFound}, added {gamesAdded} new games";
            TotalGames = Games.Count;

            _gameStatusMonitor.UpdateTrackedGames(Games);
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

    [RelayCommand]
    private void ExitFullscreen()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Close fullscreen window and show main window
            _parentWindow.Show();

            // Find and close fullscreen window
            foreach (var window in desktop.Windows)
            {
                if (window is Views.FullscreenWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }

    public async Task LaunchGameAsync(GameViewModel gameViewModel)
    {
        try
        {
            StatusMessage = $"Launching {gameViewModel.Title}...";

            // Get full game data from repository
            var gameResult = await _gameRepository.GetGameByIdAsync(gameViewModel.Id, CancellationToken.None);

            if (!gameResult.IsSuccess || gameResult.Value == null)
            {
                StatusMessage = $"Error: Could not find game data for {gameViewModel.Title}";
                return;
            }

            var game = gameResult.Value;

            // Create GameInstallation for launching
            var installation = new GameInstallation
            {
                Title = game.Title,
                InstallPath = game.InstallPath,
                Platform = game.Platform,
                ExecutablePath = game.ExecutablePath,
                IconPath = game.IconPath,
                SizeBytes = game.SizeBytes
            };

            // Launch through platform host
            var launchResult = await _platformHost.LaunchGameAsync(installation, CancellationToken.None);

            if (launchResult.IsSuccess)
            {
                var settings = _settingsService.CurrentSettings;
                var fullscreenWindow = GetFullscreenWindow();
                var didChangeWindow = ApplyLaunchAction(fullscreenWindow, settings.GameLaunchAction);

                var process = launchResult.Value;
                if (process != null)
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += (_, _) =>
                    {
                        process.Dispose();
                        if (settings.GameCloseAction == GameCloseAction.KeepMinimized)
                        {
                            return;
                        }

                        if (settings.GameCloseAction == GameCloseAction.RestoreWhenLaunchedFromUi && !didChangeWindow)
                        {
                            return;
                        }

                        Dispatcher.UIThread.Post(() => RestoreWindow(fullscreenWindow));
                    };
                }
                else
                {
                    _ = MonitorGameExitAsync(game.Id, fullscreenWindow, didChangeWindow, settings.GameCloseAction);
                }

                // Update last played
                game.LastPlayed = DateTime.UtcNow;
                await _gameRepository.UpdateGameAsync(game, CancellationToken.None);

                StatusMessage = $"Launched {game.Title}";
            }
            else
            {
                StatusMessage = $"Failed to launch {game.Title}: {launchResult.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Launch error: {ex.Message}";
        }
    }

    private static Window? GetFullscreenWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window is Views.FullscreenWindow)
                {
                    return window;
                }
            }
        }

        return null;
    }

    private static bool ApplyLaunchAction(Window? window, GameLaunchAction action)
    {
        if (window == null)
        {
            return false;
        }

        switch (action)
        {
            case GameLaunchAction.Minimize:
                window.WindowState = WindowState.Minimized;
                return true;
            case GameLaunchAction.Hide:
                window.Hide();
                return true;
            case GameLaunchAction.NoChange:
            default:
                return false;
        }
    }

    private static void RestoreWindow(Window? window)
    {
        if (window == null)
        {
            return;
        }

        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    private async Task MonitorGameExitAsync(long gameId, Window? window, bool didChangeWindow, GameCloseAction closeAction)
    {
        if (closeAction == GameCloseAction.KeepMinimized)
        {
            return;
        }

        if (closeAction == GameCloseAction.RestoreWhenLaunchedFromUi && !didChangeWindow)
        {
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromHours(6));
        try
        {
            await _gameStatusMonitor.WaitForGameExitAsync(gameId, cts.Token);
            Dispatcher.UIThread.Post(() => RestoreWindow(window));
        }
        catch (OperationCanceledException)
        {
        }
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
