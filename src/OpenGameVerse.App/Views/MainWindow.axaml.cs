using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenGameVerse.App.ViewModels;

namespace OpenGameVerse.App.Views;

public partial class MainWindow : Window
{
    private GameViewModel? _contextMenuGameVm;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnGameDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { DataContext: GameViewModel gameVm })
        {
            if (DataContext is MainWindowViewModel vm)
            {
                _ = vm.LaunchGameAsync(gameVm);
            }
            e.Handled = true;
        }
    }

    private void OnSetCategoryMenuOpening(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // 存儲當前遊戲（從菜單的DataContext獲取）
        if (menu.Parent is ContextMenu ctx && ctx.DataContext is GameViewModel gameVm)
        {
            _contextMenuGameVm = gameVm;
        }

        // 移除舊的分類菜單項（保留前3個固定項：New Category, Separator, Uncategorized）
        while (menu.Items.Count > 3)
        {
            menu.Items.RemoveAt(menu.Items.Count - 1);
        }

        // 添加分隔符
        if (vm.CategoriesForAssignment.Count > 0)
        {
            menu.Items.Add(new Separator());
        }

        // 動態添加現有分類
        foreach (var category in vm.CategoriesForAssignment)
        {
            var categoryMenuItem = new MenuItem
            {
                Header = category,
                Command = vm.SetCategoryToCommand,
                CommandParameter = (_contextMenuGameVm, category)
            };
            menu.Items.Add(categoryMenuItem);
        }
    }

}
