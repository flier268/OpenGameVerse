using OpenGameVerse.App.ViewModels;
using SukiUI.Controls;

namespace OpenGameVerse.App.Views;

public partial class CategoryInputDialog : SukiWindow
{
    public CategoryInputDialog()
    {
        InitializeComponent();
        DataContext = new CategoryInputDialogViewModel();
    }
}
