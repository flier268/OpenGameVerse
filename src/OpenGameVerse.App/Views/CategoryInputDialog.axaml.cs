using SukiUI.Controls;
using OpenGameVerse.App.ViewModels;

namespace OpenGameVerse.App.Views;

public partial class CategoryInputDialog : SukiWindow
{
    public CategoryInputDialog()
    {
        InitializeComponent();
        DataContext = new CategoryInputDialogViewModel();
    }
}
