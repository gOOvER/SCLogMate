using Avalonia.Controls;
using SCLogMate.ViewModels;

namespace SCLogMate.Views;

public partial class KeybindBackupWindow : Window
{
    public KeybindBackupWindow()
    {
        InitializeComponent();
    }

    public KeybindBackupWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}
