using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using SCLogMate.Core.Photino;
using SCLogMate.Models;

namespace SCLogMate.Core.Overlays;

/// <summary>
/// Nativer Win32 Always-On-Top Mini-HUD Overlay für das Star Citizen Vollbild-Spiel.
/// Zeigt Live-Kontostand, Session-Netto-Verdienst, Server/Shard-Info, Ping und Standort
/// in einem kompakten, verschiebbaren Glassmorphism-HUD an.
/// Unterstützt Alt+H Hotkey, Click-Through-Modus und Positions-Speicherung.
/// </summary>
public sealed class NativeMiniHudOverlay : IDisposable
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;

    private const int SW_HIDE = 0;
    private const int SW_SHOWNOACTIVATE = 4;

    private const uint LWA_ALPHA = 0x00000002;
    private const uint LWA_COLORKEY = 0x00000001;

    private const int WM_DESTROY = 0x0002;
    private const int WM_PAINT = 0x000F;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_MOVE = 0x0003;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 2;

    public const int OverlayWidth = 330;
    public const int OverlayHeight = 142;

    private IntPtr _hwnd = IntPtr.Zero;
    private Thread? _uiThread;
    private volatile bool _isVisible = false;
    private volatile bool _isDisposed = false;
    private readonly object _lock = new();

    private WndProc? _wndProcDelegate;

    // Telemetry cache
    private string _balanceText = "0 aUEC";
    private string _sessionNetText = "Saldo: +0 aUEC";
    private bool _sessionNetPositive = true;
    private string _serverText = "LIVE · Shard —";
    private string _pingText = "— ms";
    private uint _pingColor = 0x0080DE4A; // BGR Green
    private string _locationText = "Im Raum · Stanton";
    private string _systemBadge = "STANTON";
    private string _missionText = "Kein aktiver Auftrag";
    private bool _isArmistice = true;

    public bool IsVisible => _isVisible;

    public event Action<bool>? VisibilityChanged;

    public NativeMiniHudOverlay()
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
                Logger.Error("NativeMiniHudOverlay.Thread", ex);
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
        string className = "SCLogMate_MiniHudOverlayClass_" + Guid.NewGuid().ToString("N");
        IntPtr hInstance = GetModuleHandle(IntPtr.Zero);

        _wndProcDelegate = (hWnd, msg, wParam, lParam) =>
        {
            switch (msg)
            {
                case WM_PAINT:
                    {
                        var hdc = BeginPaint(hWnd, out var ps);
                        PaintHud(hdc);
                        EndPaint(hWnd, ref ps);
                        return IntPtr.Zero;
                    }

                case WM_LBUTTONDOWN:
                    {
                        int x = (short)(lParam.ToInt32() & 0xFFFF);
                        int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

                        // Close button clicked (top right 305..325, 6..24)
                        if (x >= OverlayWidth - 25 && x <= OverlayWidth - 5 && y >= 5 && y <= 25)
                        {
                            SetVisible(false);
                            var s = Settings.Load();
                            s.OverlayEnabled = false;
                            Settings.Save(s);
                            return IntPtr.Zero;
                        }

                        // Drag window if not locked
                        var settings = Settings.Load();
                        if (!settings.OverlayLocked)
                        {
                            ReleaseCapture();
                            SendMessage(hWnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                        }
                        return IntPtr.Zero;
                    }

                case WM_EXITSIZEMOVE:
                    {
                        if (GetWindowRect(hWnd, out var rect))
                        {
                            var s = Settings.Load();
                            if (!s.OverlayLocked)
                            {
                                s.OverlayPositionX = rect.left;
                                s.OverlayPositionY = rect.top;
                                Settings.Save(s);
                            }
                        }
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
            style = 0x0002 | 0x0001, // CS_HREDRAW | CS_VREDRAW
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = hInstance,
            hCursor = LoadCursor(IntPtr.Zero, (IntPtr)32512), // IDC_ARROW
            lpszClassName = className
        };

        RegisterClassEx(ref wndClass);

        var s = Settings.Load();
        int posX = (int)s.OverlayPositionX;
        int posY = (int)s.OverlayPositionY;

        if (posX <= 0 || posY <= 0)
        {
            int screenW = GetSystemMetrics(0);
            posX = Math.Max(20, screenW - OverlayWidth - 30);
            posY = 40;
        }

        int exStyle = WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED;
        if (s.OverlayClickThrough)
        {
            exStyle |= WS_EX_TRANSPARENT;
        }

        _hwnd = CreateWindowEx(
            exStyle,
            className,
            "SCLogMate Live HUD",
            WS_POPUP,
            posX, posY, OverlayWidth, OverlayHeight,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd != IntPtr.Zero)
        {
            byte alpha = (byte)(Math.Clamp(s.OverlayOpacity, 0.3, 1.0) * 255);
            SetLayeredWindowAttributes(_hwnd, 0, alpha, LWA_ALPHA);
        }
    }

    private void PaintHud(IntPtr hdc)
    {
        // Double buffering
        IntPtr memDC = CreateCompatibleDC(hdc);
        IntPtr memBmp = CreateCompatibleBitmap(hdc, OverlayWidth, OverlayHeight);
        IntPtr oldBmp = SelectObject(memDC, memBmp);

        // 1. Hintergrund (Sehr dunkles Sci-Fi Navy-Schwarz mit leichtem Blau-Stich)
        // RGB(8, 14, 24) -> BGR: 0x00180E08
        IntPtr bgBrush = CreateSolidBrush(0x00180E08);
        IntPtr oldBrush = SelectObject(memDC, bgBrush);

        // Rahmenfarbe: Armistice = Cyan (0x00EED322), Outlaw/Offen = Amber (0x000BB5F5)
        uint borderColor = _isArmistice ? 0x00EED322u : 0x000BB5F5u;
        IntPtr borderPen = CreatePen(0, 1, borderColor);
        IntPtr oldPen = SelectObject(memDC, borderPen);

        RoundRect(memDC, 1, 1, OverlayWidth - 1, OverlayHeight - 1, 12, 12);

        // Sub-Header Hintergrundleiste (RGB(14, 22, 36) -> BGR: 0x0024160E)
        IntPtr headerBrush = CreateSolidBrush(0x0024160E);
        var headerRc = new RECT { left = 2, top = 2, right = OverlayWidth - 2, bottom = 26 };
        FillRect(memDC, ref headerRc, headerBrush);
        DeleteObject(headerBrush);

        // Header Trennlinie
        IntPtr divPen = CreatePen(0, 1, 0x003D2616); // BGR subtil
        SelectObject(memDC, divPen);
        MoveToEx(memDC, 2, 26, IntPtr.Zero);
        LineTo(memDC, OverlayWidth - 2, 26);
        DeleteObject(divPen);

        // Fonts erstellen
        IntPtr fontSmall = CreateFont(12, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontBig = CreateFont(20, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr fontMono = CreateFont(13, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Consolas");

        SetBkMode(memDC, 1 /*TRANSPARENT*/);

        // 2. HEADER ZEILE: Drag Grip [⋮⋮], Server-Info, Ping, Close-Button [✕]
        SelectObject(memDC, fontSmall);

        // Drag Grip
        SetTextColor(memDC, 0x00808080);
        var gripRc = new RECT { left = 8, top = 4, right = 24, bottom = 24 };
        DrawText(memDC, "⋮⋮", -1, ref gripRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Server Subline
        SetTextColor(memDC, 0x009E948B); // Slate-400
        var srvRc = new RECT { left = 26, top = 4, right = 220, bottom = 24 };
        DrawText(memDC, _serverText, -1, ref srvRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Ping Indicator & Text
        SetTextColor(memDC, _pingColor);
        var pingRc = new RECT { left = 215, top = 4, right = 295, bottom = 24 };
        DrawText(memDC, $"📶 {_pingText}", -1, ref pingRc, 0x00000002 /*DT_RIGHT*/ | 0x00000004 | 0x00000020);

        // Close Button [✕]
        SetTextColor(memDC, 0x00A0A0A0);
        var closeRc = new RECT { left = OverlayWidth - 24, top = 4, right = OverlayWidth - 6, bottom = 24 };
        DrawText(memDC, "✕", -1, ref closeRc, 0x00000001 /*DT_CENTER*/ | 0x00000004 | 0x00000020);

        // 3. MITTLERE ZEILE: Live Kontostand & Session-Saldo
        SelectObject(memDC, fontBig);
        SetTextColor(memDC, 0x00FCF6F0); // Weiß/Helles Slate
        var balRc = new RECT { left = 12, top = 32, right = 230, bottom = 58 };
        DrawText(memDC, _balanceText, -1, ref balRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Session-Netto (Grün/Rot)
        SelectObject(memDC, fontMono);
        SetTextColor(memDC, _sessionNetPositive ? 0x0080DE4Au : 0x007171F8u); // BGR Green / Rose
        var netRc = new RECT { left = 12, top = 58, right = 230, bottom = 78 };
        DrawText(memDC, _sessionNetText, -1, ref netRc, 0x00000000 | 0x00000004 | 0x00000020);

        // System Badge rechts (STANTON / PYRO Pill)
        SelectObject(memDC, fontSmall);
        var badgeRc = new RECT { left = OverlayWidth - 85, top = 42, right = OverlayWidth - 12, bottom = 66 };
        IntPtr badgeBg = CreateSolidBrush(0x00221B16);
        IntPtr badgePen = CreatePen(0, 1, borderColor);
        SelectObject(memDC, badgeBg);
        SelectObject(memDC, badgePen);
        RoundRect(memDC, badgeRc.left, badgeRc.top, badgeRc.right, badgeRc.bottom, 6, 6);
        DeleteObject(badgeBg);
        DeleteObject(badgePen);

        SetTextColor(memDC, borderColor);
        DrawText(memDC, _systemBadge, -1, ref badgeRc, 0x00000001 | 0x00000004 | 0x00000020);

        // Subtiler Trenner vor Footer
        IntPtr footDiv = CreatePen(0, 1, 0x00261E14);
        SelectObject(memDC, footDiv);
        MoveToEx(memDC, 8, 86, IntPtr.Zero);
        LineTo(memDC, OverlayWidth - 8, 86);
        DeleteObject(footDiv);

        // 4. FOOTER: Standort & Aktiver Auftrag
        SelectObject(memDC, fontSmall);
        SetTextColor(memDC, 0x00F0E8E2); // Heller Text
        var locRc = new RECT { left = 12, top = 92, right = OverlayWidth - 12, bottom = 112 };
        string locPrefix = _isArmistice ? "🛡️ " : "📍 ";
        DrawText(memDC, $"{locPrefix}{_locationText}", -1, ref locRc, 0x00000000 | 0x00000004 | 0x00000020);

        // Mission / Statuszeile
        SetTextColor(memDC, 0x000BB5F5); // Amber Gold
        var msnRc = new RECT { left = 12, top = 114, right = OverlayWidth - 12, bottom = 134 };
        DrawText(memDC, $"🎯 {_missionText}", -1, ref msnRc, 0x00000000 | 0x00000004 | 0x00000020);

        // BitBlt zum Fenster
        BitBlt(hdc, 0, 0, OverlayWidth, OverlayHeight, memDC, 0, 0, 0x00CC0020 /*SRCCOPY*/);

        // Cleanup GDI
        SelectObject(memDC, oldBmp);
        SelectObject(memDC, oldBrush);
        SelectObject(memDC, oldPen);
        DeleteObject(memBmp);
        DeleteDC(memDC);
        DeleteObject(bgBrush);
        DeleteObject(borderPen);
        DeleteObject(fontSmall);
        DeleteObject(fontBig);
        DeleteObject(fontMono);
    }

    public void UpdateTelemetry(HudTelemetryDto dto)
    {
        if (dto == null) return;
        lock (_lock)
        {
            _balanceText = $"{dto.Balance:N0} aUEC";
            _sessionNetPositive = dto.SessionNet >= 0;
            string sign = dto.SessionNet > 0 ? "+" : "";
            _sessionNetText = $"Saldo: {sign}{dto.SessionNet:N0} aUEC";

            string shardNum = !string.IsNullOrWhiteSpace(dto.ServerShardNumber) && dto.ServerShardNumber != "—"
                ? dto.ServerShardNumber
                : dto.ServerShard;
            string reg = !string.IsNullOrWhiteSpace(dto.ServerRegionCode) && dto.ServerRegionCode != "—"
                ? dto.ServerRegionCode
                : "LIVE";
            _serverText = $"{reg} · {shardNum}";

            if (dto.ServerPingMs.HasValue && dto.ServerPingMs.Value > 0)
            {
                int ping = dto.ServerPingMs.Value;
                _pingText = $"{ping} ms";
                _pingColor = ping < 60 ? 0x0080DE4Au : (ping < 120 ? 0x000BB5F5u : 0x007171F8u);
            }
            else
            {
                _pingText = "LIVE";
                _pingColor = 0x0080DE4Au;
            }

            string body = !string.IsNullOrWhiteSpace(dto.LocationBody) && dto.LocationBody != "—"
                ? $" · {dto.LocationBody}"
                : "";
            _locationText = $"{dto.LocationName}{body}";
            _systemBadge = string.IsNullOrWhiteSpace(dto.LocationSystem) ? "STANTON" : dto.LocationSystem.ToUpperInvariant();
            _isArmistice = dto.IsArmistice;

            if (!string.IsNullOrWhiteSpace(dto.ActiveMissionTitle) && dto.ActiveMissionTitle != "Kein aktiver Auftrag")
            {
                string reward = dto.ActiveMissionReward > 0 ? $" ({dto.ActiveMissionReward:N0} aUEC)" : "";
                _missionText = $"{dto.ActiveMissionTitle}{reward}";
            }
            else
            {
                _missionText = "Kein aktiver Auftrag";
            }
        }

        if (_hwnd != IntPtr.Zero && _isVisible)
        {
            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }
    }

    public void SetVisible(bool visible)
    {
        if (_hwnd == IntPtr.Zero) return;
        _isVisible = visible;

        if (visible)
        {
            // Immer sicherstellen, dass aktuelle Einstellungen (ClickThrough / Opacity) angewendet sind
            ApplyWindowStyles();
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

    public void ApplyWindowStyles()
    {
        if (_hwnd == IntPtr.Zero) return;
        var s = Settings.Load();

        int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TOPMOST;

        if (s.OverlayClickThrough)
            ex |= WS_EX_TRANSPARENT;
        else
            ex &= ~WS_EX_TRANSPARENT;

        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);

        byte alpha = (byte)(Math.Clamp(s.OverlayOpacity, 0.3, 1.0) * 255);
        SetLayeredWindowAttributes(_hwnd, 0, alpha, LWA_ALPHA);
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
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll")] private static extern bool SetThreadDpiAwarenessContext(IntPtr dpiContext);
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
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
