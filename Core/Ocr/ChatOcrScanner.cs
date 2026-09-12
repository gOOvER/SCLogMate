using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Pollt den In-Game Chatbereich von Star Citizen (F12 Chatfenster),
/// führt OCR-Texterkennung durch, parsed Chat-Nachrichten und speichert
/// neue Einträge dedupliziert in der Datenbank.
/// </summary>
public sealed class ChatOcrScanner : IDisposable
{
    private readonly OcrEngineService _ocrEngine;
    private readonly Func<ScanRegion?> _regionProvider;
    private readonly Func<bool> _isEnabled;
    private readonly Func<string?>? _sessionIdProvider;
    private System.Timers.Timer? _timer;
    private bool _running;
    private int _busy;
    private readonly HashSet<string> _recentMessageHashes = new();
    private readonly Queue<string> _hashQueue = new();
    private const int MaxHashHistory = 500;

    public event Action<List<ChatMessageDto>>? MessagesScanned;
    public event Action<bool>? RunningChanged;
    public event Action<string>? StageChanged;

    public bool IsRunning => _running;
    public string? LastStage { get; private set; }

    public ChatOcrScanner(
        OcrEngineService ocrEngine,
        Func<ScanRegion?> regionProvider,
        Func<bool> isEnabled,
        Func<string?>? sessionIdProvider = null,
        int intervalMs = 3500)
    {
        _ocrEngine = ocrEngine;
        _regionProvider = regionProvider;
        _isEnabled = isEnabled;
        _sessionIdProvider = sessionIdProvider;
        _intervalMs = intervalMs > 1000 ? intervalMs : 3500;
    }

    private readonly int _intervalMs;

    public void Start()
    {
        if (_running) return;
        _running = true;
        Logger.Log($"[CHAT-OCR] Chat-Scanner gestartet (Polling @ {_intervalMs}ms)");
        _timer = new System.Timers.Timer(_intervalMs);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = false;
        _timer.Start();
        RunningChanged?.Invoke(true);
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        Logger.Log("[CHAT-OCR] Chat-Scanner gestoppt");
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        RunningChanged?.Invoke(false);
    }

    public async Task<List<ChatMessageDto>> ScanNowAsync()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return new List<ChatMessageDto>();

        try
        {
            return await ExecuteScanAsync(forceReturnAllVisible: true);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private async void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

        try
        {
            if (!_isEnabled())
            {
                SetStage("disabled");
                return;
            }

            await ExecuteScanAsync(forceReturnAllVisible: false);
        }
        catch (Exception ex)
        {
            Logger.Error("[CHAT-OCR] Scan Tick Fehler", ex);
            SetStage("error");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
            if (_running)
            {
                try { _timer?.Start(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    private async Task<List<ChatMessageDto>> ExecuteScanAsync(bool forceReturnAllVisible = false)
    {
        var resultList = new List<ChatMessageDto>();

        var region = _regionProvider();
        if (region == null || !region.IsValid)
        {
            SetStage("noregion");
            return resultList;
        }

        if (!_ocrEngine.IsAvailable)
        {
            SetStage("unavail");
            return resultList;
        }

        var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
        if (raw == null)
        {
            SetStage("notext");
            return resultList;
        }

        // Dual-pass OCR (Scale=1 für scharfe HUD-Schrift, kein aggressives Boosting)
        var (invText, plainText) = await _ocrEngine.RecognizeDualPassAsync(
            raw, region.Width, region.Height, scale: 1, padding: 8, boostContrast: false);

        var currentSession = _sessionIdProvider?.Invoke();

        // 1. Zuerst Plain-Pass (natürlicher weißer HUD-Text) parsen
        var parsed = !string.IsNullOrWhiteSpace(plainText)
            ? ChatParser.ParseChatLines(plainText, currentSession)
            : new List<ChatMessageDto>();

        // 2. Inverted-Pass ergänzen (erfasst ggf. dunklere oder invertiert besser lesbare Nachrichten & Org-Kanäle)
        if (!string.IsNullOrWhiteSpace(invText))
        {
            var invParsed = ChatParser.ParseChatLines(invText, currentSession);
            if (parsed.Count == 0)
            {
                parsed = invParsed;
            }
            else if (invParsed.Count > 0)
            {
                var existingHashes = new HashSet<string>(parsed.Select(m => ComputeMessageHash(m.Channel, m.Sender, m.Message)));
                foreach (var invMsg in invParsed)
                {
                    var invHash = ComputeMessageHash(invMsg.Channel, invMsg.Sender, invMsg.Message);

                    // Falls bereits in Plain erkannt, aber in Invert mit echtem Org-Kanal (z. B. 'SC Krautz' statt 'Global'):
                    var existing = parsed.FirstOrDefault(p =>
                        string.Equals(p.Sender, invMsg.Sender, StringComparison.OrdinalIgnoreCase) &&
                        ComputeMessageHash("", p.Sender, p.Message) == invHash);

                    if (existing != null)
                    {
                        if (existing.Channel == "Global" && invMsg.Channel != "Global")
                        {
                            existing.Channel = invMsg.Channel;
                        }
                    }
                    else if (existingHashes.Add(invHash))
                    {
                        parsed.Add(invMsg);
                    }
                }
            }
        }

        if (parsed.Count == 0)
        {
            SetStage(string.IsNullOrWhiteSpace(plainText) && string.IsNullOrWhiteSpace(invText) ? "notext" : "idle");
            return resultList;
        }

        var newMessages = new List<ChatMessageDto>();
        var visibleMessages = new List<ChatMessageDto>();

        lock (_recentMessageHashes)
        {
            foreach (var msg in parsed)
            {
                var hash = ComputeMessageHash(msg.Channel, msg.Sender, msg.Message);
                bool isNew = _recentMessageHashes.Add(hash);
                if (isNew)
                {
                    _hashQueue.Enqueue(hash);
                    if (_hashQueue.Count > MaxHashHistory)
                    {
                        var old = _hashQueue.Dequeue();
                        _recentMessageHashes.Remove(old);
                    }

                    // In Datenbank speichern
                    var savedId = Database.InsertChatMessage(msg);
                    if (savedId > 0)
                    {
                        msg.Id = savedId;
                        newMessages.Add(msg);
                    }
                }
                else
                {
                    var existingId = Database.InsertChatMessage(msg);
                    if (existingId > 0) msg.Id = existingId;
                }

                visibleMessages.Add(msg);
            }
        }

        if (newMessages.Count > 0)
        {
            SetStage("parsed");
            Logger.Log($"[CHAT-OCR] {newMessages.Count} neue Chat-Nachricht(en) erfasst.");
            MessagesScanned?.Invoke(newMessages);
        }
        else
        {
            SetStage("idle");
        }

        return forceReturnAllVisible ? visibleMessages : newMessages;
    }

    private static string ComputeMessageHash(string channel, string sender, string message)
    {
        var cleanMsg = message.Trim().TrimEnd('.', ',', '!', '?', '-', ';').ToLowerInvariant();
        var raw = $"{sender.Trim().ToLowerInvariant()}:{cleanMsg}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private void SetStage(string stage)
    {
        if (LastStage != stage)
        {
            LastStage = stage;
            StageChanged?.Invoke(stage);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
