using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SCLogReader.Models;

namespace SCLogReader.Core.Ocr;

/// <summary>
/// Burst-State-Machine für das Erfassen des aUEC-Kontostands bei mobiGlas-Öffnung.
/// </summary>
public sealed class WalletCapture : IDisposable
{
    public static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(300); // Warten auf mobiGlas UI Fade-In
    public static readonly TimeSpan GrabSpacing = TimeSpan.FromMilliseconds(150);
    public static readonly TimeSpan RetrySpacing = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan BurstBudget = TimeSpan.FromSeconds(4);
    public const int MaxGrabs = 10;

    private readonly OcrEngineService _ocrEngine;
    private readonly Func<ScanRegion?> _regionProvider;
    private readonly Func<bool> _isEnabled;
    private int _busy;
    private CancellationTokenSource? _cts;

    /// <summary>Wird gefeuert, wenn ein Kontostand durch mehrfache Grabs bestätigt wurde.</summary>
    public event Action<long>? BalanceCaptured;

    /// <summary>Status des letzten Burst-Laufs (z.B. "confirmed", "timeout", "no_region").</summary>
    public string? LastOutcome { get; private set; }

    public WalletCapture(OcrEngineService ocrEngine, Func<ScanRegion?> regionProvider, Func<bool> isEnabled)
    {
        _ocrEngine = ocrEngine;
        _regionProvider = regionProvider;
        _isEnabled = isEnabled;
    }

    /// <summary>Prüft jede Logzeile auf das mobiGlas-Signal.</summary>
    public void ProcessLine(string line)
    {
        if (!_isEnabled()) return;
        if (!WalletOcrTrigger.IsMobiGlasOpenSignal(line)) return;

        Trigger();
    }

    /// <summary>Führt einen sofortigen Einzel-Scan für manuelle Auslösung aus (ohne Settle-Delay).</summary>
    public async Task<long?> ScanDirectAsync()
    {
        if (!_ocrEngine.IsAvailable) return null;

        var region = _regionProvider() ?? ScreenCapture.GetDefaultWalletRegion();
        if (!region.IsValid) return null;

        var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
        if (raw == null) return null;

        var (invText, plainText) = await _ocrEngine.RecognizeDualPassAsync(raw, region.Width, region.Height, scale: 2, padding: 6).ConfigureAwait(false);
        var bestText = WalletOcrTrigger.BestRead(invText, plainText);
        var balance = WalletOcrTrigger.ExtractBalance(bestText);

        if (balance is { } val)
        {
            BalanceCaptured?.Invoke(val);
            return val;
        }

        return null;
    }

    /// <summary>Startet manuell oder log-gesteuert einen Capture-Burst.</summary>
    public void Trigger()
    {
        if (!_ocrEngine.IsAvailable) return;

        var region = _regionProvider() ?? ScreenCapture.GetDefaultWalletRegion();
        if (!region.IsValid)
        {
            LastOutcome = "no_region";
            Logger.Log("OCR: Kein mobiGlas-Bereich ermittelbar – Burst abgebrochen.");
            return;
        }

        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            LastOutcome = "busy";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(() => RunBurstAsync(token, region), token);
    }

    private async Task RunBurstAsync(CancellationToken ct, ScanRegion region)
    {
        try
        {
            Logger.Log($"OCR: mobiGlas Trigger erkannt – Burst startet für Bereich {region}...");
            var start = DateTime.UtcNow;

            await Task.Delay(SettleDelay, ct).ConfigureAwait(false);

            for (int grab = 1; grab <= MaxGrabs; grab++)
            {
                if (ct.IsCancellationRequested || DateTime.UtcNow - start > BurstBudget)
                {
                    Finish("timeout");
                    return;
                }

                var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
                if (raw == null)
                {
                    await Task.Delay(RetrySpacing, ct).ConfigureAwait(false);
                    continue;
                }

                var (invText, plainText) = await _ocrEngine.RecognizeDualPassAsync(raw, region.Width, region.Height, scale: 2, padding: 6).ConfigureAwait(false);
                var bestText = WalletOcrTrigger.BestRead(invText, plainText);
                var balance = WalletOcrTrigger.ExtractBalance(bestText);

                if (balance is { } val)
                {
                    Logger.Log($"OCR Grab {grab}: '{bestText?.Trim()}' -> {val:N0} aUEC");

                    // Sofortige Bestätigung bei validem Kontostand
                    Finish("confirmed");
                    BalanceCaptured?.Invoke(val);
                    return;
                }

                await Task.Delay(GrabSpacing, ct).ConfigureAwait(false);
            }

            Finish("unconfirmed");
        }
        catch (OperationCanceledException)
        {
            Finish("canceled");
        }
        catch (Exception ex)
        {
            Logger.Log($"OCR Fehler: {ex.Message}");
            Finish("error");
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    private void Finish(string outcome)
    {
        LastOutcome = outcome;
        Logger.Log($"OCR: Burst beendet mit Ergebnis: {outcome}");
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
