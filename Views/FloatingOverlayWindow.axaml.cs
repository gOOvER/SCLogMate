using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLogMate.Core;
using SCLogMate.ViewModels;

namespace SCLogMate.Views;

public partial class FloatingOverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int idx, int val);

    private AppSettings? _settings;
    private IntPtr _hwnd;
    private readonly Avalonia.Threading.DispatcherTimer _settingsSaveTimer;

    public FloatingOverlayWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        _settingsSaveTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _settingsSaveTimer.Tick += (_, _) =>
        {
            _settingsSaveTimer.Stop();
            if (_settings != null) Settings.Save(_settings);
        };
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is { Handle: not 0 } handle)
        {
            _hwnd = handle.Handle;
            ApplyWindowStyles();
        }
    }

    public void ApplyWindowStyles()
    {
        if (_hwnd == IntPtr.Zero || !OperatingSystem.IsWindows()) return;
        int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;

        if (_settings?.OverlayClickThrough == true)
            ex |= WS_EX_TRANSPARENT;
        else
            ex &= ~WS_EX_TRANSPARENT;

        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);
    }

    public void InitSettings(AppSettings settings)
    {
        _settings = settings;

        if (_settings.OverlayPositionX > 0 || _settings.OverlayPositionY > 0)
        {
            Position = new PixelPoint((int)_settings.OverlayPositionX, (int)_settings.OverlayPositionY);
        }

        Opacity = Math.Clamp(_settings.OverlayOpacity, 0.3, 1.0);

        PositionChanged += (_, _) =>
        {
            if (_settings != null && !_settings.OverlayLocked)
            {
                _settings.OverlayPositionX = Position.X;
                _settings.OverlayPositionY = Position.Y;
                _settingsSaveTimer.Stop();
                _settingsSaveTimer.Start();
            }
        };

        ApplyWindowStyles();
    }

    private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_settings?.OverlayLocked == true) return;

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsOverlayActive = false;
        }
        else
        {
            Hide();
        }
    }
}
