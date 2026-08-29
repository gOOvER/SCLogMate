using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SCLogReader.ViewModels;
using SCLogReader.Views;

namespace SCLogReader;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel() };

        base.OnFrameworkInitializationCompleted();
    }

    void TrayOpen(object? sender, System.EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d && d.MainWindow is { } w)
        {
            w.Show();
            w.WindowState = Avalonia.Controls.WindowState.Normal;
            w.Activate();
            w.Focus();
        }
    }

    void TrayExit(object? sender, System.EventArgs e)
    {
        MainWindow.IsExplicitExit = true;
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
