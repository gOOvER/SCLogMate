using Avalonia.Controls;
using SCLogMate.ViewModels;

namespace SCLogMate.Views;

public partial class StarmapWindow : Window
{
    public StarmapWindow()
    {
        InitializeComponent();
    }

    public StarmapWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}
