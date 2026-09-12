using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using SCLogMate.Core.Photino;
using SCLogMate.Models;

namespace SCLogMate.Core.Overlays;

/// <summary>
/// Nativer Win32 Always-On-Top Radar-Overlay für den Star Citizen RS Signal Scanner.
/// Zeigt die aktuell per OCR erfasste RS-Signatur, vorhergesagte Mineralien/Schiffe,
/// Ausbeutewerte und Vertrauensquoten direkt über dem Vollbildspiel an.
/// </summary>
public sealed class NativeRsOverlay : IDisposable
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

    private const uint LWA_ALPHA = 0x00000002;

    private const int WM_PAINT = 0x000F;
    private const int WM_DESTROY = 0x0002;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 2;

    public const int OverlayWidth = 380;
    public const int OverlayHeight = 150;

    private IntPtr _hwnd = IntPtr.Zero;
    private Thread? _uiThread;
    private volatile bool _isVisible = false;
    private volatile bool _isDisposed = false;
    private readonly object _lock = new();

    private WndProc? _wndProcDelegate;

    // RS Signal State
    private string _rsValueText = "STANDBY";
    private string _targetName = "Warte auf Radar-Signal...";
    private string _tierRarityText = "Bereit zum Scannen (V-Taste / Ping)";
    private string _clusterValueText = "— aUEC";
    private string _matchDetailText = "Tippe Scan-Taste oder aktiviere Auto-Scan";
    private uint _themeColor = 0x0038BDF8u; // Amber/Cyan in BGR (0x00F8BD38)
    private bool _hasTarget = false;

    public bool IsVisible => _isVisible;

    public event Action<bool>? VisibilityChanged;

    public NativeRsOverlay()
    {
        StartWindowThread();
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
                Logger.Error("NativeRsOverlay.Thread", ex);
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
        string className = "SCLogMate_RsOverlayClass_" + Guid.NewGuid().ToString("N");
        IntPtr hInstance = GetModuleHandle(IntPtr.Zero);

        _wndProcDelegate = (hWnd, msg, wParam, lParam) =>
        {
            switch (msg)
            {
                case WM_PAINT:
                    {
                        var hdc = BeginPaint(hWnd, out var ps);
                        PaintRsHud(hdc);
                        EndPaint(hWnd, ref ps);
                        return IntPtr.Zero;
                    }

                case WM_LBUTTONDOWN:
                    {
                        int x = (short)(lParam.ToInt32() & 0xFFFF);
                        int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

                        // Close button clicked (top right 355..375, 5..25)
                        if (x >= OverlayWidth - 25 && x <= OverlayWidth - 5 && y >= 5 && y <= 25)
                        {
                            SetVisible(false);
                            return IntPtr.Zero;
                        }

                        // Drag window
                        ReleaseCapture();
                        SendMessage(hWnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
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
        int posX = Math.Max(20, screenW - OverlayWidth - 40);
        int posY = 210;

        int exStyle = WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED;

        _hwnd = CreateWindowEx(
            exStyle,
            className,
            "SCLogMate RS Radar",
            WS_POPUP,
            posX, posY, OverlayWidth, OverlayHeight,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd != IntPtr.Zero)
        {
            SetLayeredWindowAttributes(_hwnd, 0, 240, LWA_ALPHA);
        }
    }

    public void UpdateRsMatch(RsMatchDto? match)
    {
        lock (_lock)
        {
            if (match != null && !string.IsNullOrWhiteSpace(match.ResourceName))
            {
                _hasTarget = true;
                _rsValueText = $"RS {match.ScannedRs:N0}";
                _targetName = match.ResourceName;
                _tierRarityText = $"Tier {match.Tier} · {match.Rarity.ToUpperInvariant()} · {match.Nodes}x Cluster";
                _clusterValueText = match.EstimatedClusterValue > 0 ? $"~{match.EstimatedClusterValue:N0} aUEC" : "—";
                _matchDetailText = $"Genauigkeit: {(100 - match.ErrorPct):0.#}% · {match.Method.ToUpperInvariant()}";
                _themeColor = match.Tier == "S" ? 0x0080DE4Au : (match.Tier == "A" ? 0x000BB5F5u : 0x00EED322u);
            }
            else
            {
                _hasTarget = false;
                _rsValueText = "STANDBY";
                _targetName = "Warte auf Radar-Signal...";
                _tierRarityText = "Bereit zum Scannen (V-Taste / Ping)";
                _clusterValueText = "—";
                _matchDetailText = "In-Game RS-Signal anpingen";
                _themeColor = 0x009E948Bu;
            }
        }

        if (_hwnd != IntPtr.Zero && _isVisible)
        {
            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }
    }

    private void PaintRsHud(IntPtr hdc)
    {
        IntPtr memDC = CreateCompatibleDC(hdc);
        IntPtr memBmp = CreateCompatibleBitmap(hdc, OverlayWidth, OverlayHeight);
        IntPtr oldBmp = SelectObject(memDC, memBmp);

        // Hintergrund (Dunkelblau-Schwarz: 0x00160B05)
        IntPtr bgBrush = CreateSolidBrush(0x00160B05);
        IntPtr borderPen = CreatePen(0, 1, _themeColor);
        SelectObject(memDC, bgBrush);
        SelectObject(memDC, borderPen);

        RoundRect(memDC, 1, 1, OverlayWidth - 1, OverlayHeight - 1, 12, 12);

        // Header Sub-Leiste
        IntPtr headerBrush = CreateSolidBrush(0x0024160E);
        var headerRc = new RECT { left = 2, top = 2, right = OverlayWidth - 2, bottom = 26 };
        FillRect(memDC, ref headerRc, headerBrush);
        DeleteObject(headerBrush);

        IntPtr fontSmall = CreateFont(12, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontBig = CreateFont(18, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontMono = CreateFont(13, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Consolas");

        SetBkMode(memDC, 1 /*TRANSPARENT*/);

        // Header: 🛰 RS RADAR DECODER + Close Button
        SelectObject(memDC, fontSmall);
        SetTextColor(memDC, _themeColor);
        var hdrRc = new RECT { left = 10, top = 4, right = 250, bottom = 24 };
        DrawText(memDC, "🛰 RS RADAR DECODER", -1, ref hdrRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Status / RS Value Pill
        SetTextColor(memDC, 0x00FCF6F0);
        var pillRc = new RECT { left = 240, top = 4, right = OverlayWidth - 28, bottom = 24 };
        DrawText(memDC, _rsValueText, -1, ref pillRc, 0x00000002 | 0x00000004 | 0x00000020);

        // Close Button [✕]
        SetTextColor(memDC, 0x00A0A0A0);
        var closeRc = new RECT { left = OverlayWidth - 24, top = 4, right = OverlayWidth - 6, bottom = 24 };
        DrawText(memDC, "✕", -1, ref closeRc, 0x00000001 | 0x00000004 | 0x00000020);

        // Trennlinie
        IntPtr divPen = CreatePen(0, 1, 0x003D2616);
        SelectObject(memDC, divPen);
        MoveToEx(memDC, 2, 26, IntPtr.Zero);
        LineTo(memDC, OverlayWidth - 2, 26);
        DeleteObject(divPen);

        // Ziel-Name / Hero Titel
        SelectObject(memDC, fontBig);
        SetTextColor(memDC, _hasTarget ? 0x00FCF6F0u : 0x00A0948Bu);
        var targetRc = new RECT { left = 12, top = 34, right = OverlayWidth - 12, bottom = 60 };
        DrawText(memDC, _targetName, -1, ref targetRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Tier & Rarity Subtitle
        SelectObject(memDC, fontSmall);
        SetTextColor(memDC, _themeColor);
        var tierRc = new RECT { left = 12, top = 62, right = OverlayWidth - 12, bottom = 82 };
        DrawText(memDC, _tierRarityText, -1, ref tierRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Trennlinie vor Footer
        IntPtr footPen = CreatePen(0, 1, 0x002A1C12);
        SelectObject(memDC, footPen);
        MoveToEx(memDC, 8, 88, IntPtr.Zero);
        LineTo(memDC, OverlayWidth - 8, 88);
        DeleteObject(footPen);

        // Footer: Geschätzter Clusterwert & Genauigkeit
        SelectObject(memDC, fontMono);
        SetTextColor(memDC, 0x0080DE4A); // Grün
        var valRc = new RECT { left = 12, top = 94, right = OverlayWidth - 12, bottom = 114 };
        DrawText(memDC, $"Geschätzter Wert: {_clusterValueText}", -1, ref valRc, 0x00000000 | 0x00000004 | 0x00000020);

        SelectObject(memDC, fontSmall);
        SetTextColor(memDC, 0x009E948B);
        var detRc = new RECT { left = 12, top = 118, right = OverlayWidth - 12, bottom = 138 };
        DrawText(memDC, _matchDetailText, -1, ref detRc, 0x00000000 | 0x00000004 | 0x00000020);

        BitBlt(hdc, 0, 0, OverlayWidth, OverlayHeight, memDC, 0, 0, 0x00CC0020 /*SRCCOPY*/);

        SelectObject(memDC, oldBmp);
        DeleteObject(memBmp);
        DeleteDC(memDC);
        DeleteObject(bgBrush);
        DeleteObject(borderPen);
        DeleteObject(fontSmall);
        DeleteObject(fontBig);
        DeleteObject(fontMono);
    }

    public void SetVisible(bool visible)
    {
        if (_hwnd == IntPtr.Zero) return;
        _isVisible = visible;

        if (visible)
        {
            ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }
        else
        {
            ShowWindow(_hwnd, SW_HIDE);
        }

        VisibilityChanged?.Invoke(visible);
    }

    public void Toggle()
    {
        SetVisible(!_isVisible);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

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
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
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
    [DllImport("gdi32.dll")] private static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr lpPoint);
    [DllImport("gdi32.dll")] private static extern bool LineTo(IntPtr hdc, int x, int y);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFont(
        int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
        uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
        uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality,
        uint fdwPitchAndFamily, string lpszFace);

    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int DrawText(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, uint format);

    #endregion
}
