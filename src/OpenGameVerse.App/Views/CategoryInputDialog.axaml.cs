using Avalonia.Controls;
using OpenGameVerse.App.ViewModels;

namespace OpenGameVerse.App.Views;

public partial class CategoryInputDialog : Window
{
    public CategoryInputDialog()
    {
        InitializeComponent();
        DataContext = new CategoryInputDialogViewModel();
    }
}
