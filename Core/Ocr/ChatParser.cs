using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Extrahiert strukturierte Chat-Nachrichten (Kanal, Sender, Empfänger, Text) aus rohen OCR-Textblöcken des Star Citizen Chatfensters.
/// Unterstützt ein- und mehrzeilige Chat-Nachrichten sowie flexible OCR-Formatierungsabgleiche.
/// </summary>
public static class ChatParser
{
    // Erkennt Chat-Header wie:
    // [GLOBAL] Minoofie:
    // [GLOBAL) Vanduul40k:
    // [GLOBALI gOOvER: not really
    // BALI Dragon-heart: any SoO group going?
    // [Party] Wingman: Ready
    // [Direct] Pilot to Target: whisper
    private static readonly Regex HeaderPattern = new(
        @"^(?:[\[\(\{]?(?<channel>GLOBAL|PARTY|DIRECT|WHISPER|SYSTEM|TEAM|CREW|GROUP|BALI)[\]\)\|\}I1l\:]*|[\[\(\{](?<channel>[a-zA-Z]{3,10})[\]\)\|\}I1l\:]*)\s*(?:(?:From\s+)?\[?(?<sender>[a-zA-Z0-9_\-\.]{2,30})\]?(?:\s+to\s+\[?(?<recipient>[a-zA-Z0-9_\-\.]{2,30})\]?)?)\s*(?:[:\-])?\s*(?<inlineMsg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Fallback für einfache "Player: Text" Zeilen
    private static readonly Regex ColonSenderPattern = new(
        @"^\[?(?<sender>[a-zA-Z0-9_\-\.]{2,25})\]?\s*[:\-]\s*(?<inlineMsg>.*)$",
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

        ChatMessageDto? current = null;

        void FinalizeCurrent()
        {
            if (current != null && !string.IsNullOrWhiteSpace(current.Sender) && !string.IsNullOrWhiteSpace(current.Message))
            {
                current.Message = current.Message.Trim();
                current.Sender = CleanHandle(current.Sender);
                if (!string.IsNullOrEmpty(current.Recipient))
                {
                    current.Recipient = CleanHandle(current.Recipient);
                }

                if (current.Message.Length >= 2 && IsLikelyPlayerName(current.Sender))
                {
                    list.Add(current);
                }
            }
            current = null;
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim()
                              .Replace('Ø', '0')
                              .Replace('ø', '0');

            if (line.Length < 2) continue;

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

            // 1. Muster mit Kanal-Header: [Global] Sender: Message oder [Global] Sender:
            var mHeader = HeaderPattern.Match(line);
            if (mHeader.Success)
            {
                FinalizeCurrent();
                var channelRaw = mHeader.Groups["channel"].Value.Trim();
                var sender = mHeader.Groups["sender"].Value.Trim();
                var recipient = mHeader.Groups["recipient"].Success ? mHeader.Groups["recipient"].Value.Trim() : null;
                var inlineMsg = mHeader.Groups["inlineMsg"].Value.Trim();

                current = new ChatMessageDto
                {
                    Timestamp = now,
                    SessionId = sessionId,
                    Channel = NormalizeChannel(channelRaw),
                    Sender = sender,
                    Recipient = recipient,
                    Message = inlineMsg,
                    RawOcr = line,
                    CreatedAt = now
                };
                continue;
            }

            // 2. Einfaches Muster: "PilotName: Nachricht" oder "PilotName:"
            var mColon = ColonSenderPattern.Match(line);
            if (mColon.Success)
            {
                var sender = mColon.Groups["sender"].Value.Trim();
                var inlineMsg = mColon.Groups["inlineMsg"].Value.Trim();

                if (IsLikelyPlayerName(sender))
                {
                    FinalizeCurrent();
                    current = new ChatMessageDto
                    {
                        Timestamp = now,
                        SessionId = sessionId,
                        Channel = "Global",
                        Sender = sender,
                        Message = inlineMsg,
                        RawOcr = line,
                        CreatedAt = now
                    };
                    continue;
                }
            }

            // 3. Fortsetzungszeile (Multi-line Chatnachricht der aktuellen Nachricht anhängen)
            if (current != null)
            {
                if (string.IsNullOrEmpty(current.Message))
                {
                    current.Message = line;
                }
                else
                {
                    current.Message += " " + line;
                }
                current.RawOcr += "\n" + line;
            }
        }

        FinalizeCurrent();
        return list;
    }

    private static string NormalizeChannel(string raw)
    {
        var r = raw.ToLowerInvariant();
        if (r.Contains("glo") || r.Contains("bal") || r.Contains("all") || r.Contains("server") || r.Contains("system")) return "Global";
        if (r.Contains("party") || r.Contains("gruppe") || r.Contains("crew") || r.Contains("team")) return "Party";
        if (r.Contains("direct") || r.Contains("whisper") || r.Contains("flüster") || r.Contains("pm") || r.Contains("dm")) return "Direct";
        return char.ToUpperInvariant(raw[0]) + (raw.Length > 1 ? raw.Substring(1).ToLowerInvariant() : "");
    }

    private static string CleanHandle(string raw)
    {
        // Entfernt führende/nachfolgende OCR-Artefakte wie Klammern, Doppelpunkte, Tilden
        return raw.Trim(' ', '[', ']', '(', ')', ':', '<', '>', '~', '|', '-', '{', '}')
                  .Replace('Ø', '0')
                  .Replace('ø', '0');
    }

    private static bool IsLikelyPlayerName(string s)
    {
        if (s.Length < 2 || s.Length > 30) return false;
        if (int.TryParse(s, out _)) return false;
        var lower = s.ToLowerInvariant();
        if (lower is "http" or "https" or "error" or "warning" or "info" or "size" or "scu" or "time" or "date" or "name" or "press" or "f12" or "enter" or "online" or "members") return false;
        return true;
    }
}
