using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SCLogReader.Core;

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

    public event Action<string>? Line;
    public event Action<string, bool>? LineEx;
    public event Action<string>? Status;

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

                    if (fs.Length < position) { position = 0; NotifyStatus(cts, "Log rotiert – neu eingelesen"); SetLiveStreaming(cts, false); }

                    fs.Seek(position, SeekOrigin.Begin);
                    using var sr = new StreamReader(fs);
                    string? l;
                    while ((l = await sr.ReadLineAsync()) != null)
                    {
                        if (!IsCurrent(cts)) return;
                        Line?.Invoke(l);
                        LineEx?.Invoke(l, IsLiveStreaming);
                    }

                    position = fs.Position;
                    SetLiveStreaming(cts, true); // Nach dem ersten kompletten Durchlauf sind alle neuen Zeilen echte Live-Events
                    NotifyStatus(cts, $"live · {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { NotifyStatus(cts, "Fehler: " + ex.Message); }

            try { await Task.Delay(500, ct); }
            catch (OperationCanceledException) { break; }
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
