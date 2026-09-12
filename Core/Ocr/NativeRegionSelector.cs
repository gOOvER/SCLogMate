using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Nativer Win32-Overlay-Bildschirmwähler zur interaktiven Auswahl von Scan-Bereichen (mobiGlas, RS-Scanner, Chat).
/// Erstreckt sich nahtlos über den gesamten virtuellen Desktop aller Monitore gleichzeitig.
/// Ermöglicht pixelgenaues Zeichnen auf beliebigen Monitoren (inkl. sekundärer Monitore mit negativen Offsets).
/// </summary>
public static class NativeRegionSelector
{
    public static Task<ScanRegion?> SelectRegionAsync(string targetTitle = "mobiGlas aUEC")
    {
        var tcs = new TaskCompletionSource<ScanRegion?>();

        var thread = new Thread(() =>
        {
            try
            {
                var region = ShowSelectorModal(targetTitle);
                tcs.TrySetResult(region);
            }
            catch (Exception ex)
            {
                Logger.Error("NativeRegionSelector.SelectRegionAsync", ex);
                tcs.TrySetResult(null);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return tcs.Task;
    }

    private static ScanRegion? ShowSelectorModal(string targetTitle)
    {
        var monitors = GetMonitors();

        // 1. Virtuellen Bildschirm über ALLE Monitore ermitteln
        int vX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vY = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vW = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vH = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        if (vW <= 0 || vH <= 0)
        {
            vX = 0;
            vY = 0;
            vW = GetSystemMetrics(SM_CXSCREEN);
            vH = GetSystemMetrics(SM_CYSCREEN);
            if (vW <= 0) vW = 1920;
            if (vH <= 0) vH = 1080;
        }

        if (monitors.Count == 0)
        {
            monitors.Add(new RECT { left = vX, top = vY, right = vX + vW, bottom = vY + vH });
        }

        ScanRegion? result = null;
        bool isDragging = false;
        POINT startPt = default;
        POINT curPt = default;

        IntPtr hFontBold = CreateFont(16, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr hFontRegular = CreateFont(13, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
        IntPtr hFontBadge = CreateFont(12, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 2, 0, "Consolas");

        IntPtr bgBrush = CreateSolidBrush(0x001A1009); // BGR: #09101a
        IntPtr bannerBrush = CreateSolidBrush(0x00221B16); // BGR: #161b22
        IntPtr bannerBorderPen = CreatePen(0 /*PS_SOLID*/, 1, 0x00F8BD38); // BGR: #38bdf8
        IntPtr cyanPen = CreatePen(0 /*PS_SOLID*/, 2, 0x00F8BD38); // BGR: #38bdf8
        IntPtr dragFillBrush = CreateSolidBrush(0x004A3010); // Translucent-effect BGR
        IntPtr badgeBgBrush = CreateSolidBrush(0x001A1009);

        IntPtr hCursorCross = LoadCursor(IntPtr.Zero, (IntPtr)32515 /*IDC_CROSS*/);

        string className = "SCLogMate_RegionSelectorClass_" + Guid.NewGuid().ToString("N");
        IntPtr hInstance = GetModuleHandle(IntPtr.Zero);

        WndProc wndProcDelegate = (hWnd, msg, wParam, lParam) =>
        {
            switch (msg)
            {
                case 0x0020: // WM_SETCURSOR
                    SetCursor(hCursorCross);
                    return (IntPtr)1;

                case 0x0201: // WM_LBUTTONDOWN
                    isDragging = true;
                    GetCursorPos(out startPt); // Physische absolute Bildschirm-Koordinaten
                    curPt = startPt;
                    SetCapture(hWnd);
                    InvalidateRect(hWnd, IntPtr.Zero, false);
                    return IntPtr.Zero;

                case 0x0200: // WM_MOUSEMOVE
                    if (isDragging)
                    {
                        GetCursorPos(out curPt);
                        InvalidateRect(hWnd, IntPtr.Zero, false);
                    }
                    return IntPtr.Zero;

                case 0x0202: // WM_LBUTTONUP
                    if (isDragging)
                    {
                        isDragging = false;
                        ReleaseCapture();
                        GetCursorPos(out curPt);

                        int rx = Math.Min(startPt.X, curPt.X);
                        int ry = Math.Min(startPt.Y, curPt.Y);
                        int rw = Math.Abs(startPt.X - curPt.X);
                        int rh = Math.Abs(startPt.Y - curPt.Y);

                        if (rw > 5 && rh > 5)
                        {
                            result = new ScanRegion
                            {
                                X = rx,
                                Y = ry,
                                Width = rw,
                                Height = rh
                            };
                        }

                        DestroyWindow(hWnd);
                    }
                    return IntPtr.Zero;

                case 0x0204: // WM_RBUTTONDOWN
                    // Rechtsklick bricht sofort ab
                    result = null;
                    DestroyWindow(hWnd);
                    return IntPtr.Zero;

                case 0x0100: // WM_KEYDOWN
                    if (wParam == (IntPtr)0x1B) // VK_ESCAPE
                    {
                        result = null;
                        DestroyWindow(hWnd);
                    }
                    return IntPtr.Zero;

                case 0x000F: // WM_PAINT
                    {
                        var hdc = BeginPaint(hWnd, out var ps);
                        GetClientRect(hWnd, out var rc);
                        int w = rc.right - rc.left;
                        int h = rc.bottom - rc.top;

                        IntPtr memDC = CreateCompatibleDC(hdc);
                        IntPtr memBmp = CreateCompatibleBitmap(hdc, w, h);
                        IntPtr oldBmp = SelectObject(memDC, memBmp);

                        // 1. Hintergrund füllen (dunkles Overlay über gesamten Desktop)
                        FillRect(memDC, ref rc, bgBrush);

                        // 2. Info-Banner auf JEDEM Monitor zeichnen, damit er auf allen Displays sichtbar ist
                        int bannerW = 660;
                        int bannerH = 78;

                        for (int i = 0; i < monitors.Count; i++)
                        {
                            var m = monitors[i];
                            int monW = m.right - m.left;
                            int monClientLeft = m.left - vX;
                            int monClientTop = m.top - vY;
                            int bannerX = monClientLeft + (monW - bannerW) / 2;
                            int bannerY = monClientTop + 28;
                            var bannerRc = new RECT { left = bannerX, top = bannerY, right = bannerX + bannerW, bottom = bannerY + bannerH };

                            IntPtr oldBrush = SelectObject(memDC, bannerBrush);
                            IntPtr oldPen = SelectObject(memDC, bannerBorderPen);
                            RoundRect(memDC, bannerRc.left, bannerRc.top, bannerRc.right, bannerRc.bottom, 12, 12);

                            SetBkMode(memDC, 1 /*TRANSPARENT*/);

                            // Titel
                            SelectObject(memDC, hFontBold);
                            SetTextColor(memDC, 0x00F8BD38); // Cyan BGR
                            var titleRc = new RECT { left = bannerX + 16, top = bannerY + 10, right = bannerX + bannerW - 16, bottom = bannerY + 32 };
                            string monInfo = monitors.Count > 1 ? $" · Monitor {i + 1}/{monitors.Count} ({monW}x{m.bottom - m.top})" : $" · {monW}x{m.bottom - m.top}";
                            DrawText(memDC, $"🎯 {targetTitle} auswählen{monInfo}", -1, ref titleRc, 0x00000001 /*DT_CENTER*/ | 0x00000004 /*DT_VCENTER*/ | 0x00000020 /*DT_SINGLELINE*/);

                            // Instruktionen
                            SelectObject(memDC, hFontRegular);
                            SetTextColor(memDC, 0x00D0D0D0); // Weiß/Hellgrau
                            var subRc = new RECT { left = bannerX + 16, top = bannerY + 34, right = bannerX + bannerW - 16, bottom = bannerY + 52 };
                            DrawText(memDC, "Ziehe mit gedrückter linker Maustaste ein Rechteck auf einem beliebigen Monitor.", -1, ref subRc, 0x00000001 | 0x00000004 | 0x00000020);

                            SetTextColor(memDC, 0x007171F8); // Rötlich/Orange
                            var keyRc = new RECT { left = bannerX + 16, top = bannerY + 54, right = bannerX + bannerW - 16, bottom = bannerY + 72 };
                            DrawText(memDC, "[ESC] oder Rechtsklick: Abbrechen  ·  Nahtlos über alle Monitore aktiv", -1, ref keyRc, 0x00000001 | 0x00000004 | 0x00000020);

                            SelectObject(memDC, oldBrush);
                            SelectObject(memDC, oldPen);
                        }

                        // 3. Wenn gezogen wird: Markierungsrechteck + Abmessungen-Badge
                        if (isDragging)
                        {
                            int rx = Math.Min(startPt.X, curPt.X);
                            int ry = Math.Min(startPt.Y, curPt.Y);
                            int rw = Math.Abs(startPt.X - curPt.X);
                            int rh = Math.Abs(startPt.Y - curPt.Y);

                            if (rw > 0 && rh > 0)
                            {
                                // Absolute Bildschirm-Koordinaten in Fenster-Client-Koordinaten umrechnen
                                int clientRx = rx - vX;
                                int clientRy = ry - vY;

                                SelectObject(memDC, dragFillBrush);
                                SelectObject(memDC, cyanPen);
                                Rectangle(memDC, clientRx, clientRy, clientRx + rw, clientRy + rh);

                                // Dimensions-Badge mit Echtzeit-Koordinaten
                                string badgeText = $"{rw} × {rh} px  (X:{rx}, Y:{ry})";
                                SelectObject(memDC, hFontBadge);
                                SelectObject(memDC, badgeBgBrush);
                                SelectObject(memDC, bannerBorderPen);

                                int badgeW = 180;
                                int badgeH = 24;
                                int badgeX = clientRx;
                                int badgeY = clientRy >= 32 ? (clientRy - badgeH - 4) : (clientRy + rh + 4);

                                RoundRect(memDC, badgeX, badgeY, badgeX + badgeW, badgeY + badgeH, 6, 6);
                                SetTextColor(memDC, 0x00F8BD38);
                                var badgeRc = new RECT { left = badgeX, top = badgeY, right = badgeX + badgeW, bottom = badgeY + badgeH };
                                DrawText(memDC, badgeText, -1, ref badgeRc, 0x00000001 | 0x00000004 | 0x00000020);
                            }
                        }

                        // Backbuffer übertragen
                        BitBlt(hdc, 0, 0, w, h, memDC, 0, 0, 0x00CC0020 /*SRCCOPY*/);

                        // Cleanup GDI Objektauswahl
                        SelectObject(memDC, oldBmp);
                        DeleteObject(memBmp);
                        DeleteDC(memDC);

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
            style = 0x0001 | 0x0002 /*CS_VREDRAW | CS_HREDRAW*/,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = hInstance,
            hIcon = IntPtr.Zero,
            hCursor = hCursorCross,
            hbrBackground = IntPtr.Zero,
            lpszMenuName = null,
            lpszClassName = className,
            hIconSm = IntPtr.Zero
        };

        ushort atom = RegisterClassEx(ref wndClass);
        if (atom == 0)
        {
            Logger.Log($"NativeRegionSelector: RegisterClassEx fehlgeschlagen (Error: {Marshal.GetLastWin32Error()})");
            return null;
        }

        try
        {
            uint exStyle = 0x00000008 /*WS_EX_TOPMOST*/ | 0x00080000 /*WS_EX_LAYERED*/ | 0x00000080 /*WS_EX_TOOLWINDOW*/;
            uint style = 0x80000000 /*WS_POPUP*/ | 0x10000000 /*WS_VISIBLE*/;

            // Fenster erstreckt sich über den GESAMTEN virtuellen Bildschirm aller Monitore
            IntPtr hWnd = CreateWindowEx(
                exStyle,
                className,
                "SCLogMate - Bereich auswählen",
                style,
                vX, vY, vW, vH,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (hWnd == IntPtr.Zero)
            {
                Logger.Log($"NativeRegionSelector: CreateWindowEx fehlgeschlagen (Error: {Marshal.GetLastWin32Error()})");
                return null;
            }

            // Halbtransparenter Hintergrund (~65% Deckkraft)
            SetLayeredWindowAttributes(hWnd, 0, 165, 0x00000002 /*LWA_ALPHA*/);
            SetWindowPos(hWnd, new IntPtr(-1) /*HWND_TOPMOST*/, vX, vY, vW, vH, 0x0040 /*SWP_SHOWWINDOW*/);
            SetForegroundWindow(hWnd);
            SetFocus(hWnd);

            // Message-Loop
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            UnregisterClass(className, hInstance);
            DeleteObject(hFontBold);
            DeleteObject(hFontRegular);
            DeleteObject(hFontBadge);
            DeleteObject(bgBrush);
            DeleteObject(bannerBrush);
            DeleteObject(bannerBorderPen);
            DeleteObject(cyanPen);
            DeleteObject(dragFillBrush);
            DeleteObject(badgeBgBrush);
            GC.KeepAlive(wndProcDelegate);
        }

        return result;
    }

    private static List<RECT> GetMonitors()
    {
        var list = new List<RECT>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref RECT rc, IntPtr data) =>
        {
            list.Add(rc);
            return true;
        }, IntPtr.Zero);
        return list;
    }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; public POINT(int x, int y) { X = x; Y = y; } }

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr SetCapture(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr hCursor);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] private static extern bool TranslateMessage([In] ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int nExitCode);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint crColor);
    [DllImport("gdi32.dll")] private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);
    [DllImport("gdi32.dll")] private static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
    [DllImport("gdi32.dll")] private static extern bool RoundRect(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidth, int nHeight);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);
    [DllImport("gdi32.dll")] private static extern int SetBkMode(IntPtr hdc, int iBkMode);
    [DllImport("gdi32.dll")] private static extern uint SetTextColor(IntPtr hdc, uint crColor);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int DrawText(IntPtr hDC, string lpchText, int nCount, ref RECT lpRect, uint uFormat);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateFont(
        int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
        uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
        uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace);

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
