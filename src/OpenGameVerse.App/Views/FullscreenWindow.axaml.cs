using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenGameVerse.App.ViewModels;
using SukiUI.Controls;

namespace OpenGameVerse.App.Views;

public partial class FullscreenWindow : SukiWindow
{
    private FullscreenViewModel? _viewModel;
    private bool _initialFocusSet;

    public FullscreenWindow()
    {
        InitializeComponent();

        // Handle keyboard navigation
        KeyDown += OnKeyDown;
        Opened += OnOpened;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        TryFocusFirstTile();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Games.CollectionChanged -= OnGamesCollectionChanged;
        }

        _viewModel = DataContext as FullscreenViewModel;
        if (_viewModel != null)
        {
            _initialFocusSet = false;
            _viewModel.Games.CollectionChanged += OnGamesCollectionChanged;
        }
    }

    private void OnGamesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_initialFocusSet)
        {
            return;
        }

        Dispatcher.UIThread.Post(TryFocusFirstTile, DispatcherPriority.Background);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Keyboard shortcuts
        switch (e.Key)
        {
            case Key.Escape:
                // Exit fullscreen
                if (DataContext is FullscreenViewModel vm)
                {
                    vm.ExitFullscreenCommand.Execute(null);
                }
                e.Handled = true;
                break;
            case Key.Back when e.KeyDeviceType == KeyDeviceType.Gamepad:
                // Exit fullscreen
                if (DataContext is FullscreenViewModel vmBack)
                {
                    vmBack.ExitFullscreenCommand.Execute(null);
                }
                e.Handled = true;
                break;

            case Key.F5:
                // Refresh
                if (DataContext is FullscreenViewModel vm2)
                {
                    vm2.RefreshCommand.Execute(null);
                }
                e.Handled = true;
                break;

            case Key.Enter:
            case Key.Space:
                // Launch focused game
                if (e.KeyDeviceType == KeyDeviceType.Gamepad && EnsureTileFocus())
                {
                    e.Handled = true;
                    break;
                }
                if (FocusManager?.GetFocusedElement() is Border { DataContext: GameViewModel gameVm })
                {
                    if (DataContext is FullscreenViewModel vm3)
                    {
                        _ = vm3.LaunchGameAsync(gameVm);
                    }
                    e.Handled = true;
                }
                break;

            case Key.Up:
                if (e.KeyDeviceType == KeyDeviceType.Gamepad || IsGameTileFocused())
                {
                    MoveFocus(NavigationDirection.Up);
                    e.Handled = true;
                }
                break;
            case Key.Down:
                if (e.KeyDeviceType == KeyDeviceType.Gamepad || IsGameTileFocused())
                {
                    MoveFocus(NavigationDirection.Down);
                    e.Handled = true;
                }
                break;
            case Key.Left:
                if (e.KeyDeviceType == KeyDeviceType.Gamepad || IsGameTileFocused())
                {
                    MoveFocus(NavigationDirection.Left);
                    e.Handled = true;
                }
                break;
            case Key.Right:
                if (e.KeyDeviceType == KeyDeviceType.Gamepad || IsGameTileFocused())
                {
                    MoveFocus(NavigationDirection.Right);
                    e.Handled = true;
                }
                break;
        }
    }

    private void OnGameDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { DataContext: GameViewModel gameVm })
        {
            if (DataContext is FullscreenViewModel vm)
            {
                _ = vm.LaunchGameAsync(gameVm);
            }
            e.Handled = true;
        }
    }

    private void TryFocusFirstTile()
    {
        if (_initialFocusSet)
        {
            return;
        }

        var firstTile = GetGameTiles().FirstOrDefault();
        if (firstTile == null)
        {
            return;
        }

        _initialFocusSet = true;
        FocusTile(firstTile);
    }

    private bool EnsureTileFocus()
    {
        if (GetFocusedTile() is { DataContext: GameViewModel })
        {
            return false;
        }

        TryFocusFirstTile();
        return true;
    }

    private bool IsGameTileFocused()
    {
        return GetFocusedTile() != null;
    }

    private Border? GetFocusedTile()
    {
        var focused = FocusManager?.GetFocusedElement() as Visual;
        if (focused == null)
        {
            return null;
        }

        if (focused is Border focusedBorder && focusedBorder.Classes.Contains("game-tile"))
        {
            return focusedBorder;
        }

        return focused
            .GetVisualAncestors()
            .OfType<Border>()
            .FirstOrDefault(tile => tile.Classes.Contains("game-tile"));
    }

    private void MoveFocus(NavigationDirection direction)
    {
        var tiles = GetTileInfos();
        if (tiles.Count == 0)
        {
            return;
        }

        var currentTile = GetFocusedTile();

        if (currentTile == null)
        {
            TryFocusFirstTile();
            return;
        }

        var currentInfoIndex = tiles.FindIndex(tile => ReferenceEquals(tile.Tile, currentTile));
        if (currentInfoIndex < 0)
        {
            TryFocusFirstTile();
            return;
        }

        var currentInfo = tiles[currentInfoIndex];
        var next = FindNextTile(direction, currentInfo, tiles);
        if (next != null)
        {
            FocusTile(next);
        }
    }

    private Border? FindNextTile(NavigationDirection direction, TileInfo current, IReadOnlyList<TileInfo> tiles)
    {
        var rowTolerance = current.Size.Height * 0.6;
        var columnTolerance = current.Size.Width * 0.6;

        var candidates = new List<(TileInfo tile, double primary, double secondary)>();
        foreach (var tile in tiles)
        {
            if (ReferenceEquals(tile.Tile, current.Tile))
            {
                continue;
            }

            var dx = tile.Center.X - current.Center.X;
            var dy = tile.Center.Y - current.Center.Y;

            switch (direction)
            {
                case NavigationDirection.Right when dx > 1:
                    candidates.Add((tile, dx, Math.Abs(dy)));
                    break;
                case NavigationDirection.Left when dx < -1:
                    candidates.Add((tile, -dx, Math.Abs(dy)));
                    break;
                case NavigationDirection.Down when dy > 1:
                    candidates.Add((tile, dy, Math.Abs(dx)));
                    break;
                case NavigationDirection.Up when dy < -1:
                    candidates.Add((tile, -dy, Math.Abs(dx)));
                    break;
            }
        }

        if (candidates.Count > 0)
        {
            return candidates
                .OrderBy(candidate => candidate.primary)
                .ThenBy(candidate => candidate.secondary)
                .First()
                .tile.Tile;
        }

        return direction switch
        {
            NavigationDirection.Right => FindWrapTile(current, tiles, rowTolerance, true),
            NavigationDirection.Left => FindWrapTile(current, tiles, rowTolerance, false),
            NavigationDirection.Down => FindWrapTileVertical(current, tiles, columnTolerance, true),
            NavigationDirection.Up => FindWrapTileVertical(current, tiles, columnTolerance, false),
            _ => null
        };
    }

    private Border? FindWrapTile(TileInfo current, IReadOnlyList<TileInfo> tiles, double rowTolerance, bool forward)
    {
        var sameRow = tiles
            .Where(tile => Math.Abs(tile.Center.Y - current.Center.Y) <= rowTolerance)
            .OrderBy(tile => tile.Center.X)
            .ToList();

        if (sameRow.Count > 0)
        {
            return forward ? sameRow.First().Tile : sameRow.Last().Tile;
        }

        var ordered = tiles.OrderBy(tile => tile.Center.Y).ThenBy(tile => tile.Center.X).ToList();
        return forward ? ordered.First().Tile : ordered.Last().Tile;
    }

    private Border? FindWrapTileVertical(TileInfo current, IReadOnlyList<TileInfo> tiles, double columnTolerance, bool forward)
    {
        var sameColumn = tiles
            .Where(tile => Math.Abs(tile.Center.X - current.Center.X) <= columnTolerance)
            .OrderBy(tile => tile.Center.Y)
            .ToList();

        if (sameColumn.Count > 0)
        {
            return forward ? sameColumn.First().Tile : sameColumn.Last().Tile;
        }

        var ordered = tiles.OrderBy(tile => tile.Center.Y).ThenBy(tile => tile.Center.X).ToList();
        return forward ? ordered.First().Tile : ordered.Last().Tile;
    }

    private void FocusTile(Border tile)
    {
        tile.Focus();
        tile.BringIntoView();
    }

    private IReadOnlyList<Border> GetGameTiles()
    {
        var grid = this.FindControl<ItemsControl>("GameGrid");
        if (grid == null)
        {
            return Array.Empty<Border>();
        }

        return grid.GetVisualDescendants()
            .OfType<Border>()
            .Where(tile => tile.Classes.Contains("game-tile"))
            .ToList();
    }

    private List<TileInfo> GetTileInfos()
    {
        var grid = this.FindControl<ItemsControl>("GameGrid");
        if (grid == null)
        {
            return new List<TileInfo>();
        }

        return GetGameTiles()
            .Select(tile =>
            {
                var origin = tile.TranslatePoint(new Avalonia.Point(0, 0), grid) ?? default;
                var center = new Avalonia.Point(origin.X + tile.Bounds.Width / 2, origin.Y + tile.Bounds.Height / 2);
                return new TileInfo(tile, center, tile.Bounds.Size);
            })
            .ToList();
    }

    private readonly record struct TileInfo(Border Tile, Avalonia.Point Center, Avalonia.Size Size);
}
