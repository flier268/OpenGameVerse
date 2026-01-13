using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenGameVerse.App.ViewModels;

public partial class CategoryInputDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string? CategoryName { get; set; }

    [RelayCommand]
    private void Ok(Window? window)
    {
        var trimmed = CategoryName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        window?.Close(trimmed);
    }

    [RelayCommand]
    private void Cancel(Window? window)
    {
        window?.Close(null);
    }
}
