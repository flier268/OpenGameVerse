using Avalonia.Controls;
using SukiUI.Controls;

namespace OpenGameVerse.App.Views;

public partial class CategoryManagerWindow : SukiWindow
{
    public CategoryManagerWindow()
    {
        InitializeComponent();
    }

    public void SetOwner(WindowBase? owner)
    {
        Owner = owner;
    }
}
