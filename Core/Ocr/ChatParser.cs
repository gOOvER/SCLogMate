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
    // [GLOBAL] Minoofie: text
    // [SC KRAUTZ] AnthonyBenson: text
    // [SékRAUTZj AnthonyBenson:
    // tsc KRAUTZ] Ravaxx:
    // 'IS&KRAUTZI goovERi
    // Erkennt Chat-Header wie:
    // [GLOBAL] Minoofie: text
    // [SC KRAUTZ] AnthonyBenson: text
    // [SékRAUTZj AnthonyBenson:
    // tsc KRAUTZ] Ravaxx:
    // 'IS&KRAUTZI goovERi
    // [Direct] Pilot to Target: whisper
    private static readonly Regex HeaderPattern = new(
        @"^(?:(?:['`""~!\*]*[\[\(\{I|tl1](?<channel>[a-zA-Z0-9\s&_\-\.\p{L}]{2,25})[\]\)\}\|I1lj\>]\s*)+)\[?(?:(?:From\s+)?(?<sender>[a-zA-Z0-9_\-\.]{2,25}?)(?:\s+to\s+\[?(?<recipient>[a-zA-Z0-9_\-\.]{2,25})\]?)?)\]?(?:\s*[:\-\.;i]\s*|\s+)(?<inlineMsg>.*)$",
        RegexOptions.Compiled);

    // Standalone Header wie "[SC KRAUTZ] AnthonyBenson:" auf einer eigenen Zeile
    private static readonly Regex StandaloneHeaderPattern = new(
        @"^(?:(?:['`""~!\*]*[\[\(\{I|tl1](?<channel>[a-zA-Z0-9\s&_\-\.\p{L}]{2,25})[\]\)\}\|I1lj\>]\s*)+)\[?(?:(?:From\s+)?(?<sender>[a-zA-Z0-9_\-\.]{2,25}?)(?:\s+to\s+\[?(?<recipient>[a-zA-Z0-9_\-\.]{2,25})\]?)?)\]?\s*[:\-\.;i]?$",
        RegexOptions.Compiled);

    // Reine Kanal-Zeile wie "[GLOBAL]" oder "[SC KRAUTZ]" (ohne Sender auf derselben Zeile)
    private static readonly Regex ChannelOnlyPattern = new(
        @"^(?:(?:['`""~!\*]*[\[\(\{I|tl1](?<channel>[a-zA-Z0-9\s&_\-\.\p{L}]{2,25})[\]\)\}\|I1lj\>]\s*)+)$",
        RegexOptions.Compiled);

    // Fallback für einfache "Player: Text" Zeilen (ohne Kanal-Tag davor)
    private static readonly Regex ColonSenderPattern = new(
        @"^\[?(?<sender>[a-zA-Z0-9_\-\.]{2,25})\]?\s*[:\-]\s*(?<inlineMsg>.*)$",
        RegexOptions.Compiled);

    // Erkennt eingebettete neue Nachrichten innerhalb einer Zeile (falls OCR zwei Chatzeilen zusammenzieht)
    private static readonly Regex EmbeddedHeaderSplitPattern = new(
        @"(?<=\S)\s+(?=(?:['`""~!\*]*[\[\(\{I|tl1][a-zA-Z0-9\s&_\-\.\p{L}]{2,25}[\]\)\}\|I1lj\>]\s*)+[a-zA-Z0-9_\-\.]{2,25}\s*[:\-\.;i])",
        RegexOptions.Compiled);

    private static readonly HashSet<string> IgnoredPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "press f12", "press enter", "commlink", "comm-link", "chat channel",
        "armistice zone", "shields active", "landing complete", "quantum drive",
        "power on", "power off", "engines on", "engines off", "scan mode",
        "contacts", "broadcast", "channel list", "members (", "online ("
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "is", "it", "to", "in", "at", "on", "no", "so", "do", "we", "he", "me", "my",
        "if", "or", "an", "as", "am", "be", "by", "up", "ok", "of", "and", "the", "for"
    };

    public static List<ChatMessageDto> ParseChatLines(string rawOcrText, string? sessionId = null)
    {
        var list = new List<ChatMessageDto>();
        if (string.IsNullOrWhiteSpace(rawOcrText)) return list;

        var rawLines = rawOcrText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var now = DateTime.UtcNow.ToString("o");

        // Vorab: Zeilen aufteilen, falls Windows OCR mehrere Nachrichten in eine Zeile gepackt hat
        var lines = new List<string>();
        foreach (var r in rawLines)
        {
            if (string.IsNullOrWhiteSpace(r)) continue;
            var subLines = EmbeddedHeaderSplitPattern.Split(r);
            foreach (var s in subLines)
            {
                if (!string.IsNullOrWhiteSpace(s)) lines.Add(s.Trim());
            }
        }

        ChatMessageDto? current = null;
        string? pendingChannel = null;

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

            // Ignorieren einzelner Rauschzeichen (z. B. isolierte Zahlen oder Symbole)
            if (line.Length <= 2 && !char.IsLetter(line[0])) continue;

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

            // 0. Reine Kanal-Zeile: [GLOBAL] oder [SC KRAUTZ]
            var mChanOnly = ChannelOnlyPattern.Match(line);
            if (mChanOnly.Success)
            {
                FinalizeCurrent();
                pendingChannel = NormalizeChannel(mChanOnly.Groups["channel"].Value.Trim());
                continue;
            }

            // 1. Muster mit Kanal-Header und Inline-Nachricht: [Global] Sender: Message
            var mHeader = HeaderPattern.Match(line);
            var mStandalone = StandaloneHeaderPattern.Match(line);

            if (mHeader.Success && IsLikelyPlayerName(mHeader.Groups["sender"].Value))
            {
                FinalizeCurrent();
                var channelRaw = mHeader.Groups["channel"].Success ? mHeader.Groups["channel"].Value.Trim() : (pendingChannel ?? "Global");
                var sender = mHeader.Groups["sender"].Value.Trim();
                var recipient = mHeader.Groups["recipient"].Success ? mHeader.Groups["recipient"].Value.Trim() : null;
                var inlineMsg = mHeader.Groups["inlineMsg"].Value.Trim();
                pendingChannel = null;

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
            else if (mStandalone.Success && IsLikelyPlayerName(mStandalone.Groups["sender"].Value))
            {
                FinalizeCurrent();
                var channelRaw = mStandalone.Groups["channel"].Success ? mStandalone.Groups["channel"].Value.Trim() : (pendingChannel ?? "Global");
                var sender = mStandalone.Groups["sender"].Value.Trim();
                var recipient = mStandalone.Groups["recipient"].Success ? mStandalone.Groups["recipient"].Value.Trim() : null;
                pendingChannel = null;

                current = new ChatMessageDto
                {
                    Timestamp = now,
                    SessionId = sessionId,
                    Channel = NormalizeChannel(channelRaw),
                    Sender = sender,
                    Recipient = recipient,
                    Message = "",
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

                if (IsLikelyPlayerName(sender, requireStrict: pendingChannel == null))
                {
                    FinalizeCurrent();
                    var ch = pendingChannel ?? "Global";
                    pendingChannel = null;

                    current = new ChatMessageDto
                    {
                        Timestamp = now,
                        SessionId = sessionId,
                        Channel = ch,
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
        var r = raw.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(r)) return "Global";
        if (r.Contains("glo") || r.Contains("bal") || r.Contains("all") || r.Contains("server") || r.Contains("system")) return "Global";
        if (r.Contains("party") || r.Contains("gruppe") || r.Contains("crew") || r.Contains("team")) return "Party";
        if (r.Contains("direct") || r.Contains("whisper") || r.Contains("flüster") || r.Contains("pm") || r.Contains("dm") || r.Contains("privat")) return "Direct";

        // Bereinigung für Custom / Org Channels (z. B. "SC KRAUTZ", "SékRAUTZ", "IS&KRAUTZ")
        var cleaned = raw.Trim(' ', '[', ']', '(', ')', '{', '}', '\'', '"', '~', '|', 'j', 't', 'I', 'l')
                         .Replace('&', 'C')
                         .Replace('é', 'c')
                         .Replace('É', 'C');

        if (cleaned.Length >= 2)
        {
            var title = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
            if (title.StartsWith("Sc ", StringComparison.OrdinalIgnoreCase))
            {
                title = "SC " + title.Substring(3);
            }
            else if (title.StartsWith("Sck", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Sék", StringComparison.OrdinalIgnoreCase))
            {
                title = "SC " + title.Substring(3).TrimStart();
            }
            return title;
        }

        return "Global";
    }

    private static string CleanHandle(string raw)
    {
        // Entfernt führende/nachfolgende OCR-Artefakte wie Klammern, Doppelpunkte, Tilden, Anführungszeichen
        return raw.Trim(' ', '[', ']', '(', ')', ':', '<', '>', '~', '|', '-', '{', '}', '\'', '"', ';')
                  .Replace('Ø', '0')
                  .Replace('ø', '0');
    }

    private static bool IsLikelyPlayerName(string s, bool requireStrict = false)
    {
        var clean = CleanHandle(s);
        if (clean.Length < 2 || clean.Length > 25) return false;
        if (int.TryParse(clean, out _)) return false;
        var lower = clean.ToLowerInvariant();
        if (lower is "http" or "https" or "error" or "warning" or "info" or "size" or "scu" or "time" or "date" or "name" or "press" or "f12" or "enter" or "online" or "members" or "global" or "party" or "direct") return false;
        if (StopWords.Contains(lower)) return false;
        if (requireStrict && clean.Length < 3) return false;
        return true;
    }
}
