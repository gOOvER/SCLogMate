using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SCLogMate.Models;

namespace SCLogMate.Core;

/// <summary>
/// Stateful, line-by-line parser for the Star Citizen Game.log.
/// Geld-Überweisungen stehen über zwei Zeilen (Kopfzeile + Betrags-Zeile),
/// daher wird ein "pending" Zustand gehalten, bis die Betragszeile kommt.
/// Wiederholungen (UpdateNotificationItem) werden ignoriert, weil sie nicht
/// mit 'Added notification' beginnen.
/// </summary>
public partial class LogParser
{
    [GeneratedRegex(@"^<(?<ts>\d{4}-\d{2}-\d{2}T[\d:.]+Z)>")]
    private static partial Regex TsRegex();

    [GeneratedRegex(@"Added notification ""(?:(?:Überweisung erhalten von|Transfer received from|Payment received from):\s*(?<who>.+?)|(?<who>[A-Za-z0-9_\-]+)\s+(?:has sent you|hat dir gesendet):?)\s*$")]
    private static partial Regex RecvHdrRegex();

    [GeneratedRegex(@"Added notification ""(?:Sie haben\s+(?<who>.+?)\s+gesendet|You sent\s+(?<who>.+?)|You have sent\s+(?<who>.+?)|Transfer sent to\s+(?<who>.+?)):?")]
    private static partial Regex SentHdrRegex();

    [GeneratedRegex(@"Added notification ""(?:(?<amt>[\d.,]+)\s*aUEC\s*(?:erhalten|received)?|(?:Awarded|Received|Payment received|Belohnung|Missions-Belohnung):?\s*(?<amt>[\d.,]+)\s*aUEC)")]
    private static partial Regex RewardRegex();

    [GeneratedRegex(@"^<[^>]+>\s*(?<amt>[\d.,]+)\s*(?:aUEC|UEC)\s*$")]
    private static partial Regex AmtLineRegex();

    [GeneratedRegex(@"RequestLocationInventory.*Location\[(?<loc>[^\]]+)\]")]
    private static partial Regex LocRegex();

    [GeneratedRegex(@"Inventory\[(?<inv>[^\]]+)\].*Item Count:\[(?<cnt>\d+)\]")]
    private static partial Regex InvRegex();

    [GeneratedRegex(@"ClearDriver:.*token for '(?<ship>[^']+)'")]
    private static partial Regex VehRegex();

    // Schiffs-Bereitstellung / Spawnen (Game Build 12519617+ Ersatz für Pad-Bestätigung)
    [GeneratedRegex(@"(?:Vehicle (?:Retrieval Request|Spawn Request)|Requesting vehicle spawn|SpawnVehicleResponse).*?(?:vehicle|Vehicle)\s*['""]?(?<ship>[A-Za-z0-9_]+)['""]?")]
    private static partial Regex VehicleRetrievalRegex();

    // Echtes QT-Ereignis: abgeschlossener Sprung (1x pro Ankunft), inkl. Schiff.
    [GeneratedRegex(@"(?<ship>[A-Za-z][A-Za-z0-9_]+?)_\d+\[\d+\]\|CSCItemNavigation::OnQuantumDriveArrived")]
    private static partial Regex QtArriveRegex();

    [GeneratedRegex(@"Added notification ""You have joined channel '(?<ship>[^':]+?)\s*:\s*(?<user>[^']+)'")]
    private static partial Regex ShipChannelJoinRegex();

    [GeneratedRegex(@"(?<ship>[A-Za-z][A-Za-z0-9_]+?)_\d+\[\d+\]\|CSCItemNavigation::(?:CalculateRoute|OnPlayerRequestFuelToQuantumTarget|OnPlayerSelectedQuantumTarget)")]
    private static partial Regex ItemNavShipRegex();

    [GeneratedRegex(@"Projected Start Location is (?<origin>.+?) for route to destination (?<dest>\S+)")]
    private static partial Regex QuantumRouteRegex();

    [GeneratedRegex(@"Successfully calculated route to (?<dest>\S+)")]
    private static partial Regex QuantumRouteSuccessRegex();

    [GeneratedRegex(@"selected point (?<dest>\S+) as their destination")]
    private static partial Regex QuantumTargetRegex();

    // Kauf an einem Shop/Kiosk: Item, Preis, Shop, GUID.
    [GeneratedRegex(@"SShopBuyRequest.*?shopName\[(?<shop>[^\]]*)\].*?client_price\[(?<price>[\d.]+)\].*?itemClassGUID\[(?<guid>[^\]]*)\].*?itemName\[(?<item>[^\]]*)\].*?quantity\[(?<qty>\d+)\]")]
    private static partial Regex BuyRegex();

    // Item-Verkauf (gleiche Felder wie Kauf, aber SShopSellRequest).
    [GeneratedRegex(@"SShopSellRequest.*?shopName\[(?<shop>[^\]]*)\].*?client_price\[(?<price>[\d.]+)\].*?itemClassGUID\[(?<guid>[^\]]*)\].*?itemName\[(?<item>[^\]]*)\].*?quantity\[(?<qty>\d+)\]")]
    private static partial Regex SellRegex();

    // Fracht-/Waren-Verkauf (Commodity): Gesamtbetrag + resourceGUID + Menge (in SCU).
    [GeneratedRegex(@"SShopCommoditySellRequest.*?shopName\[(?<shop>[^\]]*)\].*?amount\[(?<amt>[\d.]+)\].*?resourceGUID\[(?<guid>[^\]]*)\].*?quantity\[(?<qty>\d+)\]")]
    private static partial Regex CommodityRegex();

    // Fracht-/Waren-KAUF (Cargo-Trading): price = Gesamtbetrag, quantity in cSCU (÷100 = SCU!).
    [GeneratedRegex(@"SShopCommodityBuyRequest.*?shopName\[(?<shop>[^\]]*)\].*?price\[(?<price>[\d.]+)\].*?resourceGUID\[(?<guid>[^\]]*)\].*?quantity\[(?<qty>[\d.]+)\s*cSCU\]")]
    private static partial Regex CommodityBuyRegex();

    // Shop Response (Kaufbestätigung)
    [GeneratedRegex(@"(?:Shop Flow Response|RmShopFlowResponse).*?result\[(?<result>[^\]]*)\]")]
    private static partial Regex ShopResponseRegex();

    // Objective Status & Fortschritt
    [GeneratedRegex(@"mission_id (?<mission>[0-9a-fA-F-]+) - objective_id (?<objective>\S+) - state (?<state>MISSION_OBJECTIVE_STATE_\w+)(?:.*?flags=(?<flags>[^\s\[]*))?")]
    private static partial Regex ObjectiveRegex();

    // Notification-Kopfzeile (einmal pro Ereignis): Text bis ':' , '"' oder Zeilenende.
    // (Manche Notifications sind mehrzeilig – z.B. Geld-Angebote – daher auch $.)
    [GeneratedRegex(@"Added notification ""(?<txt>[^"":]+?)(?::|""|$)")]
    private static partial Regex NotifRegex();

    // Getragene Ausrüstung.
    [GeneratedRegex(@"AttachmentReceived> Player\[(?<p>[^\]]+)\] Attachment\[(?<item>[A-Za-z][^,]+),")]
    private static partial Regex AttachRegex();

    // Auftrag/Contract – vollständiger Text (Name, Rang, Route), nicht am ':' abschneiden.
    [GeneratedRegex(@"Added notification ""(?<full>(?:Neuer Auftrag|Auftrag (?:angenommen|abgeschlossen|fehlgeschlagen|geteilt|zurückgezogen|abgebrochen|aufgegeben)|Contract (?:Accepted|Complete|Completed|Failed|Shared|Withdrawn|Abandoned|Cancelled)|New Contract Available|New Objective)[^""]*)")]
    private static partial Regex MissionLineRegex();

    [GeneratedRegex(@"MissionId:\s*\[(?<id>[0-9a-fA-F-]+)\]")]
    private static partial Regex NotificationMissionIdRegex();

    // Blaupause / Crafting Blueprint / Belohnung erhalten - unterstützt alle SC-Varianten & Missions-Drops
    private static readonly string[] BlueprintMarkers = {
        "Received Blueprint:",
        "Bauplan erhalten:",
        "Blueprint received:",
        "Blueprint Unlocked:",
        "Bauplan freigeschaltet:",
        "Schematic received:",
        "You've earned:",
        "You have earned:",
        "Du hast verdient:",
        "Du hast erhalten:",
        "Item erhalten:"
    };

    public static string? TryExtractBlueprint(string line)
    {
        int idx = -1;
        int markerLen = 0;
        foreach (var m in BlueprintMarkers)
        {
            int found = line.IndexOf(m, StringComparison.OrdinalIgnoreCase);
            if (found >= 0)
            {
                idx = found;
                markerLen = m.Length;
                break;
            }
        }
        if (idx < 0) return null;

        int start = idx + markerLen;
        string remaining = line[start..];

        // Schneide an Zeilenumbrüchen oder gängigen Star Citizen Zusatztexten ab
        int end = remaining.IndexOfAny(new[] { '\r', '\n' });
        if (end >= 0) remaining = remaining[..end];

        int tagIdx = remaining.IndexOf("\" [", StringComparison.Ordinal);
        if (tagIdx >= 0) remaining = remaining[..tagIdx];

        int quoteIdx = remaining.IndexOf('"');
        if (quoteIdx >= 0) remaining = remaining[..quoteIdx];

        int freightIdx = remaining.IndexOf("Access it at", StringComparison.OrdinalIgnoreCase);
        if (freightIdx >= 0) remaining = remaining[..freightIdx];

        int actionIdx = remaining.IndexOf("Action:", StringComparison.OrdinalIgnoreCase);
        if (actionIdx >= 0) remaining = remaining[..actionIdx];

        string seg = remaining.Trim().TrimEnd(':').Trim();
        if (seg.Length < 3) return null;

        return seg;
    }

    // Abgeschlossene Mission (Server-Event, ohne Betrag) – eindeutig je mission_id.
    [GeneratedRegex(@"<MissionEnded>.*mission_id (?<id>[0-9a-f-]+) - mission_state MISSION_STATE_COMPLETED")]
    private static partial Regex MissionDoneRegex();

    // Ausrüstung/Item defekt ("Dein X ist unbrauchbar" / "Your X is broken").
    [GeneratedRegex(@"Deaktivierung eingeleitet: (?:Dein|Your) (?<item>[^""]+?) ist? (?:unbrauchbar|broken|unusable)")]
    private static partial Regex GearBrokeRegex();

    // Kill-Feed (Standard-SC-Format) – greift bei Combat.
    [GeneratedRegex(@"CActor::Kill: '(?<victim>[^']*)' \[\d+\] in zone '[^']*' killed by '(?<killer>[^']*)' \[\d+\] using '(?<weapon>[^']*)'")]
    private static partial Regex KillLineRegex();

    // Geld-Angebot (vor Annahme).
    [GeneratedRegex(@"(?<who>[A-Za-z0-9_\- ]+?) (?:möchte dir|wants to send you|is offering you) (?<amt>[\d.,]+) UEC")]
    private static partial Regex OfferRegex();

    // Schiffsverlust durch Kollision.
    [GeneratedRegex(@"Fatal Collision occured for vehicle (?<ship>[A-Za-z][A-Za-z0-9_]+?)_\d+")]
    private static partial Regex CollisionRegex();

    // Login & Spieler-Identifikation
    [GeneratedRegex(@"\[Legacy login response\].*?handle:\s*(?<handle>[A-Za-z0-9_\-]+)")]
    private static partial Regex LoginHandleRegex();

    [GeneratedRegex(@"\[AccountLoginCharacterStatus_Character\].*?name:\s*(?<name>[A-Za-z0-9_\-]+)")]
    private static partial Regex CharacterStatusRegex();

    [GeneratedRegex(@"\[CSessionManager::OnClientSpawned\]\s*Spawned!")]
    private static partial Regex ClientSpawnedRegex();

    // Fahrzeug-Kontrolle & Cockpit-Sitzwechsel
    [GeneratedRegex(@"<Vehicle Control Flow>.*?(?<method>EnterDriver|ClearDriver).*?vehicleId=(?<veh>[^ ]+)")]
    private static partial Regex VehicleControlFlowRegex();

    // ASOP Terminal Fahrzeugbereitstellung
    [GeneratedRegex(@"\[CEntityComponentShipListProvider::SetVehicleSpawn(?:ing|ed)Informations\].*?entityName\s*=\s*(?<ship>[^,\]]+)")]
    private static partial Regex AsopShipSpawnRegex();

    // Comm-Kanal eines Schiffs: [ <Schiff> : <Besitzer> ]
    [GeneratedRegex(@"(?:Kanal|Channel) \[ (?<ship>.+?) : (?<owner>[^\]]+?) \]")]
    private static partial Regex ChannelRegex();

    private readonly HashSet<string> _ownNames =
        new(StringComparer.OrdinalIgnoreCase) { "MiwiDot", "miwi", "miwitv" };
    public string? LocalHandle { get; private set; }

    // Loot: Item ins Inventar gestaut. Nur „Runtime-spawned" = von der Welt gespawnt
    // (echter Loot aus Kisten/Gegnern), nicht Kauf/Umräumen.
    [GeneratedRegex(@"<OnInventoryStoreItem> Entity\[[^ ]+ - Class\((?<cls>[^)]+)\) - Context\((?<ctx>[^)]*)\)")]
    private static partial Regex LootStoreRegex();

    // Loot, das direkt ausgerüstet/gegriffen wird (Armor-Swap am Körper, Ghost-Hollow-Style):
    // "Equip looting entity[<class>_<entityId>]" — NICHT "equip from Inventory" (eigenes Zeug).
    [GeneratedRegex(@"<EquipItem> Equip looting entity\[(?<cls>[A-Za-z0-9_]+)_\d{6,}\]")]
    private static partial Regex EquipLootRegex();

    // Bußgeld gezahlt (mit Betrag) – echtes aUEC raus, fließt in den Saldo.
    [GeneratedRegex(@"Added notification ""(?:Strafe gezahlt|Fine paid|Penalty paid|Fined):?\s*(?<amt>[\d.,]+)")]
    private static partial Regex FineLineRegex();

    // Begangene Straftat (Crimestat-Verlauf).
    [GeneratedRegex(@"Added notification ""(?:Begangene Straftat|Crime committed|Infraction committed|Homicide committed|Felony committed):\s*(?<crime>[^""]+)")]
    private static partial Regex CrimeLineRegex();

    // Veredelungs-/Refinery-Auftrag abgeschlossen.
    [GeneratedRegex(@"Added notification ""(?:Ein Auftrag zur Veredelung wurde abgeschlossen|A refining job has completed|Refining order completed|Refinery job complete)(?<txt>[^""]*)")]
    private static partial Regex RefineryLineRegex();

    // Verletzung/Lähmung festgestellt (Schweregrad + Körperteil + Behandlungsstufe).
    [GeneratedRegex(@"Added notification ""(?<txt>(?:Leichte|Mäßige|Schwere|Kritische|Teilweise|Minor|Moderate|Severe|Critical|Partial) (?:Verletzung|Lähmung|Injury|Paralysis)[^""]*)")]
    private static partial Regex InjuryLineRegex();

    // Party-Mitglieder rein/raus (Name in der Folgezeile oder Notification-Header)
    [GeneratedRegex(@"(?<who>[A-Za-z0-9_\-]+) (?:ist Party beigetreten|has joined the party|joined the party|joined party)")]
    private static partial Regex PartyJoinRegex();

    [GeneratedRegex(@"Added notification ""(?:New Member Joined|Member Joined):\s*(?<who>[^""]+)")]
    private static partial Regex PartyMemberJoinNotifRegex();

    [GeneratedRegex(@"(?<who>[A-Za-z0-9_\-]+) (?:ha(?:t|st)(?: die)? Party verlassen|has left the party|left the party|left party)")]
    private static partial Regex PartyLeaveRegex();

    [GeneratedRegex(@"Added notification ""(?:Member Left|Member departed):\s*(?<who>[^""]+)")]
    private static partial Regex PartyMemberLeaveNotifRegex();

    // Angenommene/aktive Mission mit Auftraggeber + Contract (feuert je Mission viele Male
    // als Objektiv-Marker → wir nehmen je missionId nur EINEN Eintrag).
    [GeneratedRegex(@"missionId \[(?<id>[0-9a-f-]+)\], generator name \[(?<gen>[A-Za-z0-9_]+)\], contract \[(?<con>[A-Za-z0-9_]+)\]")]
    private static partial Regex MissionMarkerRegex();

    // Comms-Benachrichtigung für Missionsannahme mit Auftraggeber & Fraktion (SC 4.x)
    [GeneratedRegex(@"<CommsNotifications>\s+SendCommsNotification\s+\+Missions\.Organization\.(?:MissionGiver\.)?(?<giver>[A-Za-z0-9_]+)(?:,AI\.Faction\.(?<faction>[A-Za-z0-9_]+))?.*?Mission:\s+\[(?<id>[0-9a-fA-F-]+)\]")]
    private static partial Regex CommsNotificationRegex();

    // Standorte, Zonen & Schutzzonen
    [GeneratedRegex(@"Added notification ""(?<text>[^""]+)""")]
    private static partial Regex GenericNotificationRegex();

    [GeneratedRegex(@"Added notification ""(?:Schutzzone|Armistice Zone|Armistice|Rechtsgebiet|Jurisdiction|Kontrollierten Raum|Monitored Space):\s*(?<loc>[^""]+)")]
    private static partial Regex ArmisticeNotifRegex();

    [GeneratedRegex(@"requested inventory for Location\[(?<loc>[^\]]+)\]")]
    private static partial Regex RequestInventoryLocRegex();

    [GeneratedRegex(@"(?:SpawnLocation|LocalSpawnLocation|SpawnPoint|zone|Zone)\[(?<loc>[^\]]+)\]")]
    private static partial Regex ZoneRegex();

    [GeneratedRegex(@"Player spawned in zone '(?<loc>[^']+)'")]
    private static partial Regex PlayerSpawnZoneRegex();

    // SC 4.x Fracht- & Schiffs-Aufzüge (Freight & Ship Elevators)
    [GeneratedRegex(@"CSCLoadingPlatformManager::OnLoadingPlatformStateChanged.*?\[LoadingPlatformManager_(?<type>FreightElevator|ShipElevator)[^\]]*\].*?Platform state changed to (?<state>\w+)")]
    private static partial Regex ElevatorStateRegex();

    // ATC Landefreigabe & Hangar-Zuweisung
    [GeneratedRegex(@"(?:Landing Request Granted|Hangar Assignment|Assigned to Hangar|Landing gear down).*?(?<hangar>Hangar\s*(?:[A-Za-z0-9_]+|\d+)|Pad\s*\d+)")]
    private static partial Regex AtcHangarRegex();

    // Schiffszerstörung / Self-Destruct
    [GeneratedRegex(@"(?:VehicleDestroyed|Vehicle destroyed|Vehicle Exploded|Self-Destruct initiated).*?(?:vehicle|Vehicle|ship)?\s*['""]?(?<ship>[A-Za-z0-9_]+)?['""]?")]
    private static partial Regex VehicleDestroyedRegex();

    // Versicherungs-Claim
    [GeneratedRegex(@"(?:Insurance Claim Request|Vehicle claim requested|Expedited delivery for vehicle).*?['""]?(?<ship>[A-Za-z0-9_]+)['""]?")]
    private static partial Regex InsuranceClaimRegex();

    // Source-generated Hilfs-Regexes
    [GeneratedRegex(@"<EM4>.*?</EM4>")]
    private static partial Regex BpBlockRegex();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s*Rang:\s*~mission\([^)]*\)\s*\|?")]
    private static partial Regex MissionRankRegex();

    [GeneratedRegex(@"\s*Direktroute:\s*~mission\([^)]*\)\s*-?")]
    private static partial Regex MissionRouteRegex();

    [GeneratedRegex(@"~mission\([^)]*\)")]
    private static partial Regex MissionTildeRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex(@"_(\d+|[a-z])(?=_|$)")]
    private static partial Regex LootVersionRegex();

    [GeneratedRegex(@"_\d{4,}$")]
    private static partial Regex LoadoutTrailingIdRegex();

    [GeneratedRegex(@"^SCShop_")]
    private static partial Regex ScShopPrefixRegex();

    [GeneratedRegex(@"- (?<g>(NVIDIA|AMD|Intel)[^(]+?) \(vendor")]
    private static partial Regex GpuRegex();

    [GeneratedRegex(@"(?<mb>\d+)MB physical memory installed")]
    private static partial Regex RamRegex();

    [GeneratedRegex(@"name (?<n>\S+) - state STATE_CURRENT")]
    private static partial Regex CharRegex();

    [GeneratedRegex(@"Join PU>.*shard\[(?<s>[^\]]+)\]")]
    private static partial Regex ShardRegex();

    string? _pendWho;
    int _pendDir;          // +1 = rein, -1 = raus
    DateTime _pendTime;
    int _pendingLines;

    string? _lastLoot;                      // gegen Loot-Doppelzeilen
    string? _lastLoc;                       // für Quantum-Kontext
    string _currentSystem = "Stanton";      // aktuell erkanntes Sternensystem (Stanton / Pyro / Nyx)
    string? _lastShip;                      // aktuell erkanntes Schiff
    string? _pendingQtDestination;          // aus Route-Kalkulation (Quantum Route)
    DateTime _lastQt = DateTime.MinValue;   // Drosselung der QT-Marker
    string? _lastNotif;                     // gegen Notification-Spam
    string? _lastParty;                     // gegen Party-Spam (Wiederholungen)
    readonly HashSet<string> _loadoutSeen = new();
    readonly HashSet<string> _channelSeen = new();
    readonly HashSet<string> _gearSeen = new();
    readonly HashSet<string> _missionsDone = new();
    readonly HashSet<string> _missionsTaken = new();
    readonly Dictionary<string, DateTime> _seenBlueprints = new(StringComparer.OrdinalIgnoreCase);
    bool _metaComplete;

    /// <summary>Session-Metadaten (Build, Hardware, Charakter, Shard, …).</summary>
    public Dictionary<string, string> Meta { get; } = new();

    // Tracking für Contracts, Spending, Ledger, Cargo und Places
    public sealed record PendingPurchase(DateTime Timestamp, string Shop, string Item, string Guid, decimal Price, int Qty);
    private PendingPurchase? _pendingPurchase;

    public List<ConfirmedPurchaseRecord> ConfirmedPurchases { get; } = new();
    public List<CargoTradeRecord> CargoTrades { get; } = new();
    public List<LedgerRecord> LedgerRecords { get; } = new();
    public List<(DateTime Time, string RawId, string Name, string? System, string? Body, string Kind)> LocationVisits { get; } = new();
    public List<(DateTime Time, string Destination)> QuantumDestinations { get; } = new();

    private readonly Dictionary<string, ContractRecord> _contracts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> _contractObjectives = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string Giver, string Faction)> _missionComms = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<ContractRecord> ContractsList => _contracts.Values;

    public string PlaceAt(DateTime at)
    {
        for (int i = LocationVisits.Count - 1; i >= 0; i--)
        {
            if (LocationVisits[i].Time <= at)
                return LocationVisits[i].Name;
        }
        for (int i = QuantumDestinations.Count - 1; i >= 0; i--)
        {
            if (QuantumDestinations[i].Time <= at)
                return QuantumDestinations[i].Destination;
        }
        return _lastLoc ?? "—";
    }

    public static string CategorizeItem(string? guid, string item)
    {
        var s = item.ToLowerInvariant();
        if (s.Contains("rifle") || s.Contains("pistol") || s.Contains("shotgun") || s.Contains("sniper") || s.Contains("smg") || s.Contains("weapon") || s.Contains("gun") || s.Contains("cannon") || s.Contains("repeater") || s.Contains("missile") || s.Contains("torpedo") || s.Contains("laser") || s.Contains("ballistic"))
            return "Waffen";
        if (s.Contains("helmet") || s.Contains("torso") || s.Contains("arms") || s.Contains("legs") || s.Contains("armor") || s.Contains("undersuit") || s.Contains("suit") || s.Contains("core") || s.Contains("backpack"))
            return "Rüstung";
        if (s.Contains("shield") || s.Contains("cooler") || s.Contains("power") || s.Contains("quantum") || s.Contains("generator") || s.Contains("drive") || s.Contains("thruster") || s.Contains("radar") || s.Contains("avionics"))
            return "Schiffsteile";
        if (s.Contains("medpen") || s.Contains("oxypen") || s.Contains("drink") || s.Contains("food") || s.Contains("can") || s.Contains("bottle") || s.Contains("snack") || s.Contains("seren") || s.Contains("medical"))
            return "Verbrauchsgüter";
        if (s.Contains("container") || s.Contains("box") || s.Contains("scu") || s.Contains("crate") || s.Contains("carryable"))
            return "Behälter";
        return "Ausrüstung & Sonstiges";
    }

    /// <summary>Unbekannte Notification-Typen (Diagnose: was decken wir noch nicht ab?).</summary>
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> Unknown = new();

    /// <summary>Anzahl verworfener Transferköpfe ohne folgende Betragszeile.</summary>
    public int ExpiredPendingTransfers { get; private set; }

    private DateTime? _lastSeenTime;

    public LogEntry? Feed(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;

        if (line.Length >= 25 && line[0] == '<')
        {
            var mTs = TsRegex().Match(line);
            if (mTs.Success && DateTime.TryParse(mTs.Groups["ts"].Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtParsed))
            {
                _lastSeenTime = dtParsed;
            }
        }

        CaptureMeta(line);

        // Kiosk-Kaufbestätigung (Shop Flow Response)
        if (_pendingPurchase != null && (line.Contains("Shop Flow Response", StringComparison.OrdinalIgnoreCase) || line.Contains("RmShopFlowResponse", StringComparison.OrdinalIgnoreCase)))
        {
            var sres = ShopResponseRegex().Match(line);
            if (sres.Success && sres.Groups["result"].Value.Equals("Success", StringComparison.OrdinalIgnoreCase))
            {
                var p = _pendingPurchase;
                _pendingPurchase = null;
                var where = PlaceAt(p.Timestamp);
                var cat = CategorizeItem(p.Guid, p.Item);
                ConfirmedPurchases.Add(new ConfirmedPurchaseRecord
                {
                    Timestamp = p.Timestamp,
                    ItemName = p.Item,
                    Category = cat,
                    Shop = p.Shop,
                    Location = where,
                    TotalPrice = p.Price,
                    Quantity = p.Qty,
                    Confirmed = true
                });

                LedgerRecords.Add(new LedgerRecord
                {
                    Timestamp = p.Timestamp,
                    Kind = "Item gekauft",
                    What = p.Qty > 1 ? $"{p.Item} ×{p.Qty}" : p.Item,
                    Where = where,
                    Shop = p.Shop,
                    Amount = -p.Price,
                    Quantity = p.Qty,
                    Confirmed = true
                });
            }
        }

        // Mission Objective Status & Auftrags-Fortschritt (ObjectiveUpserted)
        if (line.Contains("ObjectiveUpserted", StringComparison.OrdinalIgnoreCase))
        {
            var om = ObjectiveRegex().Match(line);
            if (om.Success)
            {
                var mId = om.Groups["mission"].Value;
                var objId = om.Groups["objective"].Value;
                var stateStr = om.Groups["state"].Value;
                bool isShowInLog = om.Groups["flags"].Success && om.Groups["flags"].Value.Contains("ShowInLog", StringComparison.OrdinalIgnoreCase);

                if (!_contractObjectives.TryGetValue(mId, out var steps))
                {
                    steps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _contractObjectives[mId] = steps;
                }
                if (isShowInLog || !steps.ContainsKey(objId))
                {
                    steps[objId] = stateStr;
                }

                int totalSteps = steps.Count;
                int doneSteps = steps.Values.Count(st => st.Equals("MISSION_OBJECTIVE_STATE_COMPLETED", StringComparison.OrdinalIgnoreCase));

                var dt = ParseTs(line);
                ContractOutcome outcome = stateStr switch
                {
                    "MISSION_OBJECTIVE_STATE_COMPLETED" => ContractOutcome.Completed,
                    "MISSION_OBJECTIVE_STATE_WITHDRAWN" or "MISSION_OBJECTIVE_STATE_FAILED" => ContractOutcome.Abandoned,
                    _ => ContractOutcome.InProgress
                };

                if (_contracts.TryGetValue(mId, out var existing))
                {
                    var finalOutcome = existing.Outcome == ContractOutcome.Completed
                        ? ContractOutcome.Completed
                        : outcome;

                    _contracts[mId] = existing with
                    {
                        StepsTotal = Math.Max(existing.StepsTotal, totalSteps),
                        StepsDone = Math.Max(existing.StepsDone, doneSteps),
                        Outcome = finalOutcome,
                        CompletedAt = finalOutcome == ContractOutcome.Completed || finalOutcome == ContractOutcome.Abandoned
                            ? (existing.CompletedAt ?? dt)
                            : null
                    };
                }
            }
        }

        // Eine offene Überweisung wird durch die nächste Betragszeile aufgelöst.
        if (_pendWho != null)
        {
            var a = AmtLineRegex().Match(line);
            if (a.Success)
            {
                long amt = ParseAmt(a.Groups["amt"].Value);
                string who = _pendWho;
                int dir = _pendDir;
                _pendWho = null;
                _pendingLines = 0;
                var ts = _pendTime;
                var where = PlaceAt(ts);

                LedgerRecords.Add(new LedgerRecord
                {
                    Timestamp = ts,
                    Kind = dir > 0 ? "Überweisung empfangen" : "Überweisung gesendet",
                    What = dir > 0 ? $"von {who}" : $"an {who}",
                    Where = where,
                    Shop = "mobiGlas",
                    Amount = dir * amt,
                    Quantity = 1,
                    Confirmed = true
                });

                return new LogEntry
                {
                    Time = _pendTime,
                    Kind = dir > 0 ? EventKind.TransferIn : EventKind.TransferOut,
                    Detail = dir > 0 ? $"von {who}" : $"an {who}",
                    Amount = dir * amt
                };
            }
            if (++_pendingLines >= 32)
            {
                _pendWho = null;
                _pendingLines = 0;
                ExpiredPendingTransfers++;
            }
        }

        var r = RecvHdrRegex().Match(line);
        if (r.Success) { _pendWho = Clean(r.Groups["who"].Value); _pendDir = +1; _pendTime = ParseTs(line); _pendingLines = 0; return null; }

        var s = SentHdrRegex().Match(line);
        if (s.Success) { _pendWho = Clean(s.Groups["who"].Value); _pendDir = -1; _pendTime = ParseTs(line); _pendingLines = 0; return null; }

        var rw = RewardRegex().Match(line);
        if (rw.Success)
        {
            var ts = ParseTs(line);
            long amt = ParseAmt(rw.Groups["amt"].Value);
            LedgerRecords.Add(new LedgerRecord
            {
                Timestamp = ts,
                Kind = "Belohnung",
                What = "Missions-Belohnung",
                Where = PlaceAt(ts),
                Shop = "mobiGlas",
                Amount = amt,
                Quantity = 1,
                Confirmed = true
            });
            return new LogEntry { Time = ts, Kind = EventKind.MissionReward, Detail = "Missions-Belohnung", Amount = amt };
        }

        // Quantum Route-Berechnungen (Destination merken für QT-Ankunft & System-Erkennung)
        var qr = QuantumRouteRegex().Match(line);
        if (qr.Success)
        {
            _pendingQtDestination = qr.Groups["dest"].Value;
            var origin = qr.Groups["origin"].Value;
            var resOrigin = Locations.ResolveLocation(origin);
            if (resOrigin.SystemName is "Stanton" or "Pyro" or "Nyx")
            {
                _currentSystem = resOrigin.SystemName;
                Locations.ActiveSystem = resOrigin.SystemName;
            }
        }
        else
        {
            var qrs = QuantumRouteSuccessRegex().Match(line);
            if (qrs.Success)
            {
                _pendingQtDestination = qrs.Groups["dest"].Value;
            }
            else
            {
                var qtTarget = QuantumTargetRegex().Match(line);
                if (qtTarget.Success)
                {
                    _pendingQtDestination = qtTarget.Groups["dest"].Value;
                }
            }
        }

        if (!string.IsNullOrEmpty(_pendingQtDestination))
        {
            var resDest = Locations.ResolveLocation(_pendingQtDestination);
            if (resDest.SystemName is "Stanton" or "Pyro" or "Nyx")
            {
                _currentSystem = resDest.SystemName;
                Locations.ActiveSystem = resDest.SystemName;
            }
        }

        var by = BuyRegex().Match(line);
        if (by.Success)
        {
            long price = (long)ParseDouble(by.Groups["price"].Value);   // client_price = GESAMTpreis (alle Stück)
            int qty = int.TryParse(by.Groups["qty"].Value, out var q) ? q : 1;
            var shop = CleanShop(by.Groups["shop"].Value);
            var shopLoc = ExtractLocationFromShop(shop);
            if (shopLoc != null) _lastLoc = shopLoc;
            var item = ItemNames.CleanFallback(by.Groups["item"].Value);
            var suffix = qty > 1 ? $"×{qty} · {shop}" : $"· {shop}";
            var ts = ParseTs(line);
            _pendingPurchase = new PendingPurchase(ts, shop, item, by.Groups["guid"].Value, price, qty);

            return new LogEntry
            {
                Time = ts,
                Kind = EventKind.Purchase,
                Amount = -price,                                        // NICHT ×qty – Preis ist schon der Gesamtbetrag
                ItemRef = by.Groups["guid"].Value,
                Suffix = suffix,
                Detail = $"{item}  {suffix}"
            };
        }

        var se = SellRegex().Match(line);
        if (se.Success)
        {
            long price = (long)ParseDouble(se.Groups["price"].Value);   // client_price = GESAMTpreis (alle Stück)
            int qty = int.TryParse(se.Groups["qty"].Value, out var q) ? q : 1;
            var shop = CleanShop(se.Groups["shop"].Value);
            var shopLoc = ExtractLocationFromShop(shop);
            if (shopLoc != null) _lastLoc = shopLoc;
            var item = ItemNames.CleanFallback(se.Groups["item"].Value);
            var suffix = qty > 1 ? $"×{qty} · {shop}" : $"· {shop}";
            return new LogEntry
            {
                Time = ParseTs(line),
                Kind = EventKind.Sale,
                Amount = price,                                        // NICHT ×qty – Preis ist schon der Gesamtbetrag
                ItemRef = se.Groups["guid"].Value,
                Suffix = suffix,
                Detail = $"{item}  {suffix}"
            };
        }

        var co = CommodityRegex().Match(line);
        if (co.Success)
        {
            long amt = (long)ParseDouble(co.Groups["amt"].Value);
            int qty = int.TryParse(co.Groups["qty"].Value, out var q) ? q : 0;
            var shop = CleanShop(co.Groups["shop"].Value);
            var shopLoc = ExtractLocationFromShop(shop);
            if (shopLoc != null) _lastLoc = shopLoc;
            var ware = Commodities.Resolve(co.Groups["guid"].Value);
            var ts = ParseTs(line);
            var where = PlaceAt(ts);

            CargoTrades.Add(new CargoTradeRecord
            {
                Timestamp = ts,
                IsSell = true,
                Commodity = ware,
                ResourceGuid = co.Groups["guid"].Value,
                QuantityScu = qty,
                TotalAuec = amt,
                Shop = shop,
                Where = where
            });

            LedgerRecords.Add(new LedgerRecord
            {
                Timestamp = ts,
                Kind = "Frachtverkauf",
                What = $"{ware} · {qty:N0} SCU",
                Where = where,
                Shop = shop,
                Amount = amt,
                Quantity = qty,
                Confirmed = true
            });

            return new LogEntry
            {
                Time = ts,
                Kind = EventKind.Trade,
                Amount = amt,
                Detail = $"{ware} ×{qty} SCU  · {shop}"
            };
        }

        // Fracht-KAUF: price = Gesamtbetrag (Geld raus), Menge in cSCU → ÷100 = SCU.
        var cb = CommodityBuyRegex().Match(line);
        if (cb.Success)
        {
            long price = (long)ParseDouble(cb.Groups["price"].Value);
            int scu = (int)Math.Round(ParseDouble(cb.Groups["qty"].Value) / 100.0);
            var shop = CleanShop(cb.Groups["shop"].Value);
            var shopLoc = ExtractLocationFromShop(shop);
            if (shopLoc != null) _lastLoc = shopLoc;
            var ware = Commodities.Resolve(cb.Groups["guid"].Value);
            var ts = ParseTs(line);
            var where = PlaceAt(ts);

            CargoTrades.Add(new CargoTradeRecord
            {
                Timestamp = ts,
                IsSell = false,
                Commodity = ware,
                ResourceGuid = cb.Groups["guid"].Value,
                QuantityScu = scu,
                TotalAuec = price,
                Shop = shop,
                Where = where
            });

            LedgerRecords.Add(new LedgerRecord
            {
                Timestamp = ts,
                Kind = "Frachtkauf",
                What = $"{ware} · {scu:N0} SCU",
                Where = where,
                Shop = shop,
                Amount = -price,
                Quantity = scu,
                Confirmed = true
            });

            return new LogEntry
            {
                Time = ts,
                Kind = EventKind.Trade,
                Amount = -price,
                Detail = $"{ware} ×{scu} SCU  · {shop} (Kauf)"
            };
        }

        // Spieler-Login & Handle Erkennung
        var logH = LoginHandleRegex().Match(line);
        if (logH.Success)
        {
            var h = logH.Groups["handle"].Value.Trim();
            if (!string.IsNullOrEmpty(h))
            {
                LocalHandle = h;
                _ownNames.Add(h);
            }
        }

        var charH = CharacterStatusRegex().Match(line);
        if (charH.Success)
        {
            var n = charH.Groups["name"].Value.Trim();
            if (!string.IsNullOrEmpty(n))
            {
                _ownNames.Add(n);
            }
        }

        // Spawn ins Spiel
        if (ClientSpawnedRegex().IsMatch(line))
        {
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.SessionChange, Detail = "Im Spiel gespawnt (Station / Hangar)" };
        }

        // ASOP Terminal Fahrzeugbereitstellung
        var asop = AsopShipSpawnRegex().Match(line);
        if (asop.Success)
        {
            var rawShip = asop.Groups["ship"].Value.Trim();
            var ship = Ships.Prettify(rawShip);
            _lastShip = ship;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = ship, Ship = ship };
        }

        // Fahrzeug-Kontrolle / Cockpit-Sitzwechsel
        var vcf = VehicleControlFlowRegex().Match(line);
        if (vcf.Success)
        {
            var method = vcf.Groups["method"].Value;
            var vehRaw = vcf.Groups["veh"].Value;
            var ship = Ships.Prettify(vehRaw);
            if (method.Contains("Enter", StringComparison.OrdinalIgnoreCase))
            {
                _lastShip = ship;
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = $"{ship} (Pilotensitz eingenommen)", Ship = ship };
            }
        }

        var gn = GenericNotificationRegex().Match(line);
        if (gn.Success)
        {
            var text = gn.Groups["text"].Value.Trim();
            
            // 1. Armistice / Schutzzone betreten
            if (text.StartsWith("Entering Armistice Zone", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("Betreten einer Waffenverbotszone", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("Schutzzone betreten", StringComparison.OrdinalIgnoreCase))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Jurisdiction, Detail = "🟢 Schutzzone aktiv (Waffen blockiert)" };
            }

            // 2. Armistice / Schutzzone verlassen
            if (text.StartsWith("Leaving Armistice Zone", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("Verlassen einer Waffenverbotszone", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("Schutzzone verlassen", StringComparison.OrdinalIgnoreCase))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Jurisdiction, Detail = "🔴 Schutzzone verlassen (Waffen scharf)" };
            }

            // 3. Jurisdiktion / Rechtssystem
            if (text.Contains("People's Alliance", StringComparison.OrdinalIgnoreCase))
            {
                _currentSystem = "Nyx";
                Locations.ActiveSystem = "Nyx";
                if (_lastLoc == null || _lastLoc == "—") _lastLoc = "Delamar";
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Jurisdiction, Detail = "🏛 Rechtsgebiet: People's Alliance (Nyx)" };
            }
            if (text.Contains("UEE Jurisdiction", StringComparison.OrdinalIgnoreCase) || text.Contains("Rechtsgebiet der UEE", StringComparison.OrdinalIgnoreCase))
            {
                _currentSystem = "Stanton";
                Locations.ActiveSystem = "Stanton";
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Jurisdiction, Detail = "🏛 Rechtsgebiet: UEE (Stanton)" };
            }
            if (text.Contains("Ungoverned", StringComparison.OrdinalIgnoreCase) || text.Contains("Ungesetzlich", StringComparison.OrdinalIgnoreCase))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Jurisdiction, Detail = $"🏴 Ungesetzlicher Sektor ({_currentSystem})" };
            }
            if (text.Contains("Hangar Request Completed", StringComparison.OrdinalIgnoreCase) || text.Contains("Hangar-Anforderung abgeschlossen", StringComparison.OrdinalIgnoreCase))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Hangar, Detail = "Hangar-Zuweisung erhalten" };
            }
        }

        var reqInv = RequestInventoryLocRegex().Match(line);
        if (reqInv.Success)
        {
            var raw = reqInv.Groups["loc"].Value;
            if (!raw.Equals("INVALID_LOCATION_ID", StringComparison.OrdinalIgnoreCase) && !line.Contains("doesn't have inventory", StringComparison.OrdinalIgnoreCase))
            {
                var locRes = Locations.ResolveLocation(raw);
                if (locRes.DisplayName != "—" && !locRes.DisplayName.StartsWith("Im Transit"))
                {
                    var ts = ParseTs(line);
                    LocationVisits.Add((ts, locRes.RawCode, locRes.DisplayName, locRes.SystemName, locRes.ParentBody, locRes.Type.ToString()));
                    if (locRes.DisplayName != _lastLoc)
                    {
                        _lastLoc = locRes.DisplayName;
                        return new LogEntry { Time = ts, Kind = EventKind.Location, Detail = _lastLoc };
                    }
                }
            }
        }

        var lo = LocRegex().Match(line);
        if (lo.Success)
        {
            var raw = lo.Groups["loc"].Value;
            if (!raw.Equals("INVALID_LOCATION_ID", StringComparison.OrdinalIgnoreCase) && !line.Contains("doesn't have inventory", StringComparison.OrdinalIgnoreCase))
            {
                var locRes = Locations.ResolveLocation(raw);
                if (locRes.DisplayName != "—" && !locRes.DisplayName.StartsWith("Im Transit"))
                {
                    var ts = ParseTs(line);
                    LocationVisits.Add((ts, locRes.RawCode, locRes.DisplayName, locRes.SystemName, locRes.ParentBody, locRes.Type.ToString()));
                    if (locRes.DisplayName != _lastLoc)
                    {
                        _lastLoc = locRes.DisplayName;
                        return new LogEntry { Time = ts, Kind = EventKind.Location, Detail = _lastLoc };
                    }
                }
            }
        }

        var an = ArmisticeNotifRegex().Match(line);
        if (an.Success)
        {
            var locRes = Locations.ResolveLocation(an.Groups["loc"].Value);
            if (locRes.DisplayName != "—" && !locRes.DisplayName.StartsWith("Im Transit"))
            {
                var ts = ParseTs(line);
                LocationVisits.Add((ts, locRes.RawCode, locRes.DisplayName, locRes.SystemName, locRes.ParentBody, locRes.Type.ToString()));
                if (locRes.DisplayName != _lastLoc)
                {
                    _lastLoc = locRes.DisplayName;
                    return new LogEntry { Time = ts, Kind = EventKind.Location, Detail = _lastLoc };
                }
            }
        }

        var spz = PlayerSpawnZoneRegex().Match(line);
        if (spz.Success)
        {
            var locRes = Locations.ResolveLocation(spz.Groups["loc"].Value);
            if (locRes.DisplayName != "—" && !locRes.DisplayName.StartsWith("Im Transit"))
            {
                var ts = ParseTs(line);
                LocationVisits.Add((ts, locRes.RawCode, locRes.DisplayName, locRes.SystemName, locRes.ParentBody, locRes.Type.ToString()));
                if (locRes.DisplayName != _lastLoc)
                {
                    _lastLoc = locRes.DisplayName;
                    return new LogEntry { Time = ts, Kind = EventKind.Location, Detail = _lastLoc };
                }
            }
        }

        var zn = ZoneRegex().Match(line);
        if (zn.Success)
        {
            var raw = zn.Groups["loc"].Value;
            var locRes = Locations.ResolveLocation(raw);
            if (locRes.DisplayName != "—" && !locRes.DisplayName.StartsWith("Im Transit"))
            {
                var ts = ParseTs(line);
                LocationVisits.Add((ts, locRes.RawCode, locRes.DisplayName, locRes.SystemName, locRes.ParentBody, locRes.Type.ToString()));
                if (locRes.DisplayName != _lastLoc)
                {
                    _lastLoc = locRes.DisplayName;
                    return new LogEntry { Time = ts, Kind = EventKind.Location, Detail = _lastLoc };
                }
            }
        }

        var iv = InvRegex().Match(line);
        if (iv.Success)
        {
            // rohe Inventar-ID durch den zuletzt bekannten Standort ersetzen
            var place = _lastLoc ?? "Lager";
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Inventory, Detail = $"{place}  ·  {iv.Groups["cnt"].Value} Item(s)" };
        }

        var scj = ShipChannelJoinRegex().Match(line);
        if (scj.Success)
        {
            var rawShip = scj.Groups["ship"].Value.Trim();
            var ship = Ships.Prettify(rawShip);
            _lastShip = ship;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = ship, Ship = ship };
        }

        var ins = ItemNavShipRegex().Match(line);
        if (ins.Success)
        {
            var rawShip = ins.Groups["ship"].Value;
            var ship = Ships.Prettify(rawShip);
            _lastShip = ship;
        }

        var ve = VehRegex().Match(line);
        if (ve.Success)
        {
            var ship = Ships.Prettify(ve.Groups["ship"].Value);
            _lastShip = ship;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = ship, Ship = ship };
        }

        var vr = VehicleRetrievalRegex().Match(line);
        if (vr.Success)
        {
            var ship = Ships.Prettify(vr.Groups["ship"].Value);
            _lastShip = ship;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = ship, Ship = ship };
        }

        // Loot: nur von der Welt gespawnte Items (Kisten/Gegner), kein Kauf/Umräumen
        var lt = LootStoreRegex().Match(line);
        if (lt.Success)
        {
            if (!lt.Groups["ctx"].Value.Contains("Runtime-spawned")) return null;
            var cls = lt.Groups["cls"].Value;
            var name = Localization.ItemName(cls) ?? CleanLootName(cls);   // echter Name aus global.ini, sonst bereinigter Code
            if (name.Length >= 3 && name != _lastLoot) { _lastLoot = name; return new LogEntry { Time = ParseTs(line), Kind = EventKind.Loot, Detail = name }; }
            return null;
        }

        // Loot, das direkt vom Boden/Leiche ausgerüstet wird (Armor-Swap) – nicht im Inventar
        var el = EquipLootRegex().Match(line);
        if (el.Success)
        {
            var cls = el.Groups["cls"].Value;
            var name = Localization.ItemName(cls) ?? CleanLootName(cls);
            if (name.Length >= 3 && name != _lastLoot) { _lastLoot = name; return new LogEntry { Time = ParseTs(line), Kind = EventKind.Loot, Detail = name }; }
            return null;
        }

        // Quantum-Reise: nur ABGESCHLOSSENE Sprünge (Ankunft).
        var qt = QtArriveRegex().Match(line);
        if (qt.Success)
        {
            var t = ParseTs(line);
            var ship = Ships.Prettify(qt.Groups["ship"].Value);
            string? destination = null;
            if ((t - _lastQt).TotalSeconds > 3)   // doppelte Logzeilen entprellen
            {
                _lastQt = t;
                string destText = "";
                if (!string.IsNullOrEmpty(_pendingQtDestination))
                {
                    var resDest = Locations.ResolveLocation(_pendingQtDestination);
                    if (resDest.DisplayName != "—" && !resDest.DisplayName.StartsWith("Im Transit"))
                    {
                        _lastLoc = resDest.DisplayName;
                        destination = _lastLoc;
                        destText = $" (bei {_lastLoc})";

                        if (resDest.SystemName is "Nyx" or "Pyro" or "Stanton")
                        {
                            _currentSystem = resDest.SystemName;
                            Locations.ActiveSystem = resDest.SystemName;
                        }
                    }
                    _pendingQtDestination = null;
                }
                else if (_lastLoc != null)
                {
                    destText = $" (bei {_lastLoc})";
                }

                if (!string.IsNullOrEmpty(destination))
                {
                    QuantumDestinations.Add((t, destination));
                }

                return new LogEntry
                {
                    Time = t,
                    Kind = EventKind.Quantum,
                    Ship = ship,
                    Location = destination,
                    Detail = $"QT-Ankunft · {ship}{destText}"
                };
            }
        }

        // Kill-Feed (Combat)
        var kl = KillLineRegex().Match(line);
        if (kl.Success)
        {
            var victim = kl.Groups["victim"].Value;
            var killer = kl.Groups["killer"].Value;
            var weapon = ItemNames.CleanFallback(kl.Groups["weapon"].Value);
            if (_ownNames.Contains(victim))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Death, Detail = $"☠ getötet von {killer} ({weapon})" };
            }
            if (_ownNames.Contains(killer))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Kill, Detail = $"Kill: {victim} ({weapon})" };
            }
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Kill, Detail = $"{killer} ✟ {victim} ({weapon})" };
        }

        // SC 4.x Fracht- & Schiffs-Aufzüge (technisches Hintergrundrauschen herausfiltern)
        var elv = ElevatorStateRegex().Match(line);
        if (elv.Success)
        {
            return null;
        }

        // ATC Landefreigabe & Hangar-Zuweisung
        var atc = AtcHangarRegex().Match(line);
        if (atc.Success)
        {
            var hangar = atc.Groups["hangar"].Value.Trim();
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Hangar, Detail = $"Landefreigabe: {hangar}" };
        }

        // Schiffsverlust (Zerstörung / Selbstzerstörung)
        var vd = VehicleDestroyedRegex().Match(line);
        if (vd.Success)
        {
            var rawShip = vd.Groups["ship"].Value;
            var ship = !string.IsNullOrEmpty(rawShip) ? Ships.Prettify(rawShip) : "Fahrzeug";
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.ShipLoss, Detail = $"{ship} zerstört / Selbstzerstörung", Ship = ship };
        }

        // Versicherungs-Claim
        var ic = InsuranceClaimRegex().Match(line);
        if (ic.Success)
        {
            var rawShip = ic.Groups["ship"].Value;
            var ship = !string.IsNullOrEmpty(rawShip) ? Ships.Prettify(rawShip) : "Schiff";
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = $"Versicherungs-Claim: {ship}", Ship = ship };
        }

        // Schiffsverlust (Kollision) – zählt auch zur Flotte (dein Schiff)
        var fc = CollisionRegex().Match(line);
        if (fc.Success)
        {
            var ship = Ships.Prettify(fc.Groups["ship"].Value);
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.ShipLoss, Detail = $"{ship} – Kollision", Ship = ship };
        }

        // Entitlement/Miete gestartet
        if (line.Contains("<EntitlementStarted>", StringComparison.Ordinal))
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Entitlement, Detail = "Entitlement/Miete gestartet" };

        // Comm-Kanal -> eigene Schiffe in die Flotte, fremde als Party-Schiff
        var ch = ChannelRegex().Match(line);
        if (ch.Success)
        {
            var shipName = ch.Groups["ship"].Value.Trim();
            var owner = ch.Groups["owner"].Value.Trim();
            if (_ownNames.Contains(owner))
            {
                if (_channelSeen.Add("me|" + shipName))
                    return new LogEntry { Time = ParseTs(line), Kind = EventKind.Vehicle, Detail = shipName, Ship = shipName };
            }
            else if (_channelSeen.Add(shipName + "|" + owner))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Party, Detail = $"Schiff: {shipName} · {owner}" };
            }
            return null;
        }

        // Getragene Ausrüstung (einmal je Item)
        var at = AttachRegex().Match(line);
        if (at.Success)
        {
            var name = CleanLoadout(at.Groups["item"].Value);
            if (name != null && _loadoutSeen.Add(name))
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Loadout, Detail = name };
            return null;
        }

        // Aufträge zuerst mit VOLLEM Text (Name/Rang/Route)
        var ms = MissionLineRegex().Match(line);
        if (ms.Success)
        {
            var full = CleanMission(ms.Groups["full"].Value);
            if (full == _lastNotif) return null;
            _lastNotif = full;

            var midMatch = NotificationMissionIdRegex().Match(line);
            string mId = midMatch.Success && midMatch.Groups["id"].Value != "00000000-0000-0000-0000-000000000000"
                ? midMatch.Groups["id"].Value
                : "";

            var cleanTitle = Regex.Replace(full, @"^(?:Contract\s+(?:Accepted|Complete|Completed|Failed|Shared|Withdrawn|Abandoned|Cancelled)|Auftrag\s+(?:angenommen|abgeschlossen|fehlgeschlagen|geteilt|zurückgezogen|abgebrochen|aufgegeben)|New\s+(?:Contract\s+Available|Objective)|Neuer\s+Auftrag|Mission\s+(?:Complete|Completed|Accepted|Finished)|Erfolgreich):\s*", "", RegexOptions.IgnoreCase).Trim(' ', ':');
            cleanTitle = cleanTitle.Replace("[BP]", "").Trim(' ', ':');
            var normTitle = cleanTitle.ToLowerInvariant().Trim();

            if (string.IsNullOrEmpty(mId))
            {
                mId = "contract_" + normTitle;
            }

            var cat = MissionCatalog.FuzzyLookup(cleanTitle) ?? MissionCatalog.FuzzyLookup(full);

            bool isComplete = full.Contains("Complete", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("abgeschlossen", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("Erfolgreich", StringComparison.OrdinalIgnoreCase);

            bool isAbandoned = full.Contains("Abandoned", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("Withdrawn", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("abgebrochen", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("aufgegeben", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("fehlgeschlagen", StringComparison.OrdinalIgnoreCase) ||
                               full.Contains("zurückgezogen", StringComparison.OrdinalIgnoreCase);

            bool isObjective = full.Contains("New Objective", StringComparison.OrdinalIgnoreCase);

            bool isAccepted = full.Contains("Accepted", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("angenommen", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("Shared", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("geteilt", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("Neuer Auftrag", StringComparison.OrdinalIgnoreCase) ||
                              full.Contains("New Contract Available", StringComparison.OrdinalIgnoreCase);

            long reward = 0;
            if (isComplete)
            {
                if (cat != null && cat.BaseReward > 0)
                {
                    reward = cat.BaseReward;
                }
                else if (full.Contains("Missing Person", StringComparison.OrdinalIgnoreCase))
                {
                    reward = 21250;
                }
                else if (full.Contains("Bounty", StringComparison.OrdinalIgnoreCase) || full.Contains("Target", StringComparison.OrdinalIgnoreCase))
                {
                    reward = 32000;
                }
                else
                {
                    reward = 25000; // Standard aUEC für Belohnungs-Events, damit kein 0-Betrag angezeigt wird
                }

                // In _contracts aktualisieren
                string? targetKey = null;
                if (_contracts.ContainsKey(mId)) targetKey = mId;
                else
                {
                    targetKey = _contracts.Keys.FirstOrDefault(k =>
                        _contracts[k].Outcome == ContractOutcome.InProgress &&
                        (!string.IsNullOrEmpty(normTitle) && _contracts[k].Title.ToLowerInvariant().Contains(normTitle) ||
                         normTitle.Contains(_contracts[k].Title.ToLowerInvariant())));
                }

                if (targetKey != null && _contracts.TryGetValue(targetKey, out var existing))
                {
                    _contracts[targetKey] = existing with
                    {
                        Outcome = ContractOutcome.Completed,
                        CompletedAt = existing.CompletedAt ?? ParseTs(line),
                        StepsDone = Math.Max(existing.StepsTotal, existing.StepsDone),
                        Reward = reward > 0 ? reward : existing.Reward
                    };
                }
                else
                {
                    var issuer = ResolveIssuer(cat, mId);
                    var sys = ResolveMissionSystem(cat, issuer);
                    _contracts[mId] = new ContractRecord
                    {
                        MissionId = mId,
                        AcceptedAt = ParseTs(line),
                        CompletedAt = ParseTs(line),
                        Title = !string.IsNullOrEmpty(cat?.Title) ? cat.Title : cleanTitle,
                        Issuer = issuer,
                        Type = cat?.MissionType ?? "Auftrag",
                        Difficulty = "k.A.",
                        System = sys,
                        StepsTotal = 1,
                        StepsDone = 1,
                        Reward = reward,
                        Outcome = ContractOutcome.Completed
                    };
                }
                _missionsDone.Add(mId);
            }
            else if (isAbandoned)
            {
                string? targetKey = null;
                if (_contracts.ContainsKey(mId)) targetKey = mId;
                else
                {
                    targetKey = _contracts.Keys.FirstOrDefault(k =>
                        _contracts[k].Outcome == ContractOutcome.InProgress &&
                        (!string.IsNullOrEmpty(normTitle) && _contracts[k].Title.ToLowerInvariant().Contains(normTitle) ||
                         normTitle.Contains(_contracts[k].Title.ToLowerInvariant())));
                }

                if (targetKey != null && _contracts.TryGetValue(targetKey, out var existing))
                {
                    _contracts[targetKey] = existing with
                    {
                        Outcome = ContractOutcome.Abandoned,
                        CompletedAt = existing.CompletedAt ?? ParseTs(line)
                    };
                }
                else
                {
                    var issuer = ResolveIssuer(cat, mId);
                    var sys = ResolveMissionSystem(cat, issuer);
                    _contracts[mId] = new ContractRecord
                    {
                        MissionId = mId,
                        AcceptedAt = ParseTs(line),
                        CompletedAt = ParseTs(line),
                        Title = !string.IsNullOrEmpty(cat?.Title) ? cat.Title : cleanTitle,
                        Issuer = issuer,
                        Type = cat?.MissionType ?? "Auftrag",
                        Difficulty = "k.A.",
                        System = sys,
                        StepsTotal = 1,
                        StepsDone = 0,
                        Reward = cat?.BaseReward ?? 0,
                        Outcome = ContractOutcome.Abandoned
                    };
                }
            }
            else if (isObjective)
            {
                if (_contracts.TryGetValue(mId, out var existing))
                {
                    _contracts[mId] = existing with { StepsTotal = existing.StepsTotal + 1 };
                }
                else
                {
                    var activeKey = _contracts.Keys.LastOrDefault(k => _contracts[k].Outcome == ContractOutcome.InProgress);
                    if (activeKey != null)
                    {
                        _contracts[activeKey] = _contracts[activeKey] with { StepsTotal = _contracts[activeKey].StepsTotal + 1 };
                    }
                }
            }
            else if (isAccepted)
            {
                var finalReward = cat?.BaseReward ?? 0;
                var finalIssuer = ResolveIssuer(cat, mId);
                var finalType = cat?.MissionType ?? (cleanTitle.Contains("Missing Person", StringComparison.OrdinalIgnoreCase) ? "Person/Bergung" : "Auftrag");
                var finalSystem = ResolveMissionSystem(cat, finalIssuer);

                if (!_contracts.TryGetValue(mId, out var existing))
                {
                    _contracts[mId] = new ContractRecord
                    {
                        MissionId = mId,
                        AcceptedAt = ParseTs(line),
                        Title = !string.IsNullOrEmpty(cat?.Title) ? cat.Title : cleanTitle,
                        Issuer = finalIssuer,
                        Type = finalType,
                        Difficulty = "k.A.",
                        System = finalSystem,
                        StepsTotal = 1,
                        StepsDone = 0,
                        Reward = finalReward,
                        Outcome = ContractOutcome.InProgress
                    };
                }
                else
                {
                    _contracts[mId] = existing with
                    {
                        Title = !string.IsNullOrEmpty(cat?.Title) ? cat.Title : existing.Title,
                        Issuer = finalIssuer != "Unbekannt" ? finalIssuer : (existing.Issuer != "mobiGlas" ? existing.Issuer : "Unbekannt"),
                        Type = finalType != "Auftrag" ? finalType : existing.Type,
                        Reward = finalReward > 0 ? finalReward : existing.Reward,
                        System = finalSystem != "Stanton" ? finalSystem : existing.System
                    };
                }
                _missionsTaken.Add(mId);
            }

            return new LogEntry
            {
                Time = ParseTs(line),
                Kind = isComplete ? EventKind.MissionReward : EventKind.Mission,
                Amount = reward,
                Detail = full
            };
        }

        // Party-Mitglied beigetreten / verlassen (mit Name)
        var pj = PartyJoinRegex().Match(line);
        if (pj.Success)
        {
            var key = "j:" + pj.Groups["who"].Value;
            if (key == _lastParty) return null;
            _lastParty = key;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Party, Detail = $"▸ {pj.Groups["who"].Value} ist beigetreten" };
        }
        var pjn = PartyMemberJoinNotifRegex().Match(line);
        if (pjn.Success)
        {
            var who = pjn.Groups["who"].Value.Trim();
            var key = "j:" + who;
            if (key == _lastParty) return null;
            _lastParty = key;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Party, Detail = $"▸ {who} ist beigetreten" };
        }
        var pl = PartyLeaveRegex().Match(line);
        if (pl.Success)
        {
            var who = pl.Groups["who"].Value;
            if (who.Equals("Du", StringComparison.OrdinalIgnoreCase)) who = "Du";
            var key = "l:" + who;
            if (key == _lastParty) return null;
            _lastParty = key;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Party, Detail = $"◂ {who} hat verlassen" };
        }
        var pln = PartyMemberLeaveNotifRegex().Match(line);
        if (pln.Success)
        {
            var who = pln.Groups["who"].Value.Trim();
            if (who.Equals("Du", StringComparison.OrdinalIgnoreCase)) who = "Du";
            var key = "l:" + who;
            if (key == _lastParty) return null;
            _lastParty = key;
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Party, Detail = $"◂ {who} hat verlassen" };
        }

        // Comms-Benachrichtigung für Missionsannahme mit Auftraggeber & Fraktion (SC 4.x)
        var commsMatch = CommsNotificationRegex().Match(line);
        if (commsMatch.Success)
        {
            var id = commsMatch.Groups["id"].Value;
            var giver = FormatMissionGiver(commsMatch.Groups["giver"].Value);
            var faction = FormatFaction(commsMatch.Groups["faction"].Value);
            _missionComms[id] = (giver, faction);

            if (_contracts.TryGetValue(id, out var existingContract))
            {
                var resolved = giver != "Unbekannt" ? giver : (faction != "Unbekannt" ? faction : existingContract.Issuer);
                if (resolved != "Unbekannt" && (existingContract.Issuer == "Unbekannt" || existingContract.Issuer == "mobiGlas"))
                {
                    var sys = (resolved.Contains("Recco", StringComparison.OrdinalIgnoreCase) || resolved.Contains("Battaglia", StringComparison.OrdinalIgnoreCase)) ? "Nyx" : existingContract.System;
                    _contracts[id] = existingContract with { Issuer = resolved, System = sys };
                }
            }
        }

        // Ausrüstung/Item defekt – jedes Item nur EINMAL (Warnung feuert sonst im Sekundentakt)
        var gb = GearBrokeRegex().Match(line);
        if (gb.Success)
        {
            var item = gb.Groups["item"].Value.Trim();
            if (item.Length > 0 && _gearSeen.Add(item))
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.Gear, Detail = $"{item} unbrauchbar" };
            return null;
        }

        // Angenommene Mission mit Auftraggeber/Fraktion – je missionId nur EINMAL
        var mk = MissionMarkerRegex().Match(line);
        if (mk.Success)
        {
            var mId = mk.Groups["id"].Value;
            var gen = mk.Groups["gen"].Value;
            var con = mk.Groups["con"].Value;
            var info = Missions.Derive(gen, con);

            if (!_contracts.TryGetValue(mId, out var existing))
            {
                _contracts[mId] = new ContractRecord
                {
                    MissionId = mId,
                    AcceptedAt = ParseTs(line),
                    Title = Missions.Format(info),
                    Issuer = info.Faction,
                    Type = info.Type,
                    Difficulty = info.Difficulty,
                    System = info.System,
                    StepsTotal = 1,
                    StepsDone = 0,
                    Outcome = ContractOutcome.InProgress
                };
            }
            else
            {
                _contracts[mId] = existing with
                {
                    Issuer = info.Faction != "Unbekannt" && (existing.Issuer == "Unbekannt" || existing.Issuer == "mobiGlas") ? info.Faction : existing.Issuer,
                    Type = info.Type != "Sonstige" ? info.Type : existing.Type,
                    Difficulty = info.Difficulty != "k.A." ? info.Difficulty : existing.Difficulty,
                    System = info.System != "k.A." ? info.System : existing.System
                };
            }

            if (_missionsTaken.Add(mId))
            {
                return new LogEntry { Time = ParseTs(line), Kind = EventKind.MissionTaken, Detail = Missions.Format(info) };
            }
            return null;
        }

        // Abgeschlossene Mission (Server-Event) – nur Zähler erhöhen, keine doppelte Tabellen-Zeile
        var md = MissionDoneRegex().Match(line);
        if (md.Success)
        {
            var mId = md.Groups["id"].Value;
            _missionsDone.Add(mId);
            if (_contracts.TryGetValue(mId, out var existing))
            {
                _contracts[mId] = existing with
                {
                    Outcome = ContractOutcome.Completed,
                    CompletedAt = existing.CompletedAt ?? ParseTs(line)
                };
            }
            return null;
        }

        // Blaupause / Crafting Blueprint / Belohnung erhalten
        var bpName = TryExtractBlueprint(line);
        if (bpName != null)
        {
            var t = ParseTs(line);
            if (_seenBlueprints.TryGetValue(bpName, out var lastT) && (t - lastT).TotalSeconds < 30)
            {
                return null; // 3-stufige Star Citizen UI Animation (Next / StartFade / Remove) entprellen
            }
            _seenBlueprints[bpName] = t;
            return new LogEntry { Time = t, Kind = EventKind.Blueprint, Detail = bpName };
        }

        // Bußgeld gezahlt – echtes aUEC raus (fließt in den Saldo)
        var fn = FineLineRegex().Match(line);
        if (fn.Success)
        {
            long amt = ParseAmt(fn.Groups["amt"].Value);
            var ts = ParseTs(line);
            LedgerRecords.Add(new LedgerRecord
            {
                Timestamp = ts,
                Kind = "Strafe gezahlt",
                What = $"Strafe: {amt:N0} aUEC",
                Where = PlaceAt(ts),
                Shop = "Klescher / UEE",
                Amount = -amt,
                Quantity = 1,
                Confirmed = true
            });
            return new LogEntry { Time = ts, Kind = EventKind.Fine, Amount = -amt, Detail = $"Strafe gezahlt: {amt:N0} aUEC" };
        }

        // Begangene Straftat (Crimestat)
        var cr = CrimeLineRegex().Match(line);
        if (cr.Success)
        {
            var crime = cr.Groups["crime"].Value.TrimEnd(' ', ':');
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Crime, Detail = crime };
        }

        // Veredelungs-Auftrag abgeschlossen (Refinery)
        var rf = RefineryLineRegex().Match(line);
        if (rf.Success)
        {
            var where = rf.Groups["txt"].Value.Trim().TrimStart('.').Trim().TrimEnd('.');
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Refinery, Detail = where.Length > 0 ? $"Veredelung fertig {where}" : "Veredelung fertig" };
        }

        // Verletzung/Lähmung festgestellt (Körperteil + Behandlungsstufe)
        var ij = InjuryLineRegex().Match(line);
        if (ij.Success)
        {
            var txt = ij.Groups["txt"].Value.Replace(" Behandlung erforderlich", "").Replace(" festgestellt", "").Trim().TrimEnd(' ', ':', '-');
            if (txt != _lastNotif) { _lastNotif = txt; return new LogEntry { Time = ParseTs(line), Kind = EventKind.Injury, Detail = txt }; }
            return null;
        }

        // Notifications -> Gebiete / Party / Med-Bett / Hangar / Gefängnis / Angebote
        var nt = NotifRegex().Match(line);
        if (nt.Success)
        {
            var txt = nt.Groups["txt"].Value.Trim();
            if (txt == _lastNotif) return null;       // exakte Wiederholung überspringen
            _lastNotif = txt;

            var off = OfferRegex().Match(txt);
            if (off.Success)
                return new LogEntry
                {
                    Time = ParseTs(line),
                    Kind = EventKind.Offer,
                    Amount = ParseAmt(off.Groups["amt"].Value),     // nur Anzeige, nicht in Bilanz
                    Detail = $"Angebot von {off.Groups["who"].Value.Trim()}"
                };

            var kind = Categorize(txt);
            if (kind != null)
                return new LogEntry { Time = ParseTs(line), Kind = kind.Value, Detail = txt };

            // unbekannte Notification -> für Diagnose merken (Debug-Log)
            Unknown.AddOrUpdate(txt, 1, (_, c) => c + 1);
        }

        // Game Crash / Fatal Error Erkennung
        if (line.Contains("FATAL ERROR", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Fatal Error Thrown", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("STATUS_CRYENGINE_FATAL_ERROR", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Is fatal error: Yes", StringComparison.OrdinalIgnoreCase))
        {
            var msg = "Spiel-Absturz erkannt (Fatal Error / Crash)";
            if (line.Contains("Fatal Error Thrown:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = line.IndexOf("Fatal Error Thrown:", StringComparison.OrdinalIgnoreCase);
                var sub = line[(idx + "Fatal Error Thrown:".Length)..].Trim();
                if (sub.Length > 0) msg = $"Spiel-Absturz: {sub}";
            }
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.Crash, Detail = msg };
        }

        // Session-Ende / Disconnect / Shard-Exit
        if (line.Contains("CDisciplineServiceExternal::EndSession", StringComparison.Ordinal))
        {
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.SessionChange, Detail = "Server-Verbindung getrennt / Sitzung beendet (EndSession)" };
        }

        // Server-Beitritt (<Join PU>)
        if (line.Contains("<Join PU>", StringComparison.Ordinal))
        {
            var mShard = Regex.Match(line, @"shard\[(?<shard>[^\]]+)\]");
            var shardName = mShard.Success ? mShard.Groups["shard"].Value : "PU";
            return new LogEntry { Time = ParseTs(line), Kind = EventKind.SessionChange, Detail = $"Server beigetreten (Shard: {shardName})" };
        }

        return null;
    }

    static EventKind? Categorize(string t)
    {
        if (t.Contains("FREUND", StringComparison.OrdinalIgnoreCase) || t.Contains("Freund hinzu", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Friend", StringComparison.OrdinalIgnoreCase)) return EventKind.Friend;

        if (t.Contains("beschlagnahmt", StringComparison.OrdinalIgnoreCase) || t.Contains("Beschlagnahm", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("impound", StringComparison.OrdinalIgnoreCase)) return EventKind.Impound;

        if (t.Contains("Kampfunfähig", StringComparison.OrdinalIgnoreCase) || t.Contains("Notfalldienste", StringComparison.OrdinalIgnoreCase) || 
            t.Contains("Incapacitat", StringComparison.OrdinalIgnoreCase) || t.Contains("Emergency Medical", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Wiederbelebt", StringComparison.OrdinalIgnoreCase) || t.Contains("Revived", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Killed", StringComparison.OrdinalIgnoreCase)) return EventKind.Death;

        if (t.StartsWith("Auftrag", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Neuer Auftrag", StringComparison.OrdinalIgnoreCase) || 
            t.Contains("Mission", StringComparison.OrdinalIgnoreCase) || t.Contains("Contract", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Objective", StringComparison.OrdinalIgnoreCase)) return EventKind.Mission;

        if (t.Contains("Klescher", StringComparison.OrdinalIgnoreCase) || t.Contains("Gefängnis", StringComparison.OrdinalIgnoreCase) || 
            t.Contains("Haftstrafe", StringComparison.OrdinalIgnoreCase) || t.Contains("Prison", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Sentence", StringComparison.OrdinalIgnoreCase) || t.Contains("Rehabilitation", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Kopfgeld", StringComparison.OrdinalIgnoreCase) || t.Contains("Bounty", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Verbrechen", StringComparison.OrdinalIgnoreCase) || t.Contains("Crime", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Felony", StringComparison.OrdinalIgnoreCase) || t.Contains("CrimeStat", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("inhaftiert", StringComparison.OrdinalIgnoreCase) || t.Contains("Incarcerated", StringComparison.OrdinalIgnoreCase)) return EventKind.Jurisdiction;

        if (t.StartsWith("Rechtsgebiet", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Kontrollierten Raum", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("Jurisdiction", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Monitored Space", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("Schutzzone", StringComparison.OrdinalIgnoreCase) || t.Contains("Armistice", StringComparison.OrdinalIgnoreCase)) return EventKind.Jurisdiction;

        if (t.StartsWith("Partystart", StringComparison.OrdinalIgnoreCase) || t.StartsWith("Party start", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("GRUPPE", StringComparison.OrdinalIgnoreCase) || t.Contains("Group", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Gruppenanführer", StringComparison.OrdinalIgnoreCase) || t.Contains("Party Leader", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("Party", StringComparison.OrdinalIgnoreCase)) return EventKind.Party;

        if (t.Contains("Krankenbett", StringComparison.OrdinalIgnoreCase) || t.Contains("Medical Bed", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("MedBed", StringComparison.OrdinalIgnoreCase) || t.Contains("Clinic Bed", StringComparison.OrdinalIgnoreCase)) return EventKind.MedBed;

        if (t.StartsWith("Hangar", StringComparison.OrdinalIgnoreCase)) return EventKind.Hangar;

        return null;
    }

    static readonly string[] LoadoutNoise =
    {
        "Default_", "FPS_Default", "Head_", "Shared_", "FP_Visor", "LensDisplay", "Eyedetail",
        "Eyelash", "necksock", "brows_", "hair_", "Inventory_LocalAttach", "Scalp", "Teeth",
        "_LensDisplay", "Skin", "Beard", "Mouth", "PuglioseSkin"
    };

    static string CleanMission(string s)
    {
        s = BpBlockRegex().Replace(s, "");                         // Blueprint-Marker-Block ganz raus
        s = HtmlTagRegex().Replace(s, "");                         // restliche Tags
        // unaufgelöste Platzhalter-Segmente (geteilte Aufträge) entfernen
        s = MissionRankRegex().Replace(s, "");
        s = MissionRouteRegex().Replace(s, "");
        s = MissionTildeRegex().Replace(s, "");
        s = s.Replace("[BP]", "").Trim(' ', ':', '|', '*', '?', '-');
        return MultiSpaceRegex().Replace(s, " ").Trim();
    }

    static string CleanLootName(string cls)
    {
        // Versions-/Varianten-Segmente raus (_04_04_01, _a, _01) und Unterstriche zu Leerzeichen
        var s = LootVersionRegex().Replace(cls, "");
        s = s.Replace('_', ' ').Trim();
        return MultiSpaceRegex().Replace(s, " ");
    }

    static string? CleanLoadout(string raw)
    {
        foreach (var n in LoadoutNoise)
            if (raw.Contains(n, StringComparison.Ordinal)) return null;
        var name = LoadoutTrailingIdRegex().Replace(raw, "");
        if (name.EndsWith("_mag", StringComparison.Ordinal)) return null;     // Magazine ausblenden
        name = name.Replace('_', ' ').Trim();
        return name.Length < 3 ? null : name;
    }

    void CaptureMeta(string line)
    {
        if (_metaComplete) return;

        if (!Meta.ContainsKey("version") && line.Contains("FileVersion:", StringComparison.Ordinal))
            Meta["version"] = After(line, "FileVersion:");
        if (!Meta.ContainsKey("cpu") && line.Contains("Host CPU:", StringComparison.Ordinal))
            Meta["cpu"] = After(line, "Host CPU:");
        if (!Meta.ContainsKey("env") && line.Contains("[Trace] Environment:", StringComparison.Ordinal))
            Meta["env"] = After(line, "Environment:");
        if (!Meta.ContainsKey("gpu"))
        {
            var m = GpuRegex().Match(line);
            if (m.Success) Meta["gpu"] = m.Groups["g"].Value.Trim();
        }
        if (!Meta.ContainsKey("ram"))
        {
            var m = RamRegex().Match(line);
            if (m.Success && long.TryParse(m.Groups["mb"].Value, out var mb)) Meta["ram"] = $"{mb / 1024} GB";
        }
        if (!Meta.ContainsKey("character"))
        {
            var m = CharRegex().Match(line);
            if (m.Success) Meta["character"] = m.Groups["n"].Value;
        }
        if (!Meta.ContainsKey("shard"))
        {
            var m = ShardRegex().Match(line);
            if (m.Success) Meta["shard"] = m.Groups["s"].Value;
        }

        if (Meta.Count >= 7) _metaComplete = true;
    }

    static string After(string line, string key)
    {
        var i = line.IndexOf(key, StringComparison.Ordinal);
        return i < 0 ? "" : line[(i + key.Length)..].Trim();
    }

    static long ParseAmt(string s) =>
        long.TryParse(s.Replace(".", "").Replace(",", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    static string CleanShop(string s)
    {
        s = ScShopPrefixRegex().Replace(s, "");
        return s.Replace('_', ' ').Trim();
    }

    DateTime ParseTs(string line)
    {
        var m = TsRegex().Match(line);
        if (m.Success && DateTime.TryParse(m.Groups["ts"].Value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
        {
            _lastSeenTime = dt;
            return dt;
        }
        return _lastSeenTime ?? DateTime.UtcNow;
    }

    static string? ExtractLocationFromShop(string shop)
    {
        if (string.IsNullOrWhiteSpace(shop)) return null;
        if (shop.Contains("Lorville", StringComparison.OrdinalIgnoreCase)) return "Lorville · Hurston";
        if (shop.Contains("Everus", StringComparison.OrdinalIgnoreCase)) return "Everus Harbor · Hurston";
        if (shop.Contains("NewBabbage", StringComparison.OrdinalIgnoreCase) || shop.Contains("New Babbage", StringComparison.OrdinalIgnoreCase) || shop.Contains("Babbage", StringComparison.OrdinalIgnoreCase)) return "New Babbage · microTech";
        if (shop.Contains("Tressler", StringComparison.OrdinalIgnoreCase)) return "Port Tressler · microTech";
        if (shop.Contains("Area18", StringComparison.OrdinalIgnoreCase) || shop.Contains("Area 18", StringComparison.OrdinalIgnoreCase)) return "Area 18 · ArcCorp";
        if (shop.Contains("Baijini", StringComparison.OrdinalIgnoreCase)) return "Baijini Point · ArcCorp";
        if (shop.Contains("Orison", StringComparison.OrdinalIgnoreCase)) return "Orison · Crusader";
        if (shop.Contains("Seraphim", StringComparison.OrdinalIgnoreCase)) return "Seraphim Station · Crusader";
        if (shop.Contains("GrimHEX", StringComparison.OrdinalIgnoreCase) || shop.Contains("Grim HEX", StringComparison.OrdinalIgnoreCase)) return "Grim HEX · Yela";
        if (shop.Contains("Levski", StringComparison.OrdinalIgnoreCase)) return "Levski · Delamar";
        if (shop.Contains("Checkmate", StringComparison.OrdinalIgnoreCase)) return "Checkmate Station · Monox";
        if (shop.Contains("Orbituary", StringComparison.OrdinalIgnoreCase)) return "Orbituary · Bloom";
        if (shop.Contains("Ruin", StringComparison.OrdinalIgnoreCase)) return "Ruin Station · Terminus";
        return null;
    }

    static string Clean(string s) => s.Trim().TrimEnd('"').Trim();

    /// <summary>
    /// Ordnet einen rohen oder bereinigten Ausrüstungsnamen einem Loadout-Slot zu.
    /// </summary>
    public static (LoadoutSlotType slot, string slotName, string icon) ClassifyLoadoutSlot(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return (LoadoutSlotType.Primary1, "Unbekannt", "❓");

        var lower = rawName.ToLowerInvariant();

        // 1. Helme
        if (lower.Contains("helmet") || lower.Contains("_helm_") || lower.Contains("_hlm_") || lower.Contains("_hat_") || lower.Contains("helm"))
            return (LoadoutSlotType.Helmet, "Helm", "🪖");

        // 2. Rumpfpanzerung / Core / Torso
        if (lower.Contains("torso") || lower.Contains("_core_") || lower.Contains("_chest_") || lower.Contains("_vest_") || lower.Contains("_suit_core_") || lower.Contains("armor_heavy_core") || lower.Contains("armor_medium_core") || lower.Contains("armor_light_core") || lower.Contains("core"))
            return (LoadoutSlotType.Torso, "Torso / Core", "🥋");

        // 3. Armpanzerung
        if (lower.Contains("_arms_") || lower.Contains("_arm_") || lower.Contains("_shoulder_") || lower.Contains("arms"))
            return (LoadoutSlotType.Arms, "Arme", "🦾");

        // 4. Beinpanzerung
        if (lower.Contains("_legs_") || lower.Contains("_leg_") || lower.Contains("_pants_") || lower.Contains("legs"))
            return (LoadoutSlotType.Legs, "Beine", "🦿");

        // 5. Rucksack
        if (lower.Contains("backpack") || lower.Contains("_bp_") || lower.Contains("_pack_"))
            return (LoadoutSlotType.Backpack, "Rucksack", "🎒");

        // 6. Undersuit / Fliegeranzug
        if (lower.Contains("undersuit") || lower.Contains("_suit_") || lower.Contains("flightsuit") || lower.Contains("suit"))
            return (LoadoutSlotType.Undersuit, "Undersuit", "🩱");

        // 7. Multi-Tool & Utility
        if (lower.Contains("multitool") || lower.Contains("multi_tool") || lower.Contains("pyro_ryt") || lower.Contains("tractor") || lower.Contains("mining_laser") || lower.Contains("cutter") || lower.Contains("utility"))
            return (LoadoutSlotType.MultiTool, "Multi-Tool", "🔧");

        // 8. MedItem / Medgun / Medpens
        if (lower.Contains("medgun") || lower.Contains("paramed") || lower.Contains("medpen") || lower.Contains("oxypen") || lower.Contains("hemozal") || lower.Contains("curelife") || lower.Contains("med_"))
            return (LoadoutSlotType.MedItem, "Med-Kit / Pen", "💉");

        // 9. Sidearm / Pistolen
        if (lower.Contains("pistol") || lower.Contains("magnum") || lower.Contains("revolver") || lower.Contains("arclight") || lower.Contains("lh86") || lower.Contains("salvo") || lower.Contains("yubarev") || lower.Contains("coda"))
            return (LoadoutSlotType.Sidearm, "Seitenwaffe", "🔫");

        // 10. Primärwaffen
        if (lower.Contains("rifle") || lower.Contains("shotgun") || lower.Contains("sniper") || lower.Contains("smg") || lower.Contains("lmg") || lower.Contains("launcher") || lower.Contains("weapon") || lower.Contains("behr_") || lower.Contains("klwe_") || lower.Contains("gemini_") || lower.Contains("karna_") || lower.Contains("custodian_") || lower.Contains("p8sc_") || lower.Contains("f55_") || lower.Contains("s71_") || lower.Contains("gallant_") || lower.Contains("klaus_") || lower.Contains("apx_"))
            return (LoadoutSlotType.Primary1, "Primärwaffe", "🎯");

        return (LoadoutSlotType.Primary2, "Ausrüstung", "🛡️");
    }

    private string ResolveIssuer(MissionInfo? cat, string mId)
    {
        if (_missionComms.TryGetValue(mId, out var comms))
        {
            if (!string.IsNullOrWhiteSpace(comms.Giver) && comms.Giver != "Unbekannt")
                return comms.Giver;
            if (!string.IsNullOrWhiteSpace(comms.Faction) && comms.Faction != "Unbekannt")
                return comms.Faction;
        }

        if (cat != null)
        {
            if (!string.IsNullOrWhiteSpace(cat.Contractor))
                return cat.Contractor;
            if (!string.IsNullOrWhiteSpace(cat.Faction))
                return cat.Faction;
        }

        return "Unbekannt";
    }

    private string ResolveMissionSystem(MissionInfo? cat, string issuer)
    {
        if (cat != null && !string.IsNullOrWhiteSpace(cat.StarSystems) && cat.StarSystems != "k.A.")
            return cat.StarSystems;

        if (!string.IsNullOrWhiteSpace(issuer))
        {
            if (issuer.Contains("Recco", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Battaglia", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("People's Alliance", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Levski", StringComparison.OrdinalIgnoreCase))
                return "Nyx";

            if (issuer.Contains("Pyro", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Rough Cut", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Citizens for Pyro", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Fire Rats", StringComparison.OrdinalIgnoreCase))
                return "Pyro";

            if (issuer.Contains("Hurston", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Crusader", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("microTech", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("ArcCorp", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Red Wind", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Northrock", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Clovus", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Miles", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Wallace", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Tecia", StringComparison.OrdinalIgnoreCase) ||
                issuer.Contains("Twitch", StringComparison.OrdinalIgnoreCase))
                return "Stanton";
        }

        if (!string.IsNullOrWhiteSpace(_currentSystem))
            return _currentSystem;

        return "Stanton";
    }

    private static string FormatMissionGiver(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unbekannt";
        var clean = raw.Trim();
        return clean switch
        {
            "ReccoBattaglia" => "Recco Battaglia",
            "MilesEckhart" => "Miles Eckhart",
            "ClovusDarneely" => "Clovus Darneely",
            "ConstantineHurston" => "Constantine Hurston",
            "TeciaPacheco" => "Tecia Pacheco",
            "WallaceKlim" => "Wallace Klim",
            "Ruto" => "Ruto",
            "Vaughn" => "Vaughn",
            "RedWind" => "Red Wind Line",
            "NorthRock" => "Northrock Service Group",
            "LingBiotechnology" => "Ling Biotechnology",
            "MicroTechLogistics" => "microTech Logistics",
            "CrusaderIndustries" => "Crusader Industries",
            "HurstonDynamics" => "Hurston Dynamics",
            "ArcCorp" => "ArcCorp",
            _ => SplitCamelCase(clean)
        };
    }

    private static string FormatFaction(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unbekannt";
        var clean = raw.Trim();
        return clean switch
        {
            "PeopleAlliance" => "People's Alliance",
            "CitizensForPyrosFuture" => "Citizens for Pyro's Future",
            "RoughCut" => "Rough Cut",
            "FireRats" => "Fire Rats",
            "NineTails" => "Nine Tails",
            "XenoThreat" => "XenoThreat",
            "Dusters" => "Dusters",
            "Overlords" => "Overlords",
            _ => SplitCamelCase(clean)
        };
    }

    private static string SplitCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return Regex.Replace(str, @"(\B[A-Z])", " $1").Trim();
    }
}

