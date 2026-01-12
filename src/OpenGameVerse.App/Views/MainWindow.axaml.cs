using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenGameVerse.App.ViewModels;

namespace OpenGameVerse.App.Views;

public partial class MainWindow : Window
{
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
}