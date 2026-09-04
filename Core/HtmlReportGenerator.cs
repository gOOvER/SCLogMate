using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SCLogMate.Models;

namespace SCLogMate.Core;

public static class HtmlReportGenerator
{
    public static string GenerateFlightReportHtml(
        string sessionLabel,
        IEnumerable<FlightTimelineItem> items,
        FlightSummary summary,
        string pilotName = "Pilot",
        string systemName = "Stanton")
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"de\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>SCLogMate Flugbericht — {sessionLabel}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
    :root {
      --bg: #070B12;
      --card-bg: #0D1522;
      --card-border: #1A2638;
      --accent: #38BDF8;
      --accent-glow: rgba(56, 189, 248, 0.25);
      --green: #4ADE80;
      --red: #F87171;
      --gold: #FBBF24;
      --text: #F0F6FC;
      --text-muted: #8B949E;
      --font-mono: 'Cascadia Code', 'Consolas', monospace;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background-color: var(--bg);
      color: var(--text);
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      padding: 30px 20px;
      display: flex;
      justify-content: center;
    }
    .container {
      width: 100%;
      max-width: 1100px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    .header {
      background: linear-gradient(135deg, #0C1A2B 0%, #070D18 100%);
      border: 1px solid #1E3A5F;
      border-radius: 12px;
      padding: 24px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      box-shadow: 0 8px 24px rgba(0,0,0,0.4);
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 14px;
    }
    .brand-icon {
      font-size: 32px;
      background: #11263F;
      border: 1px solid var(--accent);
      border-radius: 8px;
      width: 52px;
      height: 52px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .brand-title {
      font-size: 20px;
      font-weight: 800;
      letter-spacing: 0.5px;
      color: #FFF;
    }
    .brand-sub {
      font-size: 12px;
      color: var(--text-muted);
      margin-top: 4px;
    }
    .badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.5px;
      text-transform: uppercase;
      background: #142842;
      color: var(--accent);
      border: 1px solid #1F436E;
    }
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 14px;
    }
    .kpi-card {
      background: var(--card-bg);
      border: 1px solid var(--card-border);
      border-radius: 10px;
      padding: 18px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .kpi-label {
      font-size: 10.5px;
      font-weight: 700;
      color: var(--text-muted);
      letter-spacing: 0.5px;
      text-transform: uppercase;
    }
    .kpi-val {
      font-size: 20px;
      font-weight: 800;
      color: var(--text);
    }
    .kpi-sub {
      font-size: 11.5px;
      color: var(--text-muted);
    }
    .timeline-card {
      background: var(--card-bg);
      border: 1px solid var(--card-border);
      border-radius: 12px;
      padding: 24px;
    }
    .section-title {
      font-size: 14px;
      font-weight: 700;
      color: var(--accent);
      letter-spacing: 0.5px;
      text-transform: uppercase;
      margin-bottom: 20px;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .timeline {
      position: relative;
      padding-left: 28px;
    }
    .timeline::before {
      content: '';
      position: absolute;
      left: 10px;
      top: 10px;
      bottom: 10px;
      width: 2px;
      background: linear-gradient(180deg, var(--accent) 0%, #1E3A5F 100%);
    }
    .item {
      position: relative;
      margin-bottom: 20px;
      background: #080D16;
      border: 1px solid var(--card-border);
      border-radius: 8px;
      padding: 14px 16px;
      transition: transform 0.15s ease;
    }
    .item:hover {
      border-color: var(--accent);
      transform: translateX(4px);
    }
    .node {
      position: absolute;
      left: -24px;
      top: 16px;
      width: 14px;
      height: 14px;
      border-radius: 50%;
      background: var(--card-bg);
      border: 2px solid var(--accent);
      box-shadow: 0 0 8px var(--accent-glow);
    }
    .item-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 6px;
    }
    .item-title {
      font-size: 13.5px;
      font-weight: 700;
      color: #FFF;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .ship-pill {
      font-size: 10px;
      padding: 2px 6px;
      border-radius: 4px;
      background: #0F253E;
      border: 1px solid #1C4B78;
      color: var(--accent);
    }
    .time-tag {
      font-family: var(--font-mono);
      font-size: 11.5px;
      color: var(--text-muted);
    }
    .item-sub {
      font-size: 12px;
      color: #94A3B8;
      margin-bottom: 4px;
    }
    .item-meta {
      display: flex;
      gap: 16px;
      font-size: 11px;
      color: var(--text-muted);
      margin-top: 8px;
      padding-top: 6px;
      border-top: 1px solid #131E2C;
    }
    .footer {
      text-align: center;
      font-size: 11.5px;
      color: #526071;
      margin-top: 10px;
    }
  ");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");

        // Header
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <div class=\"brand\">");
        sb.AppendLine("        <div class=\"brand-icon\">🚀</div>");
        sb.AppendLine("        <div>");
        sb.AppendLine($"          <div class=\"brand-title\">STAR CITIZEN FLUSCHSCHREIBER-BERICHT</div>");
        sb.AppendLine($"          <div class=\"brand-sub\">Session: {sessionLabel} · Generiert am {DateTime.Now:dd.MM.yyyy HH:mm} Uhr</div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div><span class=\"badge\">SCLogMate Telemetrie</span></div>");
        sb.AppendLine("    </div>");

        // KPI Grid
        sb.AppendLine("    <div class=\"kpi-grid\">");
        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-label\">🌌 Flugdistanz (Quantum)</div>");
        sb.AppendLine($"        <div class=\"kpi-val\" style=\"color: #818CF8;\">{summary.TotalDistanceText}</div>");
        sb.AppendLine($"        <div class=\"kpi-sub\">{summary.QuantumJumps} Quantum-Sprünge</div>");
        sb.AppendLine("      </div>");

        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-label\">⏱ Cockpit Flugzeit</div>");
        sb.AppendLine($"        <div class=\"kpi-val\" style=\"color: #38BDF8;\">{(int)summary.TotalFlightDuration.TotalHours:D2}:{summary.TotalFlightDuration.Minutes:D2} Std.</div>");
        sb.AppendLine($"        <div class=\"kpi-sub\">von {(int)summary.TotalSessionDuration.TotalHours:D2}:{summary.TotalSessionDuration.Minutes:D2} Std. Gesamt</div>");
        sb.AppendLine("      </div>");

        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-label\">🛸 Schiffseinsätze</div>");
        sb.AppendLine($"        <div class=\"kpi-val\" style=\"color: #38BDF8;\">{summary.SortieCount} Starts</div>");
        sb.AppendLine($"        <div class=\"kpi-sub\">{summary.ShipsUsedText}</div>");
        sb.AppendLine("      </div>");

        sb.AppendLine("      <div class=\"kpi-card\">");
        sb.AppendLine("        <div class=\"kpi-label\">🪐 Destinationen</div>");
        sb.AppendLine($"        <div class=\"kpi-val\" style=\"color: #4ADE80;\">{summary.VisitedBodies.Count} Himmelskörper</div>");
        sb.AppendLine($"        <div class=\"kpi-sub\">{summary.ShipLossesText}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Timeline
        sb.AppendLine("    <div class=\"timeline-card\">");
        sb.AppendLine("      <div class=\"section-title\">🛰 Chronologischer Flugschreiber (Black Box)</div>");
        sb.AppendLine("      <div class=\"timeline\">");

        foreach (var it in items)
        {
            sb.AppendLine("        <div class=\"item\">");
            sb.AppendLine("          <div class=\"node\"></div>");
            sb.AppendLine("          <div class=\"item-header\">");
            sb.AppendLine($"            <div class=\"item-title\">{it.IconGlyph} {it.Title} {(string.IsNullOrEmpty(it.ShipName) ? "" : $"<span class=\"ship-pill\">{it.ShipName}</span>")}</div>");
            sb.AppendLine($"            <div class=\"time-tag\">{it.FormattedTime} ({it.RelativeTimeText})</div>");
            sb.AppendLine("          </div>");
            if (!string.IsNullOrEmpty(it.Subtitle))
            {
                sb.AppendLine($"          <div class=\"item-sub\">{it.Subtitle}</div>");
            }
            sb.AppendLine("          <div class=\"item-meta\">");
            sb.AppendLine($"            <span>📍 {it.LocationName}</span>");
            sb.AppendLine($"            <span>🌌 {it.SystemName}</span>");
            if (!string.IsNullOrEmpty(it.DistanceText))
            {
                sb.AppendLine($"            <span style=\"color: var(--accent); font-weight: bold;\">Distanz: {it.DistanceText}</span>");
            }
            sb.AppendLine("          </div>");
            sb.AppendLine("        </div>");
        }

        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Footer
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine($"      SCLogMate v1.0.0-beta5 · Erstellt für Star Citizen Piloten · <a href=\"https://github.com/gOOvER/SCLogMate\" style=\"color: #38BDF8; text-decoration: none;\">github.com/gOOvER/SCLogMate</a>");
        sb.AppendLine("    </div>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public static string ExportAndOpen(string sessionLabel, IEnumerable<FlightTimelineItem> items, FlightSummary summary)
    {
        var html = GenerateFlightReportHtml(sessionLabel, items, summary);
        var filename = $"Flugbericht_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var outPath = Path.Combine(Path.GetTempPath(), filename);
        File.WriteAllText(outPath, html, Encoding.UTF8);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = outPath,
                UseShellExecute = true
            });
        }
        catch { }

        return outPath;
    }
}
