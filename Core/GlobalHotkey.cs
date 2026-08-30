using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;

namespace SCLogReader.Core;

public static class GlobalHotkey
{
    private const int MOD_ALT = 0x0001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int MOD_NOREPEAT = 0x4000;
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9021;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    private static Thread? _listenerThread;
    private static bool _isRunning = false;
    public static event Action? HotkeyPressed;

    public static void Start()
    {
        if (_isRunning || !OperatingSystem.IsWindows()) return;
        _isRunning = true;

        _listenerThread = new Thread(() =>
        {
            // 'H' key code is 0x48. Alt modifier is MOD_ALT.
            uint vk = 0x48; // 'H'
            uint modifiers = MOD_ALT | MOD_NOREPEAT;

            if (RegisterHotKey(IntPtr.Zero, HOTKEY_ID, modifiers, vk))
            {
                while (_isRunning && GetMessage(out var msg, IntPtr.Zero, 0, 0))
                {
                    if (msg.message == WM_HOTKEY && (int)msg.wParam == HOTKEY_ID)
                    {
                        Dispatcher.UIThread.Post(() => HotkeyPressed?.Invoke());
                    }
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
                UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
            }
        })
        {
            IsBackground = true,
            Name = "GlobalHotkeyListener"
        };
        _listenerThread.Start();
    }

    public static void Stop()
    {
        _isRunning = false;
    }
}
