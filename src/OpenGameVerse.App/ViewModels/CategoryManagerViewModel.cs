using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameVerse.App.Services;
using OpenGameVerse.Core.Abstractions;
using OpenGameVerse.Core.Models;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace OpenGameVerse.App.ViewModels;

/// <summary>
/// ViewModel for managing game categories
/// </summary>
public partial class CategoryManagerViewModel : ViewModelBase
{
    private readonly IGameRepository _gameRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    public partial ObservableCollection<CategoryInfo> Categories { get; set; } = new();

    [ObservableProperty]
    public partial CategoryInfo? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public CategoryManagerViewModel(
        IGameRepository gameRepository,
        ICategoryRepository categoryRepository,
        IDialogService dialogService
    )
    {
        _gameRepository = gameRepository;
        _categoryRepository = categoryRepository;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        try
        {
            Categories.Clear();

            // Load all categories from the repository
            var categoryList = new List<CategoryInfo>();
            await foreach (
                var (name, gameCount) in _categoryRepository
                    .GetAllCategoriesAsync(CancellationToken.None)
                    .ConfigureAwait(false)
            )
            {
                categoryList.Add(new CategoryInfo { Name = name, Count = gameCount });
            }

            // Add to collection on UI thread
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var category in categoryList)
                {
                    Categories.Add(category);
                }
            });
        }
        catch
        {
            // Handle error silently
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (IsLoading)
        {
            return;
        }

        if (SelectedCategory == null || SelectedCategory.Name == "Uncategorized")
        {
            return;
        }

        IsLoading = true;
        try
        {
            var categoryName = SelectedCategory.Name;

            // Move all games in this category to uncategorized (off UI thread)
            await Task.Run(async () =>
                {
                    var gamesToUpdate = new List<Game>();
                    await foreach (
                        var game in _gameRepository
                            .GetAllGamesAsync(CancellationToken.None)
                            .ConfigureAwait(false)
                    )
                    {
                        if (game.CustomCategory == categoryName)
                        {
                            gamesToUpdate.Add(game);
                        }
                    }

                    // Update games in batch
                    foreach (var game in gamesToUpdate)
                    {
                        game.CustomCategory = null;
                        await _gameRepository
                            .UpdateGameAsync(game, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    // Delete the category from the repository
                    await _categoryRepository
                        .DeleteCategoryAsync(categoryName, CancellationToken.None)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            // Clear selection
            SelectedCategory = null;

            await LoadCategoriesAsync();
        }
        catch
        {
            // Handle error
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (IsLoading)
        {
            return;
        }

        var categoryName = await _dialogService.ShowCategoryInputDialogAsync();
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            await AddCategoryFromDialogAsync(categoryName.Trim());
        }
    }

    [RelayCommand]
    private void Close(Window? window)
    {
        window?.Close();
    }

    public async Task AddCategoryFromDialogAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return;
        }

        try
        {
            // Add category to database
            var success = await _categoryRepository.AddCategoryAsync(
                categoryName,
                CancellationToken.None
            );

            if (success && !Categories.Any(c => c.Name == categoryName))
            {
                Categories.Add(new CategoryInfo { Name = categoryName, Count = 0 });
            }
        }
        catch
        {
            // Handle error
        }
    }
}

/// <summary>
/// Represents a category with game count
/// </summary>
public sealed class CategoryInfo
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
