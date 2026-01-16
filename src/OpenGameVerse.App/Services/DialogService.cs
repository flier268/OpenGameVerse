using Avalonia.Controls;
using OpenGameVerse.App.ViewModels;
using OpenGameVerse.App.Views;
using OpenGameVerse.Core.Abstractions;

namespace OpenGameVerse.App.Services;

/// <summary>
/// Service for handling dialog interactions
/// </summary>
public interface IDialogService
{
    Task<string?> ShowCategoryInputDialogAsync();
    Task ShowCategoryManagerAsync(
        IGameRepository gameRepository,
        ICategoryRepository categoryRepository
    );
    Task ShowSettingsAsync(IAppSettingsService settingsService);
}

public sealed class DialogService : IDialogService
{
    private Window? _mainWindow;

    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    public async Task<string?> ShowCategoryInputDialogAsync()
    {
        if (_mainWindow == null)
        {
            return null;
        }

        var dialog = new CategoryInputDialog();
        var result = await dialog.ShowDialog<string?>(_mainWindow);
        return result;
    }

    public async Task ShowCategoryManagerAsync(
        IGameRepository gameRepository,
        ICategoryRepository categoryRepository
    )
    {
        if (_mainWindow == null)
        {
            return;
        }

        var categoryVm = new CategoryManagerViewModel(gameRepository, categoryRepository, this);
        await categoryVm.InitializeAsync();

        var managerWindow = new CategoryManagerWindow { DataContext = categoryVm };
        managerWindow.SetOwner(_mainWindow);
        await managerWindow.ShowDialog(_mainWindow);
    }

    public async Task ShowSettingsAsync(IAppSettingsService settingsService)
    {
        if (_mainWindow == null)
        {
            return;
        }

        var settingsVm = new SettingsViewModel(settingsService);
        var settingsWindow = new SettingsWindow { DataContext = settingsVm };

        await settingsWindow.ShowDialog(_mainWindow);
    }
}
