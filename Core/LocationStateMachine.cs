using System;
using System.Collections.Generic;

namespace SCLogMate.Core;

/// <summary>
/// Vertrauensstufe der Standorterkennung basierend auf der Stärke des Signals.
/// </summary>
public enum LocationConfidence
{
    /// <summary>Noch kein Signal empfangen oder im Hauptmenü.</summary>
    None,

    /// <summary>Indirekt abgeleitet (z. B. nach Spielerspawn oder Verlassen des Menüs).</summary>
    Low,

    /// <summary>Aus Quantum-Ankunft, Schutzzone oder ATC-Hangar abgeleitet.</summary>
    Medium,

    /// <summary>Direktes, unmissverständliches Signal (z. B. lokales Inventar geöffnet, Kiosk-Kauf).</summary>
    High
}

/// <summary>
/// Schnappschuss des aktuellen Spielerstandorts und eventueller aktiver Quantum-Reisen.
/// </summary>
public sealed record PlayerLocationState(
    ResolvedLocation? Current,
    ResolvedLocation? TravellingTo,
    LocationConfidence Confidence,
    bool InGame,
    string? GameRules,
    DateTime AsOf)
{
    public static readonly PlayerLocationState Unknown =
        new(null, null, LocationConfidence.None, false, null, DateTime.MinValue);

    public bool IsTravelling => TravellingTo is not null;
}

/// <summary>
/// Zeichnet einen protokollierten Standortwechsel auf (für Chronik & Reiseverlauf).
/// </summary>
public sealed record LocationChangeRecord(
    DateTime Timestamp,
    ResolvedLocation? From,
    ResolvedLocation To,
    LocationConfidence Confidence,
    bool ViaQuantum);

/// <summary>
/// Intelligente Zustandsmaschine zur Standort-Rekonstruktion aus schwachen und starken Logsignalen.
/// </summary>
public sealed class LocationStateMachine
{
    private readonly List<LocationChangeRecord> _history = new();

    /// <summary>Aktueller Schätzwert des Spielerstandorts.</summary>
    public PlayerLocationState State { get; private set; } = PlayerLocationState.Unknown;

    /// <summary>Vollständige Historie aller verifizierten Standortwechsel.</summary>
    public IReadOnlyList<LocationChangeRecord> History => _history;

    /// <summary>Wird ausgelöst, sobald sich der Spielerstandort nachweislich verändert.</summary>
    public event Action<LocationChangeRecord>? Changed;

    /// <summary>
    /// Direktes, hochgradig vertrauenswürdiges Signal: Lokales Inventar an einem Standort angefordert.
    /// </summary>
    public void ApplyInventoryRequest(string rawLoc, DateTime ts)
    {
        if (string.IsNullOrWhiteSpace(rawLoc) || rawLoc.Equals("INVALID_LOCATION_ID", StringComparison.OrdinalIgnoreCase))
            return;

        var loc = Locations.ResolveLocation(rawLoc);
        if (loc.DisplayName == "—") return;

        MoveTo(ts, loc, LocationConfidence.High, viaQuantum: State.IsTravelling);
    }

    /// <summary>
    /// Spieler hat einen Quantum-Zielpunkt im HUD angewählt.
    /// </summary>
    public void ApplyQuantumTarget(string targetLoc, DateTime ts)
    {
        if (string.IsNullOrWhiteSpace(targetLoc)) return;
        var dest = Locations.ResolveLocation(targetLoc);
        if (dest.DisplayName == "—") return;

        BeginTravel(ts, dest);
    }

    /// <summary>
    /// Eine Quantum-Route zu einem Ziel wurde erfolgreich berechnet.
    /// </summary>
    public void ApplyQuantumRoute(string? originLoc, string destLoc, DateTime ts)
    {
        if (string.IsNullOrWhiteSpace(destLoc)) return;
        var dest = Locations.ResolveLocation(destLoc);
        if (dest.DisplayName == "—") return;

        BeginTravel(ts, dest);
    }

    /// <summary>
    /// Quantum-Sprung beendet / Ankunft am Zielort signalisiert.
    /// </summary>
    public void ApplyQuantumArrival(DateTime ts)
    {
        if (State.TravellingTo != null)
        {
            MoveTo(ts, State.TravellingTo, LocationConfidence.Medium, viaQuantum: true);
        }
    }

    /// <summary>
    /// Spieler ist im Spiel gespawnt (Station, Bett, Hangar).
    /// </summary>
    public void ApplySpawn(string? rawLoc, DateTime ts)
    {
        ResolvedLocation? loc = null;
        if (!string.IsNullOrWhiteSpace(rawLoc))
        {
            var res = Locations.ResolveLocation(rawLoc);
            if (res.DisplayName != "—") loc = res;
        }

        State = State with
        {
            Current = loc ?? State.Current,
            TravellingTo = null,
            Confidence = (loc ?? State.Current) is null ? LocationConfidence.None : LocationConfidence.Low,
            InGame = true,
            AsOf = ts
        };
    }

    /// <summary>
    /// Hangar-Zuweisung oder ATC-Freigabe an einer Station.
    /// </summary>
    public void ApplyHangarAssignment(string? stationOrHangar, DateTime ts)
    {
        if (string.IsNullOrWhiteSpace(stationOrHangar)) return;
        var loc = Locations.ResolveLocation(stationOrHangar);
        if (loc.DisplayName != "—" && !loc.DisplayName.StartsWith("Hangar", StringComparison.OrdinalIgnoreCase))
        {
            MoveTo(ts, loc, LocationConfidence.Medium, viaQuantum: State.IsTravelling);
        }
    }

    /// <summary>
    /// Schutzzone betreten (liefert oft den Stationsnamen).
    /// </summary>
    public void ApplyArmistice(string? locText, DateTime ts)
    {
        if (string.IsNullOrWhiteSpace(locText)) return;
        var loc = Locations.ResolveLocation(locText);
        if (loc.DisplayName != "—")
        {
            MoveTo(ts, loc, LocationConfidence.Medium, viaQuantum: State.IsTravelling);
        }
    }

    /// <summary>
    /// Trennt Menü-Zeit (SC_Frontend) vom aktiven Universum (SC_Default).
    /// </summary>
    public void ApplyGameRules(string gameRules, DateTime ts)
    {
        bool inGame = !gameRules.Equals("SC_Frontend", StringComparison.OrdinalIgnoreCase);
        State = State with
        {
            GameRules = gameRules,
            InGame = inGame,
            AsOf = ts,
            Confidence = inGame ? State.Confidence : LocationConfidence.None
        };
    }

    private void BeginTravel(DateTime ts, ResolvedLocation destination)
    {
        if (State.TravellingTo?.RawCode == destination.RawCode)
            return;

        State = State with { TravellingTo = destination, AsOf = ts };
    }

    private void MoveTo(DateTime ts, ResolvedLocation destination, LocationConfidence confidence, bool viaQuantum)
    {
        bool unchanged = State.Current?.RawCode == destination.RawCode;

        if (unchanged && !State.IsTravelling)
        {
            State = State with { Confidence = confidence, AsOf = ts };
            return;
        }

        var change = new LocationChangeRecord(ts, State.Current, destination, confidence, viaQuantum);

        State = State with
        {
            Current = destination,
            TravellingTo = null,
            Confidence = confidence,
            InGame = true,
            AsOf = ts
        };

        if (unchanged)
            return;

        _history.Add(change);
        Changed?.Invoke(change);
    }

    /// <summary>
    /// Setzt alle Zustände für eine neue Sitzung zurück.
    /// </summary>
    public void Reset()
    {
        State = PlayerLocationState.Unknown;
        _history.Clear();
    }
}
