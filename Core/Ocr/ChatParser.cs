using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Extrahiert strukturierte Chat-Nachrichten (Kanal, Sender, Empfänger, Text) aus rohen OCR-Textblöcken des Star Citizen Chatfensters.
/// </summary>
public static class ChatParser
{
    private static readonly Regex BracketChatPattern = new(
        @"^(?:\[(?<channel>[^\]]+)\]|\((?<channel>[^\)]+)\)|(?<channel>Global|Party|Direct|Whisper|Group)\s*[:\|])\s*(?:(?:From\s+)?\[?(?<sender>[a-zA-Z0-9_\-\.]{2,30})\]?(?:\s+to\s+\[?(?<recipient>[a-zA-Z0-9_\-\.]{2,30})\]?)?\s*[:\-])\s*(?<message>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SimpleColonPattern = new(
        @"^(?<sender>[a-zA-Z0-9_\-\.]{2,25})\s*:\s*(?<message>.+)$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> IgnoredPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "press f12", "press enter", "commlink", "comm-link", "chat channel",
        "armistice zone", "shields active", "landing complete", "quantum drive",
        "power on", "power off", "engines on", "engines off", "scan mode",
        "contacts", "broadcast", "channel list", "members (", "online ("
    };

    public static List<ChatMessageDto> ParseChatLines(string rawOcrText, string? sessionId = null)
    {
        var list = new List<ChatMessageDto>();
        if (string.IsNullOrWhiteSpace(rawOcrText)) return list;

        var lines = rawOcrText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var now = DateTime.UtcNow.ToString("o");

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length < 4) continue;

            // Ignorieren offensichtlicher HUD- oder Menü-Fragmente
            bool shouldIgnore = false;
            foreach (var phrase in IgnoredPhrases)
            {
                if (line.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    shouldIgnore = true;
                    break;
                }
            }
            if (shouldIgnore) continue;

            // 1. Muster mit Kanal-Tags [Global], [Party], [Direct]
            var m = BracketChatPattern.Match(line);
            if (m.Success)
            {
                var channelRaw = m.Groups["channel"].Value.Trim();
                var sender = m.Groups["sender"].Value.Trim();
                var recipient = m.Groups["recipient"].Success ? m.Groups["recipient"].Value.Trim() : null;
                var msg = m.Groups["message"].Value.Trim();

                if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(msg)) continue;

                var normalizedChannel = NormalizeChannel(channelRaw);

                list.Add(new ChatMessageDto
                {
                    Timestamp = now,
                    SessionId = sessionId,
                    Channel = normalizedChannel,
                    Sender = CleanHandle(sender),
                    Recipient = !string.IsNullOrEmpty(recipient) ? CleanHandle(recipient) : null,
                    Message = msg,
                    RawOcr = line,
                    CreatedAt = now
                });
                continue;
            }

            // 2. Einfaches Muster: "PilotName: Nachricht"
            var mSimple = SimpleColonPattern.Match(line);
            if (mSimple.Success)
            {
                var sender = mSimple.Groups["sender"].Value.Trim();
                var msg = mSimple.Groups["message"].Value.Trim();

                // Validierung: Sender darf kein reines Wort wie 'http', 'https', 'size', 'scu' sein
                if (IsLikelyPlayerName(sender) && !string.IsNullOrWhiteSpace(msg) && msg.Length > 1)
                {
                    list.Add(new ChatMessageDto
                    {
                        Timestamp = now,
                        SessionId = sessionId,
                        Channel = "Global",
                        Sender = CleanHandle(sender),
                        Message = msg,
                        RawOcr = line,
                        CreatedAt = now
                    });
                }
            }
        }

        return list;
    }

    private static string NormalizeChannel(string raw)
    {
        var r = raw.ToLowerInvariant();
        if (r.Contains("global") || r.Contains("all") || r.Contains("server") || r.Contains("system")) return "Global";
        if (r.Contains("party") || r.Contains("gruppe") || r.Contains("crew") || r.Contains("team")) return "Party";
        if (r.Contains("direct") || r.Contains("whisper") || r.Contains("flüster") || r.Contains("pm") || r.Contains("dm")) return "Direct";
        return char.ToUpperInvariant(raw[0]) + (raw.Length > 1 ? raw.Substring(1) : "");
    }

    private static string CleanHandle(string raw)
    {
        // Entfernt führende/nachfolgende OCR-Artefakte wie Unterstriche, Tildes, Punkte
        return raw.Trim(' ', '[', ']', '(', ')', ':', '<', '>', '~', '|', '-');
    }

    private static bool IsLikelyPlayerName(string s)
    {
        if (s.Length < 2 || s.Length > 30) return false;
        if (int.TryParse(s, out _)) return false;
        var lower = s.ToLowerInvariant();
        if (lower is "http" or "https" or "error" or "warning" or "info" or "size" or "scu" or "time" or "date" or "name") return false;
        return true;
    }
}
