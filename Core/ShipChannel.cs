using System;

namespace SCLogMate.Core;

/// <summary>
/// Art der Benachrichtigung eines Schiffs-Kommunikationskanals.
/// </summary>
public enum ChannelMoment
{
    /// <summary>Der lokale Spieler hat ein Schiff betreten (eigenes oder fremdes).</summary>
    YouBoarded,

    /// <summary>Ein anderer Spieler hat das Schiff betreten, auf dem man sich befindet.</summary>
    TheyBoarded,

    /// <summary>Ein Spieler hat das Schiff verlassen.</summary>
    TheyLeft
}

/// <summary>
/// Gelesenes Ereignis eines Schiffs-Kommunikationskanals.
/// </summary>
/// <param name="At">Zeitstempel des Log-Eintrags.</param>
/// <param name="Ship">Anzeigename des Fahrzeugs / Schiffs.</param>
/// <param name="Owner">Besitzer des Schiffs (Spieler-Handle).</param>
/// <param name="Handle">Spieler, auf den sich die Aktion bezieht (null bei YouBoarded).</param>
/// <param name="Moment">Aktions-Typ (YouBoarded, TheyBoarded, TheyLeft).</param>
public sealed record ChannelNote(
    DateTime At,
    string Ship,
    string Owner,
    string? Handle,
    ChannelMoment Moment);

/// <summary>
/// Liest und analysiert Schiffs-Kommunikationskanäle, die das Spiel öffnet, wenn jemand ein Schiff betritt.
/// Unterscheidet zwischen eigenem Boarding und anderen Besatzungsmitgliedern (Multi-Crew).
/// </summary>
public static class ShipChannel
{
    private const string BoardedEn = "You have joined channel ";
    private const string BoardedDe = "Du bist Kanal ";
    private const string JoinedPrefixEn = "New Member Joined ";
    private const string LeftPrefixEn = "Member Left ";

    /// <summary>
    /// Prüft, ob ein Benachrichtigungstext zu einem Schiffs-Kommunikationskanal gehört.
    /// </summary>
    public static bool IsChannel(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        return text.StartsWith(BoardedEn, StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(BoardedDe, StringComparison.OrdinalIgnoreCase)
            || ((text.StartsWith(JoinedPrefixEn, StringComparison.OrdinalIgnoreCase)
                 || text.StartsWith(LeftPrefixEn, StringComparison.OrdinalIgnoreCase)
                 || text.Contains("joined the channel '", StringComparison.OrdinalIgnoreCase)
                 || text.Contains("left the channel '", StringComparison.OrdinalIgnoreCase))
                && (text.Contains(" the channel '", StringComparison.OrdinalIgnoreCase) || text.Contains("Kanal [", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Liest eine Schiffs-Kanalbenachrichtigung und extrahiert Schiff, Eigner und Akteur.
    /// </summary>
    public static ChannelNote? Read(DateTime at, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // 1. Lokaler Spieler betritt Schiff (Englisch)
        if (Tail(text, BoardedEn) is { } bodyEn)
        {
            var (ship, owner) = Berth(bodyEn);
            if (ship != null && owner != null)
                return new ChannelNote(at, ship, owner, null, ChannelMoment.YouBoarded);
        }

        // 2. Lokaler Spieler betritt Schiff (Deutsch)
        if (Tail(text, BoardedDe) is { } bodyDe)
        {
            var (ship, owner) = Berth(bodyDe);
            if (ship != null && owner != null)
                return new ChannelNote(at, ship, owner, null, ChannelMoment.YouBoarded);
        }

        // 3. Multi-Crew: Anderes Besatzungsmitglied betritt oder verlässt das Schiff
        var shapes = new (string Prefix, string Ending, ChannelMoment Moment)[]
        {
            (JoinedPrefixEn, " has joined the channel ", ChannelMoment.TheyBoarded),
            (LeftPrefixEn, " has left the channel ", ChannelMoment.TheyLeft),
            ("Mitglied beigetreten ", " ist dem Kanal ", ChannelMoment.TheyBoarded),
            ("Mitglied verlassen ", " hat den Kanal ", ChannelMoment.TheyLeft),
        };

        foreach (var (prefix, ending, moment) in shapes)
        {
            string? content = Tail(text, prefix);
            if (content == null && text.Contains(ending, StringComparison.OrdinalIgnoreCase))
            {
                // Falls Prefix fehlt, Text direkt verwenden
                content = text;
            }

            if (content == null) continue;

            int atIdx = content.IndexOf(ending, StringComparison.OrdinalIgnoreCase);
            if (atIdx <= 0) continue;

            var handle = content[..atIdx].Trim();
            if (handle.Length == 0 || handle.Contains(' '))
                continue;

            var berthPart = content[(atIdx + ending.Length)..];
            var (ship, owner) = Berth(berthPart);
            if (ship != null && owner != null)
            {
                return new ChannelNote(at, ship, owner, handle, moment);
            }
        }

        return null;
    }

    /// <summary>
    /// Extrahiert Schiffsname und Eigner aus Formaten wie:
    /// 'RSI Ursa Medivac : DeathStrokeo1' oder [ RSI Ursa Medivac : DeathStrokeo1 ]
    /// </summary>
    private static (string? Ship, string? Owner) Berth(string text)
    {
        string inside = text;

        // Anführungszeichen '...' auswerten
        int openQuote = text.IndexOf('\'');
        int closeQuote = openQuote >= 0 ? text.IndexOf('\'', openQuote + 1) : -1;
        if (openQuote >= 0 && closeQuote > openQuote)
        {
            inside = text[(openQuote + 1)..closeQuote];
        }
        else
        {
            // Eckige Klammern [...] auswerten (deutsche Lokalisierung)
            int openBracket = text.IndexOf('[');
            int closeBracket = openBracket >= 0 ? text.IndexOf(']', openBracket + 1) : -1;
            if (openBracket >= 0 && closeBracket > openBracket)
            {
                inside = text[(openBracket + 1)..closeBracket];
            }
        }

        // Am letzten ' : ' trennen (Schiffsname darf Doppelpunkte enthalten, Handle nicht)
        int split = inside.LastIndexOf(" : ", StringComparison.Ordinal);
        if (split <= 0) return (null, null);

        string shipRaw = inside[..split].Trim();
        string owner = inside[(split + 3)..].Trim().TrimEnd('.', ']', '\'');

        if (shipRaw.Length == 0 || owner.Length == 0 || owner.Contains(' '))
            return (null, null);

        string ship = Ships.Prettify(shipRaw);
        return (ship, owner);
    }

    private static string? Tail(string text, string prefix) =>
        text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? text[prefix.Length..]
            : null;
}
