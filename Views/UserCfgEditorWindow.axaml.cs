using Avalonia.Controls;
using SCLogMate.ViewModels;

namespace SCLogMate.Views;

public partial class UserCfgEditorWindow : Window
{
    public UserCfgEditorWindow()
    {
        InitializeComponent();
    }

    public UserCfgEditorWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}
