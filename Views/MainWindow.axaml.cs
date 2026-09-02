using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SCLogMate.Services;

namespace SCLogMate.Views;

public partial class MainWindow : Window
{
    public static bool IsExplicitExit { get; set; } = false;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        UiServices.TopLevel = this;   // für Datei-Dialoge im ViewModel
        Closing += OnMainWindowClosing;
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (IsExplicitExit) return;

        var vm = DataContext as ViewModels.MainViewModel;
        if (vm?.MinimizeToTrayOnClose == true)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            IsExplicitExit = true;
            (Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
    }

    void OnGridDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm && vm.LookupCommand.CanExecute(null))
            vm.LookupCommand.Execute(null);
    }

    void OnBalanceKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter && DataContext is ViewModels.MainViewModel vm)
        {
            if (vm.SetBalanceCommand.CanExecute(null))
                vm.SetBalanceCommand.Execute(null);
        }
    }
}
