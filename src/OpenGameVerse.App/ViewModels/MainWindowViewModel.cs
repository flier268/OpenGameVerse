using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using OpenGameVerse.Metadata.Abstractions;
using OpenGameVerse.App.Views;
using OpenGameVerse.App.Services;

namespace OpenGameVerse.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGameRepository _gameRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPlatformHost _platformHost;
    private readonly IMetadataService? _metadataService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    public partial string Title { get; set; } = "OpenGameVerse - Game Library";

    [ObservableProperty]
    public partial ObservableCollection<GameViewModel> Games { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<GameViewModel> FilteredGames { get; set; } = new();

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial int TotalGames { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SelectedPlatformFilter { get; set; } = "All Platforms";

    [ObservableProperty]
    public partial string? SelectedCategoryFilter { get; set; } = "All Categories";

    [ObservableProperty]
    public partial bool ShowFavoritesOnly { get; set; }

    [ObservableProperty]
    public partial bool IsGridView { get; set; } = true;

    public ObservableCollection<string> AvailablePlatforms { get; } = new() { "All Platforms" };
    public ObservableCollection<string> AvailableCategories { get; } = new() { "All Categories" };

    /// <summary>
    /// Available categories for assignment (without "All Categories" and "Uncategorized")
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<string> CategoriesForAssignment { get; set; } = new();

    public MainWindowViewModel(IGameRepository gameRepository, ICategoryRepository categoryRepository, IPlatformHost platformHost, IDialogService dialogService, IMetadataService? metadataService = null)
    {
        _gameRepository = gameRepository;
        _categoryRepository = categoryRepository;
        _platformHost = platformHost;
        _metadataService = metadataService;
        _dialogService = dialogService;

        // Wire up property changed events for filtering
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SearchText) or nameof(SelectedPlatformFilter)
                or nameof(SelectedCategoryFilter) or nameof(ShowFavoritesOnly))
            {
                ApplyFilters();
            }
        };
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

            // Update available platforms and categories
            await UpdateFiltersAsync();

            // Apply initial filter
            ApplyFilters();
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

                        if (_metadataService != null)
                        {
                            _ = EnrichGameMetadataAsync(game, gameVm);
                        }
                    }
                }
            }

            StatusMessage = $"Scan complete: Found {gamesFound}, added {gamesAdded} new games";
            TotalGames = Games.Count;

            // Refresh filters so newly added games appear in the visible list
            await UpdateFiltersAsync();
            ApplyFilters();
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
    private async Task EnterFullscreenAsync(Window? parentWindow)
    {
        if (parentWindow == null || !parentWindow.IsVisible)
        {
            return;
        }

        var fullscreenViewModel = new FullscreenViewModel(_gameRepository, _platformHost, parentWindow, _metadataService);
        var fullscreenWindow = new FullscreenWindow
        {
            DataContext = fullscreenViewModel
        };

        fullscreenWindow.Closed += (_, _) => parentWindow.Show();

        parentWindow.Hide();
        fullscreenWindow.Show();

        await fullscreenViewModel.InitializeAsync();
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

    [RelayCommand]
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

    private async Task UpdateFiltersAsync()
    {
        // Update platform list
        var platforms = Games.Select(g => g.Platform).Distinct().OrderBy(p => p).ToList();
        AvailablePlatforms.Clear();
        AvailablePlatforms.Add("All Platforms");
        foreach (var platform in platforms)
        {
            AvailablePlatforms.Add(platform);
        }
        if (string.IsNullOrEmpty(SelectedPlatformFilter) || !AvailablePlatforms.Contains(SelectedPlatformFilter))
        {
            SelectedPlatformFilter = "All Platforms";
        }

        // Update category list from repository and existing game assignments
        var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var (name, _) in _categoryRepository.GetAllCategoriesAsync(CancellationToken.None))
        {
            if (!string.IsNullOrWhiteSpace(name) && name != "Uncategorized")
            {
                categories.Add(name);
            }
        }

        foreach (var category in Games
            .Where(g => !string.IsNullOrEmpty(g.CustomCategory))
            .Select(g => g.CustomCategory!))
        {
            categories.Add(category);
        }

        var orderedCategories = categories.OrderBy(c => c).ToList();
        AvailableCategories.Clear();
        AvailableCategories.Add("All Categories");
        AvailableCategories.Add("Uncategorized");
        foreach (var category in orderedCategories)
        {
            AvailableCategories.Add(category);
        }
        if (string.IsNullOrEmpty(SelectedCategoryFilter) || !AvailableCategories.Contains(SelectedCategoryFilter))
        {
            SelectedCategoryFilter = "All Categories";
        }

        // Update assignment categories (for right-click menu)
        CategoriesForAssignment.Clear();
        foreach (var category in orderedCategories)
        {
            CategoriesForAssignment.Add(category);
        }
    }

    private void ApplyFilters()
    {
        var filtered = Games.AsEnumerable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(g => g.Title.ToLowerInvariant().Contains(searchLower));
        }

        // Favorites filter
        if (ShowFavoritesOnly)
        {
            filtered = filtered.Where(g => g.IsFavorite);
        }

        // Platform filter
        if (!string.IsNullOrEmpty(SelectedPlatformFilter) && SelectedPlatformFilter != "All Platforms")
        {
            filtered = filtered.Where(g => g.Platform == SelectedPlatformFilter);
        }

        // Category filter
        if (!string.IsNullOrEmpty(SelectedCategoryFilter) && SelectedCategoryFilter != "All Categories")
        {
            if (SelectedCategoryFilter == "Uncategorized")
            {
                filtered = filtered.Where(g => string.IsNullOrEmpty(g.CustomCategory));
            }
            else
            {
                filtered = filtered.Where(g => g.CustomCategory == SelectedCategoryFilter);
            }
        }

        // Sort: Favorites first, then by custom sort order within category, then by title
        filtered = filtered
            .OrderByDescending(g => g.IsFavorite)
            .ThenBy(g => g.CustomCategory ?? string.Empty)
            .ThenBy(g => g.SortOrder)
            .ThenBy(g => g.Title);

        FilteredGames.Clear();
        foreach (var game in filtered)
        {
            FilteredGames.Add(game);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(GameViewModel gameViewModel)
    {
        try
        {
            gameViewModel.IsFavorite = !gameViewModel.IsFavorite;

            // Update in database
            var gameResult = await _gameRepository.GetGameByIdAsync(gameViewModel.Id, CancellationToken.None);
            if (gameResult.IsSuccess && gameResult.Value != null)
            {
                var game = gameResult.Value;
                game.IsFavorite = gameViewModel.IsFavorite;
                await _gameRepository.UpdateGameAsync(game, CancellationToken.None);
            }

            // Reapply filters to update order
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error toggling favorite: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetCategoryAsync(GameViewModel gameViewModel)
    {
        try
        {
            // For now, set to null (uncategorized)
            // In a full implementation, this would open a dialog to select/create category
            gameViewModel.CustomCategory = null;

            // Update in database
            var gameResult = await _gameRepository.GetGameByIdAsync(gameViewModel.Id, CancellationToken.None);
            if (gameResult.IsSuccess && gameResult.Value != null)
            {
                var game = gameResult.Value;
                game.CustomCategory = null;
                await _gameRepository.UpdateGameAsync(game, CancellationToken.None);
            }

            // Update available categories
            await UpdateFiltersAsync();

            // Reapply filters
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error setting category: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetCategoryToAsync((GameViewModel game, string category) args)
    {
        try
        {
            args.game.CustomCategory = args.category;

            // Update in database
            var gameResult = await _gameRepository.GetGameByIdAsync(args.game.Id, CancellationToken.None);
            if (gameResult.IsSuccess && gameResult.Value != null)
            {
                var game = gameResult.Value;
                game.CustomCategory = args.category;
                await _gameRepository.UpdateGameAsync(game, CancellationToken.None);
            }

            // Update available categories
            await UpdateFiltersAsync();

            // Reapply filters
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error setting category: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedPlatformFilter = "All Platforms";
        SelectedCategoryFilter = "All Categories";
        ShowFavoritesOnly = false;
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsGridView = !IsGridView;
    }

    [RelayCommand]
    private async Task CreateNewCategoryAsync(GameViewModel gameViewModel)
    {
        try
        {
            var categoryName = await _dialogService.ShowCategoryInputDialogAsync();
            if (!string.IsNullOrEmpty(categoryName) && gameViewModel != null)
            {
                var trimmedName = categoryName.Trim();
                if (string.IsNullOrEmpty(trimmedName))
                {
                    return;
                }

                var exists = await _categoryRepository.CategoryExistsAsync(trimmedName, CancellationToken.None);
                if (!exists)
                {
                    var added = await _categoryRepository.AddCategoryAsync(trimmedName, CancellationToken.None);
                    if (!added)
                    {
                        StatusMessage = $"Error creating category: {trimmedName}";
                        return;
                    }
                }

                await SetCategoryToCommand.ExecuteAsync((gameViewModel, trimmedName));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating category: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ManageCategoriesAsync()
    {
        try
        {
            await _dialogService.ShowCategoryManagerAsync(_gameRepository, _categoryRepository);
            // Reload games so deleted categories are reflected in the visible list
            await LoadGamesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error managing categories: {ex.Message}";
        }
    }
}
