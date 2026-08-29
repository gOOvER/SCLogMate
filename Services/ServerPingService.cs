using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SCLogReader.Services;

/// <summary>
/// Misst die Netzwerk-Latenz (Ping / RTT) zum entsprechenden Star Citizen / AWS Server-Rechenzentrum.
/// Verwendet zuverlässigen TCP-Handshake auf Port 443 (funktioniert auch bei ICMP-Blockaden).
/// </summary>
public static class ServerPingService
{
    public static string GetRegionalHost(string shard)
    {
        var s = (shard ?? "").ToLowerInvariant();
        if (s.Contains("euc") || s.Contains("fra") || s.Contains("ger"))
            return "ec2.eu-central-1.amazonaws.com"; // Frankfurt
        if (s.Contains("euw") || s.Contains("eu") || s.Contains("lon") || s.Contains("irl"))
            return "ec2.eu-west-1.amazonaws.com"; // Irland / Europa
        if (s.Contains("use") || s.Contains("va") || s.Contains("us-east"))
            return "ec2.us-east-1.amazonaws.com"; // US East (Virginia)
        if (s.Contains("usw") || s.Contains("or") || s.Contains("us-west") || s.Contains("us"))
            return "ec2.us-west-2.amazonaws.com"; // US West (Oregon)
        if (s.Contains("aus") || s.Contains("oce") || s.Contains("ap") || s.Contains("syd"))
            return "ec2.ap-southeast-2.amazonaws.com"; // Australien (Sydney)
        if (s.Contains("asia") || s.Contains("jp") || s.Contains("sg") || s.Contains("tyo"))
            return "ec2.ap-northeast-1.amazonaws.com"; // Asien (Tokio)

        return "ec2.eu-west-1.amazonaws.com";
    }

    public static async Task<long?> MeasureLatencyAsync(string shard, int timeoutMs = 1500, CancellationToken ct = default)
    {
        var host = GetRegionalHost(shard);
        try
        {
            using var client = new TcpClient();
            var sw = Stopwatch.StartNew();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            var connectTask = client.ConnectAsync(host, 443, cts.Token).AsTask();
            await connectTask;
            sw.Stop();

            if (client.Connected)
            {
                return sw.ElapsedMilliseconds;
            }
        }
        catch
        {
            // Ignorieren bei Timeout oder Verbindungsunterbrechung
        }
        return null;
    }
}
