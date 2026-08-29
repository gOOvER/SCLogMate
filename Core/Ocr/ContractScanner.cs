using System;
using System.Timers;
using SCLogReader.Models;

namespace SCLogReader.Core.Ocr;

/// <summary>
/// Pollt den Auftragsbereich im 1000ms-Intervall, extrahiert angenommene Aufträge aus dem
/// "ACCEPTED"-Tab im mobiGlas und meldet neue/aktualisierte Verträge an das ViewModel.
/// Basiert auf dem bewährten NexusApp-Muster.
/// </summary>
public sealed class ContractScanner : IDisposable
{
    private readonly OcrEngineService _ocrEngine;
    private readonly Func<ScanRegion?> _regionProvider;
    private readonly Func<bool> _isEnabled;
    private System.Timers.Timer? _timer;
    private bool _running;
    private bool _busy;
    private string _lastKey = "";

    public event Action<ContractDetails>? ContractScanned;
    public event Action<bool>? RunningChanged;
    public event Action<string>? StageChanged;

    public bool IsRunning => _running;
    public string? LastStage { get; private set; }

    public ContractScanner(OcrEngineService ocrEngine, Func<ScanRegion?> regionProvider, Func<bool> isEnabled)
    {
        _ocrEngine = ocrEngine;
        _regionProvider = regionProvider;
        _isEnabled = isEnabled;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        Logger.Log("[CONTRACT] Contract-Scanner gestartet (Polling @ 1000ms)");
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = false;
        _timer.Start();
        RunningChanged?.Invoke(true);
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        Logger.Log("[CONTRACT] Contract-Scanner gestoppt");
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        RunningChanged?.Invoke(false);
    }

    private async void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (_busy) return;
        _busy = true;

        try
        {
            if (!_isEnabled())
            {
                SetStage("disabled");
                return;
            }

            var region = _regionProvider();
            if (region == null || !region.IsValid)
            {
                SetStage("noregion");
                return;
            }

            if (!_ocrEngine.IsAvailable)
            {
                SetStage("unavail");
                return;
            }

            var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
            if (raw == null)
            {
                SetStage("notext");
                return;
            }

            var text = await _ocrEngine.RecognizeSinglePassAsync(raw, region.Width, region.Height);
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStage("notext");
                return;
            }

            // Streng nur im ACCEPTED-Tab auswerten
            var details = ContractParser.Parse(text, requireAccepted: true);
            if (details != null && details.Reward > 0 && details.Title.Length >= 3)
            {
                SetStage("parsed");
                var key = $"{ContractParser.NormalizeTitle(details.Title)}:{details.Reward}";
                if (key != _lastKey)
                {
                    _lastKey = key;
                    Logger.Log($"[CONTRACT] Angenommener Auftrag erfasst: '{details.Title}' ({details.ContractedBy}) · Belohnung: {details.Reward:N0} aUEC");
                    ContractScanned?.Invoke(details);
                }
            }
            else
            {
                if (ContractParser.IsAcceptedContract(text))
                    SetStage("accepted_unreadable");
                else
                    SetStage("not_accepted_tab");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("[CONTRACT] Scan Tick Fehler", ex);
            SetStage("error");
        }
        finally
        {
            _busy = false;
            if (_running)
            {
                try { _timer?.Start(); }
                catch (ObjectDisposedException) { }
            }
        }
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
