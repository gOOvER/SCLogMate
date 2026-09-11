using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SCLogMate.Core.Photino;

public static class NativeDialogs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class OpenFileName
    {
        public int lStructSize = Marshal.SizeOf<OpenFileName>();
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr hInstance = IntPtr.Zero;
        public string? lpstrFilter = null;
        public string? lpstrCustomFilter = null;
        public int nMaxCustFilter = 0;
        public int nFilterIndex = 1;
        public string? lpstrFile = null;
        public int nMaxFile = 0;
        public string? lpstrFileTitle = null;
        public int nMaxFileTitle = 0;
        public string? lpstrInitialDir = null;
        public string? lpstrTitle = null;
        public int Flags = 0;
        public short nFileOffset = 0;
        public short nFileExtension = 0;
        public string? lpstrDefExt = null;
        public IntPtr lCustData = IntPtr.Zero;
        public IntPtr lpfnHook = IntPtr.Zero;
        public string? lpTemplateName = null;
        public IntPtr pvReserved = IntPtr.Zero;
        public int dwReserved = 0;
        public int FlagsEx = 0;
    }

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    /// <summary>
    /// Öffnet den nativen Windows Datei-Dialog (OpenFileDialog) auf einem STA-Thread.
    /// </summary>
    public static string? ShowOpenFileDialog(string title = "Game.log auswählen", string? initialDir = null)
    {
        string? selectedPath = null;

        var thread = new Thread(() =>
        {
            try
            {
                var ofn = new OpenFileName
                {
                    lpstrTitle = title,
                    lpstrFilter = "Star Citizen Game.log (*.log)\0*.log;Game.log\0Alle Dateien (*.*)\0*.*\0\0",
                    lpstrFile = new string(new char[1024]),
                    nMaxFile = 1024,
                    lpstrInitialDir = !string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir) ? initialDir : null,
                    Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008 // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
                };

                if (GetOpenFileName(ofn))
                {
                    selectedPath = ofn.lpstrFile.TrimEnd('\0');
                }
            }
            catch (Exception ex)
            {
                Logger.Error("NativeDialogs.ShowOpenFileDialog", ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return selectedPath;
    }
}
