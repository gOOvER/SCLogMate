using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Schnelle Win32-GDI-Erfassung beliebiger Bildschirmbereiche in ein 32-Bit-BGRA-Array oder WriteableBitmap.
/// </summary>
public static class ScreenCapture
{
    private const int SRCCOPY = 0xCC0020;
    private const uint DIB_RGB_COLORS = 0;

    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
    [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, IntPtr bits, ref BITMAPINFOHEADER bmi, uint usage);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize, biWidth;
        public int biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    /// <summary>Erfasst einen Bildschirmbereich als 32-Bit BGRA Byte-Array.</summary>
    public static unsafe byte[]? Capture(int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return null;

        var hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero) return null;

        var hdcMem = CreateCompatibleDC(hdc);
        var hBmp = CreateCompatibleBitmap(hdc, w, h);
        var hOld = SelectObject(hdcMem, hBmp);

        BitBlt(hdcMem, 0, 0, w, h, hdc, x, y, SRCCOPY);

        var bmpInfo = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = (uint)w,
            biHeight = -h, // Top-Down DIB
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0
        };

        var buf = new byte[w * h * 4];
        fixed (byte* p = buf)
        {
            GetDIBits(hdcMem, hBmp, 0, (uint)h, (IntPtr)p, ref bmpInfo, DIB_RGB_COLORS);
        }

        SelectObject(hdcMem, hOld);
        DeleteObject(hBmp);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdc);

        return buf;
    }

    /// <summary>Erfasst einen Bildschirmbereich direkt als Avalonia WriteableBitmap für Freeze-Frame Overlays.</summary>
    public static WriteableBitmap? CaptureToBitmap(int x, int y, int w, int h)
    {
        var raw = Capture(x, y, w, h);
        if (raw == null) return null;

        try
        {
            var wb = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var fb = wb.Lock())
            {
                Marshal.Copy(raw, 0, fb.Address, raw.Length);
            }
            return wb;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Liefert eine sinnvolle Standard-Region für mobiGlas aUEC basierend auf der aktuellen Bildschirmauflösung.</summary>
    public static ScanRegion GetDefaultWalletRegion()
    {
        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);

        if (screenW <= 0 || screenH <= 0)
        {
            screenW = 1920;
            screenH = 1080;
        }

        double scaleX = screenW / 1920.0;
        double scaleY = screenH / 1080.0;

        int w = (int)Math.Round(500 * scaleX);
        int h = (int)Math.Round(80 * scaleY);
        int x = (int)Math.Round(1300 * scaleX);
        int y = (int)Math.Round(415 * scaleY);

        return new ScanRegion { X = x, Y = y, Width = w, Height = h };
    }

    /// <summary>Liefert die Standard-Region für die mobiGlas Auftrags-Detailkarte (schneidet die linke Sidebar vollständig ab).</summary>
    public static ScanRegion GetDefaultContractRegion()
    {
        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);

        if (screenW <= 0 || screenH <= 0)
        {
            screenW = 1920;
            screenH = 1080;
        }

        // Die Detailkarte im mobiGlas beginnt bei ca. 30% der Bildschirmbreite und reicht über die gesamte Höhe
        int x = (int)Math.Round(screenW * 0.30);
        int y = (int)Math.Round(screenH * 0.03);
        int w = (int)Math.Round(screenW * 0.68);
        int h = (int)Math.Round(screenH * 0.94);

        return new ScanRegion { X = x, Y = y, Width = w, Height = h };
    }

    /// <summary>Liefert die aktuelle primäre Bildschirmauflösung.</summary>
    public static (int Width, int Height) GetPrimaryScreenSize()
    {
        int sw = GetSystemMetrics(SM_CXSCREEN);
        int sh = GetSystemMetrics(SM_CYSCREEN);
        return (sw > 0 ? sw : 1920, sh > 0 ? sh : 1080);
    }

    /// <summary>Liefert die Standard-Region für die Star Citizen HUD Scanner RS-Signatur.</summary>
    public static ScanRegion GetDefaultRsRegion()
    {
        var (sw, sh) = GetPrimaryScreenSize();
        int w = Math.Min(600, (int)(sw * 0.35));
        int h = Math.Min(360, (int)(sh * 0.35));
        int x = (sw - w) / 2;
        int y = (sh - h) / 2;
        return new ScanRegion { X = x, Y = y, Width = w, Height = h };
    }
}

