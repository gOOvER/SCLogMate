using Avalonia.Controls;
using SCLogReader.ViewModels;

namespace SCLogReader.Views;

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
