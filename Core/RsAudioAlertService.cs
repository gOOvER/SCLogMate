using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SCLogMate.Models;

namespace SCLogMate.Core;

/// <summary>
/// Leichtgewichtiger Dienst für akustische RS-Radar Alarme (Sci-Fi Sonar-Chime &amp; optionale Windows-TTS Sprachausgabe).
/// </summary>
public static class RsAudioAlertService
{
    private static class NativeSound
    {
        private const uint SND_ASYNC = 0x0001;
        private const uint SND_MEMORY = 0x0004;

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern bool PlaySound(byte[] ptrToSound, IntPtr hmod, uint fdwSound);

        public static void PlayMemoryWav(byte[] wavBytes)
        {
            PlaySound(wavBytes, IntPtr.Zero, SND_ASYNC | SND_MEMORY);
        }
    }

    private static int _lastAlertedRs;
    private static string? _lastAlertedResource;
    private static DateTime _lastAlertTimestamp = DateTime.MinValue;
    private static DateTime _lastSeenTimestamp = DateTime.MinValue;
    private static readonly ConcurrentDictionary<string, DateTime> _lastResourceAlertTime = new(StringComparer.OrdinalIgnoreCase);

    private static byte[]? _cachedSonarWav;
    private static readonly object _sonarLock = new();

    /// <summary>
    /// Setzt die Alarm-Sperre zurück (z.B. bei manuellem Eingriff oder neuem Scan).
    /// </summary>
    public static void ResetTargetAlertLatch()
    {
        _lastAlertedRs = 0;
        _lastAlertedResource = null;
        _lastAlertTimestamp = DateTime.MinValue;
        _lastSeenTimestamp = DateTime.MinValue;
        _lastResourceAlertTime.Clear();
    }

    /// <summary>
    /// Spielt einen akustischen Alarm für ein erkanntes Zielmaterial ab.
    /// Verhindert lästiges Dauer-Piepen bei wiederholten Radar-Pings desselben Vorkommens.
    /// </summary>
    public static void TriggerTargetAlert(RsMatch match, bool playSound, bool playTts)
    {
        if (!playSound && !playTts) return;

        var now = DateTime.UtcNow;
        var resName = match.Resource.Name;
        int scannedRs = match.ScannedRs;

        // 1. Wenn exakt derselbe RS-Wert aktiv erfasst wird:
        // Solange das Signal kontinuierlich oder in regelmäßigen Radar-Pings (< 45s Abstand)
        // empfangen wird, bleibt der Alarm stumm (Pilot fliegt Ziel bereits an oder baut ab).
        // Erst wenn das Signal für mindestens 45s abreißt, gilt ein erneutes Auftauchen als Neufund.
        if (scannedRs == _lastAlertedRs)
        {
            bool stillInContact = (now - _lastSeenTimestamp).TotalSeconds < 45.0;
            _lastSeenTimestamp = now;
            if (stillInContact)
            {
                return;
            }
        }

        // 2. Wenn dasselbe Material im selben Gebiet liegt (z.B. Salvage Panel 2k -> 4k):
        // Mindestens 60 Sekunden Ruhe für dasselbe Material, um Wiederholungs-Spam im selben Cluster zu verhindern.
        if (_lastResourceAlertTime.TryGetValue(resName, out var lastResTime))
        {
            if ((now - lastResTime).TotalSeconds < 60.0)
            {
                _lastAlertedRs = scannedRs;
                _lastSeenTimestamp = now;
                return;
            }
        }

        // 3. Globaler Mindestabstand von 3.0 Sekunden zwischen verschiedenen Alarmen
        if ((now - _lastAlertTimestamp).TotalSeconds < 3.0)
        {
            return;
        }

        _lastAlertedRs = scannedRs;
        _lastAlertedResource = resName;
        _lastAlertTimestamp = now;
        _lastSeenTimestamp = now;
        _lastResourceAlertTime[resName] = now;

        Logger.Log($"[RsAudioAlertService] Ziel-Alarm ausgelöst für: {resName} ({scannedRs:N0} RS)");

        // 1. Sci-Fi Sonar-Ping (sofort, non-blocking via winmm.dll)
        if (playSound)
        {
            Task.Run(() => PlaySonarPing());
        }

        // 2. Optionale Windows-TTS Sprachausgabe
        if (playTts)
        {
            Task.Run(async () =>
            {
                if (playSound)
                {
                    // Kurze Verzögerung nach dem Sonar-Ping, bevor gesprochen wird
                    await Task.Delay(400);
                }
                await SpeakTargetNameAsync(match.Resource.Name);
            });
        }
    }

    /// <summary>
    /// Spielt einen im RAM synthetisierten Sci-Fi Sonar-Ping ab (44.1 kHz, 16-Bit PCM, 0ms Latenz).
    /// </summary>
    public static void PlaySonarPing()
    {
        try
        {
            byte[] wavBytes;
            lock (_sonarLock)
            {
                _cachedSonarWav ??= GenerateSonarPingWav();
                wavBytes = _cachedSonarWav;
            }

            NativeSound.PlayMemoryWav(wavBytes);
        }
        catch (Exception ex)
        {
            Logger.Error("RsAudioAlertService.PlaySonarPing", ex);
        }
    }

    /// <summary>
    /// Erzeugt ein 44.1 kHz 16-Bit Mono WAV eines weichen Sci-Fi Sonar-Pings (1250 Hz -> 1750 Hz mit Exponential-Decay).
    /// </summary>
    private static byte[] GenerateSonarPingWav()
    {
        const int sampleRate = 44100;
        const double duration = 0.38; // 380 Millisekunden
        int totalSamples = (int)(sampleRate * duration);
        short[] samples = new short[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double progress = (double)i / totalSamples;

            // Frequenzkurve: Schneller Pitch-Up von 1250 Hz auf 1750 Hz im Attack, dann sanft ausklingend
            double freq = 1250.0 + (500.0 * Math.Sin(progress * Math.PI * 0.5));

            // Hüllkurve: 8 ms linearer Attack (kein Klick), dann sanfter exponentieller Decay
            double env;
            if (t < 0.008)
            {
                env = t / 0.008;
            }
            else
            {
                env = Math.Exp(-7.5 * (t - 0.008));
            }

            // Obertöne für futuristischen Sci-Fi Sonar-Sound
            double sampleValue = Math.Sin(2.0 * Math.PI * freq * t) * 0.75 +
                                 Math.Sin(2.0 * Math.PI * (freq * 2.0) * t) * 0.25;

            samples[i] = (short)(sampleValue * env * 28000.0);
        }

        // WAV File Header (RIFF 44 Bytes)
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int subChunk2Size = totalSamples * 2; // 16 Bit = 2 Bytes pro Sample
        int chunkSize = 36 + subChunk2Size;

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size für PCM
        bw.Write((short)1); // PCM Format
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // ByteRate
        bw.Write((short)2); // BlockAlign
        bw.Write((short)16); // BitsPerSample
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(subChunk2Size);

        foreach (var sample in samples)
        {
            bw.Write(sample);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Liest den Materialnamen über die native Windows Text-to-Speech Engine vor.
    /// </summary>
    private static async Task SpeakTargetNameAsync(string resourceName)
    {
        try
        {
            var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            var text = resourceName.Equals("Salvage Panel", StringComparison.OrdinalIgnoreCase)
                ? "Salvage Panels erfasst"
                : $"{resourceName} geortet";

            var stream = await synth.SynthesizeTextToStreamAsync(text);
            var player = new Windows.Media.Playback.MediaPlayer();
            player.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, stream.ContentType);
            player.Play();
        }
        catch (Exception ex)
        {
            Logger.Error("RsAudioAlertService.SpeakTargetNameAsync", ex);
        }
    }
}
