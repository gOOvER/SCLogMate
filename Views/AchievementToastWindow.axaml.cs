using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SCLogMate.Core;
using SCLogMate.Models;

namespace SCLogMate.Views;

public partial class AchievementToastWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int idx, int val);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    public ObservableCollection<AchievementToastData> ActiveToasts { get; } = new();

    private readonly ConcurrentDictionary<Guid, DispatcherTimer> _itemTimers = new();
    private readonly ConcurrentDictionary<Guid, DispatcherTimer> _fadeTimers = new();
    private AppSettings? _settings;
    private IntPtr _hwnd;
    private bool _isDragging;

    public AchievementToastWindow()
    {
        InitializeComponent();
        DataContext = this;
        Opened += OnOpened;
    }

    public void InitSettings(AppSettings settings)
    {
        _settings = settings;

        if (_settings.ToastPositionX >= 0 || _settings.ToastPositionY >= 0)
        {
            Position = new PixelPoint((int)Math.Max(0, _settings.ToastPositionX), (int)Math.Max(0, _settings.ToastPositionY));
        }
        else
        {
            CenterTopOnScreen();
        }

        PositionChanged += (_, _) =>
        {
            if (_settings != null && !_isDragging)
            {
                _settings.ToastPositionX = Position.X;
                _settings.ToastPositionY = Position.Y;
                Settings.Save(_settings);
            }
        };
    }

    private void CenterTopOnScreen()
    {
        try
        {
            var screen = Screens?.Primary ?? (Screens?.All.Count > 0 ? Screens.All[0] : null);
            if (screen != null)
            {
                var bounds = screen.Bounds;
                int centerX = bounds.X + (bounds.Width - 460) / 2;
                int topY = bounds.Y + 60;
                Position = new PixelPoint(centerX, topY);
            }
        }
        catch { /* ignore */ }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is { Handle: not 0 } handle)
        {
            _hwnd = handle.Handle;
            int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }
    }

    public void ShowToast(AchievementToastData toast, double durationSeconds = 5.5)
    {
        // Maximal 5 gleichzeitige Banners anzeigen
        if (ActiveToasts.Count >= 5)
        {
            var oldest = ActiveToasts[0];
            RemoveToastItem(oldest);
        }

        ActiveToasts.Add(toast);

        // Sound-Effekt abspielen, falls in den Einstellungen aktiviert
        if (_settings?.ToastSoundEnabled == true && OperatingSystem.IsWindows())
        {
            try
            {
                MessageBeep(0x00000040 /* MB_ICONASTERISK */);
            }
            catch { /* ignore */ }
        }

        if (!IsVisible)
        {
            Show();
        }

        // Sicherstellen, dass das Fenster ganz oben liegt ohne Fokus zu klauen
        if (_hwnd != IntPtr.Zero)
        {
            SetWindowPos(_hwnd, new IntPtr(-1) /*HWND_TOPMOST*/, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040 | 0x0010);
        }

        // Lebensdauer-Timer für dieses spezifische Toast-Item
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(2.0, durationSeconds)) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _itemTimers.TryRemove(toast.Id, out _);
            FadeOutItem(toast);
        };
        _itemTimers[toast.Id] = timer;
        timer.Start();
    }

    private void FadeOutItem(AchievementToastData toast)
    {
        var fade = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _fadeTimers[toast.Id] = fade;
        fade.Tick += (_, _) =>
        {
            if (toast.Opacity > 0.08)
            {
                toast.Opacity -= 0.10;
            }
            else
            {
                fade.Stop();
                _fadeTimers.TryRemove(toast.Id, out _);
                RemoveToastItem(toast);
            }
        };
        fade.Start();
    }

    private void RemoveToastItem(AchievementToastData toast)
    {
        if (_itemTimers.TryRemove(toast.Id, out var t))
        {
            t.Stop();
        }

        if (_fadeTimers.TryRemove(toast.Id, out var fade))
        {
            fade.Stop();
        }

        ActiveToasts.Remove(toast);

        if (ActiveToasts.Count == 0)
        {
            Hide();
        }
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            BeginMoveDrag(e);
            _isDragging = false;

            if (_settings != null)
            {
                _settings.ToastPositionX = Position.X;
                _settings.ToastPositionY = Position.Y;
                Settings.Save(_settings);
            }
        }
    }

    private void OnCloseItemClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AchievementToastData toast })
        {
            RemoveToastItem(toast);
        }
    }
}
