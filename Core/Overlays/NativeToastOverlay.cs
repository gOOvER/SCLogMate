using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using SCLogMate.Models;

namespace SCLogMate.Core.Overlays;

public class NativeToastItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Icon { get; set; } = "ℹ️";
    public string Header { get; set; } = "SYSTEM";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public uint BorderColor { get; set; } = 0x00EED322; // Cyan (BGR)
    public uint IconBgColor { get; set; } = 0x0024160E; // Dark Navy
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public double Opacity { get; set; } = 1.0;
}

/// <summary>
/// Nativer Win32 Always-On-Top Toast-Overlay für In-Game Benachrichtigungen
/// über dem Star Citizen Spielfenster (Aufzüge, Schiffszerstörung, Aufträge, Baupläne, Ruf).
/// Unterstützt Auto-Stacking, sanftes Ein-/Ausblenden und automatischen Timer-Ablauf.
/// </summary>
public sealed class NativeToastOverlay : IDisposable
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint WS_POPUP = 0x80000000;

    private const int SW_HIDE = 0;
    private const int SW_SHOWNOACTIVATE = 4;

    private const uint LWA_COLORKEY = 0x00000001;
    private const uint LWA_ALPHA = 0x00000002;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    private const int WM_PAINT = 0x000F;
    private const int WM_DESTROY = 0x0002;

    public const int ToastWidth = 560;
    public const int SingleToastHeight = 86;
    public const int ToastSpacing = 8;
    public const int MaxVisibleToasts = 3;

    private IntPtr _hwnd = IntPtr.Zero;
    private Thread? _uiThread;
    private volatile bool _isDisposed = false;
    private readonly object _lock = new();

    private WndProc? _wndProcDelegate;

    private readonly List<NativeToastItem> _activeToasts = new();
    private System.Threading.Timer? _animTimer;

    public NativeToastOverlay()
    {
        StartWindowThread();
        _animTimer = new System.Threading.Timer(OnAnimTick, null, 100, 50);
    }

    private void StartWindowThread()
    {
        var readyEvent = new ManualResetEventSlim(false);
        _uiThread = new Thread(() =>
        {
            try
            {
                try { SetThreadDpiAwarenessContext((IntPtr)(-4)); } catch { }
                CreateWindowInstance();
                readyEvent.Set();

                while (!_isDisposed && GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("NativeToastOverlay.Thread", ex);
                readyEvent.Set();
            }
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Start();

        readyEvent.Wait(2500);
    }

    private void CreateWindowInstance()
    {
        string className = "SCLogMate_ToastOverlayClass_" + Guid.NewGuid().ToString("N");
        IntPtr hInstance = GetModuleHandle(IntPtr.Zero);

        _wndProcDelegate = (hWnd, msg, wParam, lParam) =>
        {
            switch (msg)
            {
                case WM_PAINT:
                    {
                        var hdc = BeginPaint(hWnd, out var ps);
                        PaintToasts(hdc);
                        EndPaint(hWnd, ref ps);
                        return IntPtr.Zero;
                    }

                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        };

        var wndClass = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = 0x0002 | 0x0001,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = hInstance,
            hCursor = LoadCursor(IntPtr.Zero, (IntPtr)32512),
            lpszClassName = className
        };

        RegisterClassEx(ref wndClass);

        int screenW = GetSystemMetrics(0);
        int posX = Math.Max(20, (screenW - ToastWidth) / 2);
        int posY = 20;

        var s = Settings.Load();
        if (s.ToastPositionX >= 0 && s.ToastPositionY >= 0)
        {
            posX = (int)s.ToastPositionX;
            posY = (int)s.ToastPositionY;
        }

        int totalH = (SingleToastHeight + ToastSpacing) * MaxVisibleToasts;
        int exStyle = WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TRANSPARENT;

        _hwnd = CreateWindowEx(
            exStyle,
            className,
            "SCLogMate Toasts",
            WS_POPUP,
            posX, posY, ToastWidth, totalH,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd != IntPtr.Zero)
        {
            SetLayeredWindowAttributes(_hwnd, 0x00000000, 250, LWA_COLORKEY | LWA_ALPHA);
        }
    }

    public void ShowToast(string icon, string header, string title, string subtitle = "", uint bgrColor = 0x00EED322)
    {
        var item = new NativeToastItem
        {
            Icon = icon,
            Header = header.ToUpperInvariant(),
            Title = title,
            Subtitle = subtitle,
            BorderColor = bgrColor,
            CreatedAt = DateTime.UtcNow,
            Opacity = 1.0
        };

        lock (_lock)
        {
            _activeToasts.Insert(0, item);
            while (_activeToasts.Count > MaxVisibleToasts)
            {
                _activeToasts.RemoveAt(_activeToasts.Count - 1);
            }
        }

        if (_hwnd != IntPtr.Zero)
        {
            int currentH = Math.Max(1, _activeToasts.Count * (SingleToastHeight + ToastSpacing));
            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, ToastWidth, currentH, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
            ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }
    }

    private void OnAnimTick(object? state)
    {
        if (_isDisposed || _hwnd == IntPtr.Zero) return;

        bool needsRepaint = false;
        bool hasVisible = false;
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                var toast = _activeToasts[i];
                double elapsed = (now - toast.CreatedAt).TotalSeconds;

                if (elapsed >= 4.5)
                {
                    _activeToasts.RemoveAt(i);
                    needsRepaint = true;
                }
                else if (elapsed >= 3.8)
                {
                    // Fade out in den letzten 0.7 Sekunden
                    toast.Opacity = Math.Clamp((4.5 - elapsed) / 0.7, 0.0, 1.0);
                    needsRepaint = true;
                }
            }

            hasVisible = _activeToasts.Count > 0;
        }

        if (needsRepaint && _hwnd != IntPtr.Zero)
        {
            int currentH = Math.Max(1, _activeToasts.Count * (SingleToastHeight + ToastSpacing));
            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, ToastWidth, currentH, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }

        if (!hasVisible && _hwnd != IntPtr.Zero)
        {
            ShowWindow(_hwnd, SW_HIDE);
        }
    }

    private void PaintToasts(IntPtr hdc)
    {
        List<NativeToastItem> copy;
        lock (_lock)
        {
            copy = new List<NativeToastItem>(_activeToasts);
        }

        if (copy.Count == 0) return;

        int totalH = Math.Max(SingleToastHeight, copy.Count * (SingleToastHeight + ToastSpacing));

        IntPtr memDC = CreateCompatibleDC(hdc);
        IntPtr memBmp = CreateCompatibleBitmap(hdc, ToastWidth, totalH);
        IntPtr oldBmp = SelectObject(memDC, memBmp);

        // Hintergrund Transparent / Leer (Schwarz 0x00000000 wird per LWA_COLORKEY 100% transparent!)
        IntPtr emptyBrush = CreateSolidBrush(0x00000000);
        var fullRc = new RECT { left = 0, top = 0, right = ToastWidth, bottom = totalH };
        FillRect(memDC, ref fullRc, emptyBrush);
        DeleteObject(emptyBrush);

        IntPtr fontHeader = CreateFont(13, 0, 0, 0, 800, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontTitle = CreateFont(20, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontSub = CreateFont(14, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontIcon = CreateFont(32, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI Emoji");

        SetBkMode(memDC, 1 /*TRANSPARENT*/);

        for (int i = 0; i < copy.Count; i++)
        {
            var item = copy[i];
            int yOffset = i * (SingleToastHeight + ToastSpacing);

            // Toast-Box (Dunkles Navy-Schwarz: 0x001A1009)
            IntPtr boxBrush = CreateSolidBrush(0x001A1009);
            IntPtr boxPen = CreatePen(0, 2, item.BorderColor);

            IntPtr oldB = SelectObject(memDC, boxBrush);
            IntPtr oldP = SelectObject(memDC, boxPen);

            RoundRect(memDC, 2, yOffset + 2, ToastWidth - 2, yOffset + SingleToastHeight - 2, 14, 14);

            // Linkes Wappen-Icon Schild (60x60)
            int iconBoxX = 14;
            int iconBoxY = yOffset + 13;
            int iconBoxSize = 60;
            IntPtr iconBg = CreateSolidBrush(0x002A1C14);
            IntPtr iconPen = CreatePen(0, 1, item.BorderColor);
            SelectObject(memDC, iconBg);
            SelectObject(memDC, iconPen);
            RoundRect(memDC, iconBoxX, iconBoxY, iconBoxX + iconBoxSize, iconBoxY + iconBoxSize, 12, 12);
            DeleteObject(iconBg);
            DeleteObject(iconPen);

            // Icon Text
            SelectObject(memDC, fontIcon);
            SetTextColor(memDC, item.BorderColor);
            var iconRc = new RECT { left = iconBoxX, top = iconBoxY + 4, right = iconBoxX + iconBoxSize, bottom = iconBoxY + iconBoxSize };
            DrawText(memDC, item.Icon, -1, ref iconRc, 0x00000001 | 0x00000004 | 0x00000020);

            // Text-Bereich
            int textX = iconBoxX + iconBoxSize + 16;
            int textR = ToastWidth - 18;

            // 1. Header (z. B. "AUFTRAG ERFOLGREICH" in Amber / Cyan / Green)
            SelectObject(memDC, fontHeader);
            SetTextColor(memDC, item.BorderColor);
            var hRc = new RECT { left = textX, top = yOffset + 11, right = textR, bottom = yOffset + 27 };
            DrawText(memDC, item.Header, -1, ref hRc, 0x00000000 | 0x00000004 | 0x00000020);

            // 2. Title (z. B. "Covalex Cargo Transport" in Weiß)
            SelectObject(memDC, fontTitle);
            SetTextColor(memDC, 0x00FFFFFF);
            var tRc = new RECT { left = textX, top = yOffset + 28, right = textR, bottom = yOffset + 54 };
            DrawText(memDC, item.Title, -1, ref tRc, 0x00000000 | 0x00000004 | 0x00000020);

            // 3. Subtitle (z. B. "+45.000 aUEC Belohnung")
            if (!string.IsNullOrWhiteSpace(item.Subtitle))
            {
                SelectObject(memDC, fontSub);
                SetTextColor(memDC, 0x0080DE4A); // Grün
                var sRc = new RECT { left = textX, top = yOffset + 55, right = textR, bottom = yOffset + 75 };
                DrawText(memDC, item.Subtitle, -1, ref sRc, 0x00000000 | 0x00000004 | 0x00000020);
            }

            SelectObject(memDC, oldB);
            SelectObject(memDC, oldP);
            DeleteObject(boxBrush);
            DeleteObject(boxPen);
        }

        BitBlt(hdc, 0, 0, ToastWidth, totalH, memDC, 0, 0, 0x00CC0020 /*SRCCOPY*/);

        SelectObject(memDC, oldBmp);
        DeleteObject(memBmp);
        DeleteDC(memDC);
        DeleteObject(fontHeader);
        DeleteObject(fontTitle);
        DeleteObject(fontSub);
        DeleteObject(fontIcon);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _animTimer?.Dispose();

        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, 0x0010 /*WM_CLOSE*/, IntPtr.Zero, IntPtr.Zero);
            _hwnd = IntPtr.Zero;
        }
    }

    #region Win32 P/Invoke

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x, pt_y;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int nExitCode);
    [DllImport("user32.dll")] private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] private static extern bool TranslateMessage([In] ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
    [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll")] private static extern bool SetThreadDpiAwarenessContext(IntPtr dpiContext);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern bool RoundRect(IntPtr hdc, int left, int top, int right, int bottom, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);
    [DllImport("gdi32.dll")] private static extern int SetBkMode(IntPtr hdc, int iBkMode);
    [DllImport("gdi32.dll")] private static extern uint SetTextColor(IntPtr hdc, uint crColor);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateFont(
        int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
        uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
        uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality,
        uint fdwPitchAndFamily, string lpszFace);

    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int DrawText(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, uint format);

    #endregion
}
