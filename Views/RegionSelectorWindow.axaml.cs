using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SCLogMate.Models;

namespace SCLogMate.Views;

public partial class RegionSelectorWindow : Window
{
    private Point _start;
    private bool _dragging;
    private Canvas? _canvas;
    private Avalonia.Controls.Shapes.Rectangle? _selectRect;
    private Button? _nextMonitorBtn;
    private TextBlock? _monitorLabel;

    private readonly List<RECT> _monitors = new();
    private int _monitorIndex = 0;
    private IntPtr _hwnd;

    public event Action<ScanRegion>? RegionSelected;

    public RegionSelectorWindow()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("DrawCanvas");
        _selectRect = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("SelectRect");
        _nextMonitorBtn = this.FindControl<Button>("NextMonitorBtn");
        _monitorLabel = this.FindControl<TextBlock>("MonitorLabel");

        KeyDown += OnKeyDown;
        if (_canvas != null)
        {
            _canvas.PointerPressed += OnPointerPressed;
            _canvas.PointerMoved += OnPointerMoved;
            _canvas.PointerReleased += OnPointerReleased;
        }

        if (_nextMonitorBtn != null)
        {
            _nextMonitorBtn.Click += (s, e) =>
            {
                e.Handled = true;
                SwitchMonitor();
            };
        }

        Opened += OnOpened;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is { Handle: not 0 } handle)
        {
            _hwnd = handle.Handle;
        }

        _monitors.Clear();
        var handles = new List<IntPtr>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr hMon, IntPtr _, ref RECT rc, IntPtr _) =>
            {
                handles.Add(hMon);
                _monitors.Add(rc);
                return true;
            }, IntPtr.Zero);

        if (_monitors.Count == 0) return;

        if (_nextMonitorBtn != null)
        {
            _nextMonitorBtn.IsVisible = _monitors.Count > 1;
        }

        ApplyCurrentMonitor();
    }

    public void SwitchMonitor()
    {
        if (_monitors.Count <= 1) return;
        _dragging = false;
        if (_selectRect != null) _selectRect.IsVisible = false;

        _monitorIndex = (_monitorIndex + 1) % _monitors.Count;
        ApplyCurrentMonitor();
    }

    private void ApplyCurrentMonitor()
    {
        if (_monitors.Count == 0 || _hwnd == IntPtr.Zero) return;

        var rc = _monitors[_monitorIndex];
        int w = rc.right - rc.left;
        int h = rc.bottom - rc.top;

        Position = new PixelPoint(rc.left, rc.top);
        double scaling = RenderScaling > 0 ? RenderScaling : 1.0;
        Width = w / scaling;
        Height = h / scaling;

        SetWindowPos(_hwnd, new IntPtr(-1) /*HWND_TOPMOST*/, rc.left, rc.top, w, h, 0x4050);

        if (_monitorLabel != null)
        {
            int next = ((_monitorIndex + 1) % _monitors.Count) + 1;
            _monitorLabel.Text = $"AUF MONITOR {next} WECHSELN (Aktuell: {_monitorIndex + 1}/{_monitors.Count} · {w}x{h})";
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.Tab || e.Key == Key.M || e.Key == Key.Space)
        {
            SwitchMonitor();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_canvas == null || _selectRect == null) return;
        var p = e.GetCurrentPoint(_canvas);
        if (!p.Properties.IsLeftButtonPressed) return;

        _start = p.Position;
        _dragging = true;
        _selectRect.IsVisible = true;
        Canvas.SetLeft(_selectRect, _start.X);
        Canvas.SetTop(_selectRect, _start.Y);
        _selectRect.Width = 0;
        _selectRect.Height = 0;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _canvas == null || _selectRect == null) return;
        var pos = e.GetCurrentPoint(_canvas).Position;

        double x = Math.Min(_start.X, pos.X);
        double y = Math.Min(_start.Y, pos.Y);
        double w = Math.Abs(pos.X - _start.X);
        double h = Math.Abs(pos.Y - _start.Y);

        Canvas.SetLeft(_selectRect, x);
        Canvas.SetTop(_selectRect, y);
        _selectRect.Width = w;
        _selectRect.Height = h;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging || _canvas == null || _selectRect == null) return;
        _dragging = false;

        var pos = e.GetCurrentPoint(_canvas).Position;
        double x = Math.Min(_start.X, pos.X);
        double y = Math.Min(_start.Y, pos.Y);
        double w = Math.Abs(pos.X - _start.X);
        double h = Math.Abs(pos.Y - _start.Y);

        if (w > 5 && h > 5)
        {
            var rc = _monitors.Count > 0 && _monitorIndex < _monitors.Count
                ? _monitors[_monitorIndex]
                : new RECT { left = 0, top = 0, right = 1920, bottom = 1080 };

            double scaling = RenderScaling > 0 ? RenderScaling : 1.0;

            int physX = rc.left + (int)Math.Round(x * scaling);
            int physY = rc.top + (int)Math.Round(y * scaling);
            int physW = (int)Math.Round(w * scaling);
            int physH = (int)Math.Round(h * scaling);

            var region = new ScanRegion
            {
                X = Math.Max(0, physX),
                Y = Math.Max(0, physY),
                Width = Math.Max(1, physW),
                Height = Math.Max(1, physH)
            };

            RegionSelected?.Invoke(region);
        }

        Close();
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }
}
