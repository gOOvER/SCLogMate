using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLogReader.Core;
using SCLogReader.ViewModels;

namespace SCLogReader.Views;

public partial class RsScanOverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int idx, int val);

    private AppSettings? _settings;
    private IntPtr _hwnd;

    public RsScanOverlayWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
            if (DataContext is MainViewModel vm)
            {
                vm.IsRsOverlayActive = false;
            }
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
        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);
    }

    public void InitSettings(AppSettings settings)
    {
        _settings = settings;

        if (_settings.RsOverlayPositionX > 0 || _settings.RsOverlayPositionY > 0)
        {
            Position = new PixelPoint((int)_settings.RsOverlayPositionX, (int)_settings.RsOverlayPositionY);
        }
        else
        {
            Position = new PixelPoint(140, 140);
        }

        Opacity = Math.Clamp(_settings.OverlayOpacity, 0.3, 1.0);

        PositionChanged += (_, _) =>
        {
            if (_settings != null)
            {
                _settings.RsOverlayPositionX = Position.X;
                _settings.RsOverlayPositionY = Position.Y;
                Settings.Save(_settings);
            }
        };

        ApplyWindowStyles();
    }

    private void OnBorderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsRsOverlayActive = false;
        }
        else
        {
            Hide();
        }
    }
}
