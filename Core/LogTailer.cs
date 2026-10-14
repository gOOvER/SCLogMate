using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SCLogMate.Core;

/// <summary>
/// Liest die Game.log live mit, OHNE Star Citizen zu stören.
/// Entscheidend: FileShare.ReadWrite | FileShare.Delete, sonst "Access denied",
/// weil das Spiel die Datei offen hält. Erkennt Log-Rotation (Neustart von SC).
/// </summary>
public class LogTailer
{
    readonly string _path;
    CancellationTokenSource? _cts;
    Task? _loopTask;
    string? _carry;

    public event Action<string>? Line;
    public event Action<string, bool>? LineEx;
    public event Action<string>? Status;
    public event Action<string?>? Rotated;

    public bool IsLiveStreaming { get; private set; } = false;

    public LogTailer(string path) => _path = path;

    public void Start(bool fromStart = true)
    {
        Stop();
        var cts = new CancellationTokenSource();
        _cts = cts;
        IsLiveStreaming = !fromStart;
        _loopTask = Task.Run(() => LoopAsync(cts, fromStart));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
        IsLiveStreaming = false;
    }

    async Task LoopAsync(CancellationTokenSource cts, bool fromStart)
    {
        var ct = cts.Token;
        long position = fromStart ? 0 : -1;
        bool first = true;
        while (!ct.IsCancellationRequested && IsCurrent(cts))
        {
            try
            {
                if (!File.Exists(_path))
                {
                    Status?.Invoke("warte auf Datei…");
                }
                else
                {
                    using var fs = new FileStream(_path, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete);

                    if (first && position < 0) { position = fs.Length; first = false; }
                    first = false;

                    if (fs.Length < position)
                    {
                        // Star Citizen hat Game.log rotiert (nach logbackups\ verschoben und neu erstellt)
                        string? rotatedBackupPath = null;
                        try
                        {
                            var dir = Path.GetDirectoryName(_path) ?? ".";
                            var backupsDir = Path.Combine(dir, "logbackups");
                            if (Directory.Exists(backupsDir))
                            {
                                var recentBackup = new DirectoryInfo(backupsDir)
                                    .GetFiles("*.log")
                                    .OrderByDescending(f => f.LastWriteTimeUtc)
                                    .FirstOrDefault();

                                if (recentBackup != null && (DateTime.UtcNow - recentBackup.LastWriteTimeUtc).TotalMinutes < 5)
                                {
                                    rotatedBackupPath = recentBackup.FullName;
                                    DrainRotatedBackup(recentBackup.FullName, position, cts);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LogTailer.FindRotatedBackup", ex);
                        }

                        position = 0;
                        _carry = null;
                        NotifyStatus(cts, "Log rotiert – Sitzung archiviert & neu eingelesen");
                        SetLiveStreaming(cts, false);

                        try
                        {
                            Rotated?.Invoke(rotatedBackupPath);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LogTailer.RotatedInvoke", ex);
                        }
                    }

                    fs.Seek(position, SeekOrigin.Begin);
                    using var sr = new StreamReader(fs);
                    var lines = new System.Collections.Generic.List<string>();
                    string? line;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        lines.Add(line);
                    }

                    position = fs.Length;
                    if (_carry is not null)
                    {
                        lines.Insert(0, _carry);
                        _carry = null;
                    }

                    var entries = new System.Collections.Generic.List<string>(LogEntryReader.ReadEntries(lines));
                    if (entries.Count > 0 && LogEntryReader.HasUnterminatedQuote(entries[^1]))
                    {
                        _carry = entries[^1];
                        entries.RemoveAt(entries.Count - 1);
                    }

                    foreach (var entry in entries)
                    {
                        if (!IsCurrent(cts)) return;
                        Line?.Invoke(entry);
                        LineEx?.Invoke(entry, IsLiveStreaming);
                    }

                    SetLiveStreaming(cts, true); // Nach dem ersten kompletten Durchlauf sind alle neuen Zeilen echte Live-Events
                    NotifyStatus(cts, $"live · {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (IOException ex) { NotifyStatus(cts, "Log vorübergehend nicht lesbar: " + ex.Message); }
            catch (UnauthorizedAccessException ex) { NotifyStatus(cts, "Kein Zugriff auf Log: " + ex.Message); }
            catch (Exception ex) { NotifyStatus(cts, "Fehler: " + ex.Message); }

            try { await Task.Delay(500, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// Liest eventuell verbliebene Nachzügler-Zeilen aus dem rotierten Archiv-Log ein,
    /// die vor dem Verschieben der Game.log noch nicht übertragen wurden.
    /// </summary>
    void DrainRotatedBackup(string backupPath, long previousOffset, CancellationTokenSource cts)
    {
        try
        {
            if (!File.Exists(backupPath)) return;
            using var bfs = new FileStream(backupPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            if (bfs.Length <= previousOffset) return;

            bfs.Seek(previousOffset, SeekOrigin.Begin);
            using var bsr = new StreamReader(bfs);
            var trailingLines = new System.Collections.Generic.List<string>();
            string? line;
            while ((line = bsr.ReadLine()) != null)
            {
                trailingLines.Add(line);
            }

            if (trailingLines.Count == 0) return;

            if (_carry is not null)
            {
                trailingLines.Insert(0, _carry);
                _carry = null;
            }

            var entries = LogEntryReader.ReadEntries(trailingLines);
            foreach (var entry in entries)
            {
                if (!IsCurrent(cts)) return;
                Line?.Invoke(entry);
                LineEx?.Invoke(entry, IsLiveStreaming);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("LogTailer.DrainRotatedBackup", ex);
        }
    }

    bool IsCurrent(CancellationTokenSource cts) => ReferenceEquals(_cts, cts);

    void SetLiveStreaming(CancellationTokenSource cts, bool value)
    {
        if (IsCurrent(cts)) IsLiveStreaming = value;
    }

    void NotifyStatus(CancellationTokenSource cts, string message)
    {
        if (IsCurrent(cts)) Status?.Invoke(message);
    }
}
