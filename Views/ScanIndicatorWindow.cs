using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SCLogMate.Models;

namespace SCLogMate.Views;

/// <summary>
/// Persistenter, klick-durchlässiger (WS_EX_TRANSPARENT) Indikator-Rahmen auf dem Bildschirm,
/// der anzeigt, wo der mobiGlas aUEC-Scanner hinschaut.
/// </summary>
public class ScanIndicatorWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static readonly IBrush CyanBrush = new SolidColorBrush(Color.Parse("#22D3EE"));
    private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#4ADE80"));

    private readonly string _title;
    private readonly IBrush _accentBrush;
    private readonly Border _border;
    private readonly TextBlock _label;
    private IntPtr _hwnd;
    private int _px, _py, _pw, _ph;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int idx, int val);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public ScanIndicatorWindow() : this("aUEC Scan", "#22D3EE") { }

    public ScanIndicatorWindow(string title, string hexColor)
    {
        _title = title;
        SystemDecorations = SystemDecorations.None;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        Focusable = false;

        var brush = new SolidColorBrush(Color.Parse(hexColor));
        _accentBrush = brush;
        var c = Color.Parse(hexColor);
        var bgBrush = new SolidColorBrush(Color.FromArgb(18, c.R, c.G, c.B));

        _label = new TextBlock
        {
            Text = title,
            FontSize = 10,
            Foreground = brush,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(4, -15, 0, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };

        _border = new Border
        {
            BorderBrush = brush,
            BorderThickness = new Thickness(2),
            Background = bgBrush,
            CornerRadius = new CornerRadius(3),
            Child = _label
        };

        Content = _border;
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (TryGetPlatformHandle() is { Handle: not 0 } handle)
        {
            _hwnd = handle.Handle;
            int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            ApplyPhysicalBounds();
        }
    }

    public void SetRegion(ScanRegion? region)
    {
        if (region == null || !region.IsValid)
        {
            Hide();
            return;
        }

        _px = region.X;
        _py = region.Y;
        _pw = region.Width;
        _ph = region.Height;

        Position = new PixelPoint(_px, _py);
        double scaling = RenderScaling > 0 ? RenderScaling : 1.0;
        Width = _pw / scaling;
        Height = _ph / scaling;

        if (IsVisible)
        {
            ApplyPhysicalBounds();
        }
    }

    public void FlashGreen()
    {
        // Niemals einblenden, wenn die Box vom Nutzer nicht explizit eingeschaltet wurde
        if (!IsVisible) return;

        _border.BorderBrush = GreenBrush;
        _label.Foreground = GreenBrush;
        _label.Text = $"✓ {_title}";

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _border.BorderBrush = _accentBrush;
            _label.Foreground = _accentBrush;
            _label.Text = _title;
        };
        timer.Start();
    }

    private void ApplyPhysicalBounds()
    {
        if (_hwnd == IntPtr.Zero || _pw <= 0 || _ph <= 0) return;
        SetWindowPos(_hwnd, new IntPtr(-1), _px, _py, _pw, _ph, 0x4050);
    }
}
