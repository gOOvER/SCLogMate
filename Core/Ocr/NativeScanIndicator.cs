using System;
using System.Runtime.InteropServices;
using System.Threading;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Persistenter, klick-durchlässiger (WS_EX_TRANSPARENT) Win32-Overlay-Rahmen auf dem Bildschirm,
/// der anzeigt, welchen Bildschirmbereich der mobiGlas- oder Auftrags-Scanner erfasst.
/// </summary>
public sealed class NativeScanIndicator : IDisposable
{
    private readonly string _title;
    private readonly uint _defaultBorderColor; // BGR
    private readonly uint _flashBorderColor;   // BGR (Green: #4ADE80 -> 0x0080DE4A)

    private IntPtr _hwnd = IntPtr.Zero;
    private Thread? _uiThread;
    private volatile bool _isVisible = false;
    private volatile bool _isDisposed = false;
    private ScanRegion? _currentRegion;

    private readonly object _lock = new();
    private System.Threading.Timer? _flashTimer;
    private volatile bool _isFlashing = false;

    public bool IsVisible => _isVisible;

    public NativeScanIndicator(string title = "mobiGlas aUEC Scan", uint hexColorRgb = 0x22D3EE)
    {
        _title = title;
        // Konvertiere RGB zu BGR für GDI
        byte r = (byte)((hexColorRgb >> 16) & 0xFF);
        byte g = (byte)((hexColorRgb >> 8) & 0xFF);
        byte b = (byte)(hexColorRgb & 0xFF);
        _defaultBorderColor = (uint)((b << 16) | (g << 8) | r);
        _flashBorderColor = 0x0080DE4A; // #4ADE80 in BGR

        StartWindowThread();
    }

    private void StartWindowThread()
    {
        var readyEvent = new ManualResetEventSlim(false);
        _uiThread = new Thread(() =>
        {
            try
            {
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
                Logger.Error("NativeScanIndicator.Thread", ex);
                readyEvent.Set();
            }
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Start();

        readyEvent.Wait(2000);
    }

    private void CreateWindowInstance()
    {
        string className = "SCLogMate_ScanIndicatorClass_" + Guid.NewGuid().ToString("N");
        IntPtr hInstance = GetModuleHandle(IntPtr.Zero);

        IntPtr font = CreateFont(11, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr blackBrush = CreateSolidBrush(0x00000000); // Reines Schwarz als Color-Key (100% transparent)

        WndProc wndProcDelegate = (hWnd, msg, wParam, lParam) =>
        {
            switch (msg)
            {
                case 0x000F: // WM_PAINT
                    {
                        var hdc = BeginPaint(hWnd, out var ps);
                        GetClientRect(hWnd, out var rc);
                        int w = rc.right - rc.left;
                        int h = rc.bottom - rc.top;

                        // 1. Hintergrund mit Color-Key Schwarz füllen -> wird durch SetLayeredWindowAttributes 100% transparent!
                        FillRect(hdc, ref rc, blackBrush);

                        // 2. Rahmen zeichnen (Cyan oder Grün beim Flashen)
                        uint borderColor = _isFlashing ? _flashBorderColor : _defaultBorderColor;
                        IntPtr borderPen = CreatePen(0 /*PS_SOLID*/, 2, borderColor);
                        IntPtr oldPen = SelectObject(hdc, borderPen);
                        IntPtr oldBrush = SelectObject(hdc, GetStockObject(5 /*NULL_BRUSH*/));

                        Rectangle(hdc, 1, 1, w, h);

                        // 3. Titel-Badge oben links
                        SelectObject(hdc, font);
                        SetBkMode(hdc, 1 /*TRANSPARENT*/);
                        SetTextColor(hdc, borderColor);
                        var textRc = new RECT { left = 6, top = 4, right = w - 6, bottom = 22 };
                        DrawText(hdc, _isFlashing ? $"✓ {_title}" : _title, -1, ref textRc, 0x00000000 | 0x00000020);

                        SelectObject(hdc, oldPen);
                        SelectObject(hdc, oldBrush);
                        DeleteObject(borderPen);

                        EndPaint(hWnd, ref ps);
                        return IntPtr.Zero;
                    }

                case 0x0002: // WM_DESTROY
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        };

        var wndClass = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = 0x0001 | 0x0002,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
            hInstance = hInstance,
            lpszClassName = className
        };

        RegisterClassEx(ref wndClass);

        // WS_EX_TRANSPARENT = Klicks gehen direkt durch an das Spiel
        // WS_EX_LAYERED = Unterstützt Color-Key-Transparenz
        // WS_EX_NOACTIVATE & WS_EX_TOOLWINDOW = Kein Fokus, kein Taskleisten-Icon
        uint exStyle = 0x00000008 /*WS_EX_TOPMOST*/ |
                       0x00080000 /*WS_EX_LAYERED*/ |
                       0x00000020 /*WS_EX_TRANSPARENT*/ |
                       0x08000000 /*WS_EX_NOACTIVATE*/ |
                       0x00000080 /*WS_EX_TOOLWINDOW*/;

        uint style = 0x80000000 /*WS_POPUP*/;

        _hwnd = CreateWindowEx(
            exStyle, className, "SCLogMate Scan Frame", style,
            0, 0, 100, 100, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd != IntPtr.Zero)
        {
            // Schwarz (0x000000) ist transparent
            SetLayeredWindowAttributes(_hwnd, 0x00000000, 255, 0x00000001 /*LWA_COLORKEY*/);
        }
    }

    public void SetRegion(ScanRegion? region)
    {
        lock (_lock)
        {
            _currentRegion = region;
            if (_hwnd == IntPtr.Zero) return;

            if (region == null || !region.IsValid)
            {
                Hide();
                return;
            }

            SetWindowPos(_hwnd, new IntPtr(-1) /*HWND_TOPMOST*/,
                region.X, region.Y, region.Width, region.Height,
                (uint)(0x0010 | (_isVisible ? 0x0040 : 0)));

            InvalidateRect(_hwnd, IntPtr.Zero, true);
        }
    }

    public void Show()
    {
        lock (_lock)
        {
            _isVisible = true;
            if (_hwnd == IntPtr.Zero) return;

            if (_currentRegion != null && _currentRegion.IsValid)
            {
                SetWindowPos(_hwnd, new IntPtr(-1),
                    _currentRegion.X, _currentRegion.Y, _currentRegion.Width, _currentRegion.Height,
                    (uint)(0x0040 | 0x0010));
            }
            else
            {
                ShowWindow(_hwnd, 4 /*SW_SHOWNOACTIVATE*/);
            }
            InvalidateRect(_hwnd, IntPtr.Zero, true);
        }
    }

    public void Hide()
    {
        lock (_lock)
        {
            _isVisible = false;
            if (_hwnd != IntPtr.Zero)
            {
                ShowWindow(_hwnd, 0 /*SW_HIDE*/);
            }
        }
    }

    public void FlashGreen()
    {
        if (!_isVisible || _hwnd == IntPtr.Zero) return;

        _isFlashing = true;
        InvalidateRect(_hwnd, IntPtr.Zero, true);

        _flashTimer?.Dispose();
        _flashTimer = new System.Threading.Timer(_ =>
        {
            _isFlashing = false;
            if (_hwnd != IntPtr.Zero && _isVisible)
            {
                InvalidateRect(_hwnd, IntPtr.Zero, true);
            }
        }, null, 700, Timeout.Infinite);
    }

    public void Dispose()
    {
        _isDisposed = true;
        _flashTimer?.Dispose();
        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, 0x0010 /*WM_CLOSE*/, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
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
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] private static extern bool TranslateMessage([In] ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int nExitCode);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint crColor);
    [DllImport("gdi32.dll")] private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);
    [DllImport("gdi32.dll")] private static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
    [DllImport("gdi32.dll")] private static extern IntPtr GetStockObject(int fnObject);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);
    [DllImport("gdi32.dll")] private static extern int SetBkMode(IntPtr hdc, int iBkMode);
    [DllImport("gdi32.dll")] private static extern uint SetTextColor(IntPtr hdc, uint crColor);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int DrawText(IntPtr hDC, string lpchText, int nCount, ref RECT lpRect, uint uFormat);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateFont(
        int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
        uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
        uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }
}
