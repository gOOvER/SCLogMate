using System;
using System.Collections.Generic;

namespace SCLogMate.Core;

/// <summary>
/// Verbindet physische Game.log-Zeilen zu vollständigen logischen Einträgen.
/// HUD-Notifications können innerhalb eines zitierten Texts umbrechen und
/// tragen in der Folgezeile trotzdem erneut denselben Zeitstempel.
/// </summary>
public static class LogEntryReader
{
    private const int MaxContinuations = 32;

    public static IEnumerable<string> ReadEntries(IEnumerable<string> lines)
    {
        string? pending = null;
        var continuations = 0;

        foreach (var line in lines)
        {
            if (pending is not null && continuations < MaxContinuations && HasUnterminatedQuote(pending))
            {
                pending = string.Concat(pending, " ", StripTimestamp(line).Trim());
                continuations++;
                continue;
            }

            if (pending is not null)
                yield return pending;

            pending = line;
            continuations = 0;
        }

        if (pending is not null)
            yield return pending;
    }

    public static bool HasUnterminatedQuote(string line)
    {
        var count = 0;
        foreach (var character in line)
        {
            if (character == '"')
                count++;
        }

        return (count & 1) == 1;
    }

    public static string StripTimestamp(string line)
    {
        if (line.Length == 0 || line[0] != '<')
            return line;

        var close = line.IndexOf('>');
        return close < 0 ? line : line[(close + 1)..];
    }
}