using Avalonia.Controls;
using SCLogMate.ViewModels;

namespace SCLogMate.Views;

public partial class CloudBackupWindow : Window
{
    public CloudBackupWindow()
    {
        InitializeComponent();
    }

    public CloudBackupWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}
