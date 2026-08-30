using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogReader.Core;

/// <summary>
/// Vollständiger Location-Resolver für Star Citizen Standorte und Systeme.
/// Übersetzt interne Star Citizen Location-IDs, SOCPak-Container, OOC-Objekte,
/// Lagrange-Punkte (RR_*), Sprungtore (rs_ext_*) und Planeten-Outposts in saubere,
/// displayfähige und kartografierbare Standorte mit System- und Himmelskörper-Zuordnung.
/// </summary>
public static partial class Locations
{
    #region Static Universe Dictionaries

    public static readonly IReadOnlyDictionary<string, string> StantonBodies =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "Hurston",
            ["1a"] = "Arial",
            ["1b"] = "Aberdeen",
            ["1c"] = "Magda",
            ["1d"] = "Ita",
            ["2"] = "Crusader",
            ["2a"] = "Cellin",
            ["2b"] = "Daymar",
            ["2c"] = "Yela",
            ["3"] = "ArcCorp",
            ["3a"] = "Lyria",
            ["3b"] = "Wala",
            ["4"] = "microTech",
            ["4a"] = "Calliope",
            ["4b"] = "Clio",
            ["4c"] = "Euterpe"
        };

    public static readonly IReadOnlyDictionary<string, string> PyroBodies =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "Pyro I",
            ["2"] = "Monox",
            ["3"] = "Bloom",
            ["4"] = "Pyro IV",
            ["5"] = "Pyro V",
            ["6"] = "Terminus"
        };

    public static readonly IReadOnlyDictionary<string, (string System, string Body)> RestStopBodies =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["MIC"] = ("Stanton", "microTech"),
            ["CRU"] = ("Stanton", "Crusader"),
            ["HUR"] = ("Stanton", "Hurston"),
            ["ARC"] = ("Stanton", "ArcCorp"),
            ["S1"] = ("Stanton", "Hurston"),
            ["S2"] = ("Stanton", "Crusader"),
            ["S3"] = ("Stanton", "ArcCorp"),
            ["S4"] = ("Stanton", "microTech"),
            ["P1"] = ("Pyro", "Pyro I"),
            ["P2"] = ("Pyro", "Monox"),
            ["P3"] = ("Pyro", "Bloom"),
            ["P4"] = ("Pyro", "Pyro IV"),
            ["P5"] = ("Pyro", "Pyro V"),
            ["P6"] = ("Pyro", "Terminus")
        };

    public static readonly IReadOnlyDictionary<string, string> LeoStations =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HUR"] = "Everus Harbor",
            ["S1"] = "Everus Harbor",
            ["CRU"] = "Seraphim Station",
            ["S2"] = "Seraphim Station",
            ["ARC"] = "Baijini Point",
            ["S3"] = "Baijini Point",
            ["MIC"] = "Port Tressler",
            ["S4"] = "Port Tressler"
        };

    public static readonly IReadOnlyDictionary<string, string> Cities =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["NewBabbage"] = "New Babbage",
            ["Lorville"] = "Lorville",
            ["Orison"] = "Orison",
            ["Area18"] = "Area 18",
            ["Area061"] = "Area 061",
            ["Levski"] = "Levski"
        };

    public static readonly IReadOnlyDictionary<string, string> JumpPoints =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stan-pyro"] = "Stanton – Pyro Jump Point",
            ["pyro-stan"] = "Pyro – Stanton Jump Point",
            ["stan-terra"] = "Stanton – Terra Jump Point",
            ["terra-stan"] = "Terra – Stanton Jump Point",
            ["stan-nyx"] = "Stanton – Nyx Jump Point",
            ["nyx-stan"] = "Nyx – Stanton Jump Point",
            ["pyro-nyx"] = "Pyro – Nyx Jump Point",
            ["nyx-pyro"] = "Nyx – Pyro Jump Point",
            ["StantonPyro"] = "Stanton – Pyro Jump Point",
            ["PyroStanton"] = "Pyro – Stanton Jump Point",
            ["StantonTerra"] = "Stanton – Terra Jump Point",
            ["TerraStanton"] = "Terra – Stanton Jump Point",
            ["StantonNyx"] = "Stanton – Nyx Jump Point",
            ["NyxStanton"] = "Nyx – Stanton Jump Point"
        };

    public static readonly IReadOnlyDictionary<string, (string Name, string System, string? Body, StarmapObjectType Type, bool IsArmistice)> WellKnown =
        new Dictionary<string, (string, string, string?, StarmapObjectType, bool)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Nyx_Levski"] = ("Levski", "Nyx", "Delamar", StarmapObjectType.LandingZone, true),
            ["Delamar_Levski"] = ("Levski", "Nyx", "Delamar", StarmapObjectType.LandingZone, true),
            ["Levski"] = ("Levski", "Nyx", "Delamar", StarmapObjectType.LandingZone, true),
            ["GrimHEX"] = ("Grim HEX", "Stanton", "Yela", StarmapObjectType.SpaceStation, false),
            ["Grim HEX"] = ("Grim HEX", "Stanton", "Yela", StarmapObjectType.SpaceStation, false),
            ["Stanton2b_GrimHEX"] = ("Grim HEX", "Stanton", "Yela", StarmapObjectType.SpaceStation, false),
            ["Port_Tressler"] = ("Port Tressler", "Stanton", "microTech", StarmapObjectType.SpaceStation, true),
            ["Port Tressler"] = ("Port Tressler", "Stanton", "microTech", StarmapObjectType.SpaceStation, true),
            ["Seraphim_Station"] = ("Seraphim Station", "Stanton", "Crusader", StarmapObjectType.SpaceStation, true),
            ["Seraphim Station"] = ("Seraphim Station", "Stanton", "Crusader", StarmapObjectType.SpaceStation, true),
            ["Everus_Harbor"] = ("Everus Harbor", "Stanton", "Hurston", StarmapObjectType.SpaceStation, true),
            ["Everus Harbor"] = ("Everus Harbor", "Stanton", "Hurston", StarmapObjectType.SpaceStation, true),
            ["Baijini_Point"] = ("Baijini Point", "Stanton", "ArcCorp", StarmapObjectType.SpaceStation, true),
            ["Baijini Point"] = ("Baijini Point", "Stanton", "ArcCorp", StarmapObjectType.SpaceStation, true),
            ["Klescher"] = ("Klescher Automated Rehabilitation", "Stanton", "Aberdeen", StarmapObjectType.Outpost, false),
            ["Checkmate"] = ("Checkmate Station", "Pyro", "Monox", StarmapObjectType.SpaceStation, false),
            ["Orbituary"] = ("Orbituary", "Pyro", "Bloom", StarmapObjectType.SpaceStation, false),
            ["RuinStation"] = ("Ruin Station", "Pyro", "Terminus", StarmapObjectType.SpaceStation, false),
            ["Ruin Station"] = ("Ruin Station", "Pyro", "Terminus", StarmapObjectType.SpaceStation, false),
            ["SPK"] = ("Security Post Kareah", "Stanton", "Cellin", StarmapObjectType.SpaceStation, false),
            ["SecurityPostKareah"] = ("Security Post Kareah", "Stanton", "Cellin", StarmapObjectType.SpaceStation, false)
        };

    public static readonly IReadOnlyList<(string Token, StarmapObjectType Kind)> SiteKinds =
    [
        ("DistributionCentre", StarmapObjectType.Outpost),
        ("Distribution", StarmapObjectType.Outpost),
        ("MiningFacility", StarmapObjectType.Outpost),
        ("Mine", StarmapObjectType.Outpost),
        ("Rayari", StarmapObjectType.Outpost),
        ("Research", StarmapObjectType.Outpost),
        ("Hydro", StarmapObjectType.Outpost),
        ("Shubin", StarmapObjectType.Outpost),
        ("Farm", StarmapObjectType.Outpost),
        ("Scrap", StarmapObjectType.Outpost),
        ("Outpost", StarmapObjectType.Outpost),
        ("Station", StarmapObjectType.SpaceStation)
    ];

    public static readonly IReadOnlyDictionary<string, string> Operators =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RayariHydro"] = "Rayari",
            ["Covalex"] = "Covalex",
            ["SakuraSun"] = "Sakura Sun",
            ["ArcCorp"] = "ArcCorp",
            ["Shubin"] = "Shubin",
            ["MicroTech"] = "microTech",
            ["HDMS"] = "HDMS",
            ["Hurston"] = "Hurston Dynamics"
        };

    #endregion

    #region Resolution Logic

    public static ResolvedLocation ResolveLocation(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId) || rawId == "—")
            return new ResolvedLocation { RawCode = rawId ?? "", DisplayName = "—", SystemName = "Stanton", ParentBody = "—" };

        var id = CleanRawId(rawId);

        // 1. Sentinels / Unspezifische Filter
        if (IsAmbiguous(id))
            return new ResolvedLocation { RawCode = rawId, DisplayName = "Im Transit (Quantum-Route)", SystemName = "Stanton", ParentBody = "—" };

        // 2. Bekannte Orte (Well-Known)
        if (WellKnown.TryGetValue(id, out var wk))
        {
            return new ResolvedLocation
            {
                RawCode = rawId,
                DisplayName = wk.Name,
                SystemName = wk.System,
                ParentBody = wk.Body ?? wk.System,
                Type = wk.Type,
                IsArmistice = wk.IsArmistice
            };
        }

        // 3. Nyx Spezifisch
        var nyx = TryNyx(rawId, id);
        if (nyx != null) return nyx;

        // 4. Sprungtor Rest Stop
        var jpRs = TryJumpPointRestStop(rawId, id);
        if (jpRs != null) return jpRs;

        // 5. Reguläre Lagrange Rest Stops (RR_MIC_L1 etc.)
        var rs = TryRestStop(rawId, id);
        if (rs != null) return rs;

        // 6. Sprungtore (rs_ext_*)
        var jp = TryJumpPoint(rawId, id);
        if (jp != null) return jp;

        // 7. Externe Stationen (rs_ext_*)
        var rsExt = TryRsExt(rawId, id);
        if (rsExt != null) return rsExt;

        // 8. Planetare Outposts / Städte (Stanton4_NewBabbage etc.)
        var planet = TryPlanetary(rawId, id);
        if (planet != null) return planet;

        // 9. Orbitale Himmelskörper
        var orbit = TryOrbital(rawId, id);
        if (orbit != null) return orbit;

        // 10. Eingebettete System-Token (_Stanton1b_)
        var embed = TryEmbeddedSystem(rawId, id);
        if (embed != null) return embed;

        // 11. Städte / LZ Token (NewBabbage, Lorville, Orison, Area18)
        var city = TryWellKnownCity(rawId, id);
        if (city != null) return city;

        // Fallback
        var pretty = Prettify(id);
        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = pretty,
            SystemName = "Stanton",
            ParentBody = "Stanton",
            Type = StarmapObjectType.Outpost,
            IsArmistice = true
        };
    }

    /// <summary>Einfacher String-Fallback für bestehende Aufrufe Locations.Resolve(code).</summary>
    public static string Resolve(string code)
    {
        var res = ResolveLocation(code);
        return res.DisplayName;
    }

    private static string CleanRawId(string raw)
    {
        var id = raw.Trim();
        if (id.EndsWith(".socpak", StringComparison.OrdinalIgnoreCase))
            id = id[..^".socpak".Length];

        id = GuidSuffixRegex.Replace(id, string.Empty);
        id = id.TrimStart('\'').Trim();

        if (id.StartsWith("LOC_", StringComparison.OrdinalIgnoreCase))
            id = id[4..];
        if (id.StartsWith("OOC_", StringComparison.OrdinalIgnoreCase))
            id = id[4..];

        id = id.Replace("_objectContainer", string.Empty, StringComparison.OrdinalIgnoreCase)
               .Replace("ObjectContainer_", string.Empty, StringComparison.OrdinalIgnoreCase)
               .Replace("_LOC", string.Empty, StringComparison.OrdinalIgnoreCase);

        return id;
    }

    private static bool IsAmbiguous(string id)
    {
        return id.Equals("INVALID_LOCATION_ID", StringComparison.OrdinalIgnoreCase)
            || id.Equals("ObjectContainer_RestStop", StringComparison.OrdinalIgnoreCase)
            || id.Equals("RestStop", StringComparison.OrdinalIgnoreCase)
            || NavPointRegex.IsMatch(id)
            || MissionBeaconRegex.IsMatch(id);
    }

    private static ResolvedLocation? TryNyx(string rawId, string id)
    {
        if (!id.StartsWith("Nyx", StringComparison.OrdinalIgnoreCase) && !NyxGatewayRegex.IsMatch(id))
            return null;

        var body = id.Contains("Glaciem", StringComparison.OrdinalIgnoreCase) ? "Glaciem Ring"
            : id.Contains("Keeger", StringComparison.OrdinalIgnoreCase) ? "Keeger Belt"
            : id.Contains("Levski", StringComparison.OrdinalIgnoreCase) ? "Delamar"
            : "Delamar";

        var name = id.Contains("SocialStation_003", StringComparison.OrdinalIgnoreCase) ? "People's Service Station Theta"
            : id.Contains("RockCracker_007", StringComparison.OrdinalIgnoreCase) ? "QV Breaker Station BRK-267"
            : id.Contains("OutlawStation_Keeger", StringComparison.OrdinalIgnoreCase) || id.Contains("Keeger", StringComparison.OrdinalIgnoreCase) ? "Moraine Base (Keeger Depot)"
            : id.Contains("Levski", StringComparison.OrdinalIgnoreCase) ? "Levski"
            : Spaced(id);

        var type = id.Contains("Levski", StringComparison.OrdinalIgnoreCase) ? StarmapObjectType.LandingZone
            : id.Contains("Station", StringComparison.OrdinalIgnoreCase) ? StarmapObjectType.SpaceStation
            : StarmapObjectType.Outpost;

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = name,
            SystemName = "Nyx",
            ParentBody = body,
            Type = type,
            IsArmistice = true
        };
    }

    private static ResolvedLocation? TryJumpPointRestStop(string rawId, string id)
    {
        var m = RestStopJumpPointRegex.Match(id);
        if (!m.Success) return null;

        var route = m.Groups["route"].Value;
        var name = JumpPoints.TryGetValue(route, out var known) ? $"{known} Station" : $"{Spaced(route)} Jump Point Station";
        var sys = route.StartsWith("Pyro", StringComparison.OrdinalIgnoreCase) ? "Pyro" : "Stanton";

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = name,
            SystemName = sys,
            ParentBody = sys,
            Type = StarmapObjectType.JumpPoint,
            IsArmistice = true
        };
    }

    private static ResolvedLocation? TryRestStop(string rawId, string id)
    {
        var m = RestStopRegex.Match(id);
        if (!m.Success) return null;

        var bodyToken = m.Groups["body"].Value;
        var slot = m.Groups["slot"].Value.ToUpperInvariant();

        if (slot == "LEO" && LeoStations.TryGetValue(bodyToken, out var leoStation))
        {
            var pBody = RestStopBodies.TryGetValue(bodyToken, out var orb) ? orb.Body : bodyToken;
            var sys = RestStopBodies.TryGetValue(bodyToken, out var orbSys) ? orbSys.System : "Stanton";
            return new ResolvedLocation
            {
                RawCode = rawId,
                DisplayName = leoStation,
                SystemName = sys,
                ParentBody = pBody,
                Type = StarmapObjectType.SpaceStation,
                IsArmistice = true
            };
        }

        if (RestStopBodies.TryGetValue(bodyToken, out var body))
        {
            var name = $"{bodyToken.ToUpperInvariant()}-{slot} Station";
            return new ResolvedLocation
            {
                RawCode = rawId,
                DisplayName = name,
                SystemName = body.System,
                ParentBody = body.Body,
                Type = StarmapObjectType.LagrangeStation,
                IsArmistice = true
            };
        }

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = $"{bodyToken} {slot} Station",
            SystemName = "Stanton",
            ParentBody = bodyToken,
            Type = StarmapObjectType.LagrangeStation,
            IsArmistice = true
        };
    }

    private static ResolvedLocation? TryJumpPoint(string rawId, string id)
    {
        var m = JumpPointRegex.Match(id);
        if (!m.Success) return null;

        var key = $"{m.Groups["from"].Value}-{m.Groups["to"].Value}";
        var name = JumpPoints.TryGetValue(key, out var known) ? known : $"{Title(m.Groups["from"].Value)} – {Title(m.Groups["to"].Value)} Jump Point";
        var sys = m.Groups["from"].Value.Contains("pyro", StringComparison.OrdinalIgnoreCase) ? "Pyro" : "Stanton";

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = name,
            SystemName = sys,
            ParentBody = sys,
            Type = StarmapObjectType.JumpPoint,
            IsArmistice = true
        };
    }

    private static ResolvedLocation? TryRsExt(string rawId, string id)
    {
        var m = RsExtRegex.Match(id);
        if (!m.Success) return null;

        var bodyToken = m.Groups["body"].Value;
        var slot = m.Groups["slot"].Value.ToUpperInvariant();

        if (slot.StartsWith("LEO", StringComparison.OrdinalIgnoreCase) && LeoStations.TryGetValue(bodyToken, out var leo))
        {
            var pBody = RestStopBodies.TryGetValue(bodyToken, out var orb) ? orb.Body : bodyToken;
            var sys = RestStopBodies.TryGetValue(bodyToken, out var orbSys) ? orbSys.System : "Stanton";
            return new ResolvedLocation
            {
                RawCode = rawId,
                DisplayName = leo,
                SystemName = sys,
                ParentBody = pBody,
                Type = StarmapObjectType.SpaceStation,
                IsArmistice = true
            };
        }

        if (RestStopBodies.TryGetValue(bodyToken, out var body))
        {
            return new ResolvedLocation
            {
                RawCode = rawId,
                DisplayName = $"{body.Body} {slot} Station",
                SystemName = body.System,
                ParentBody = body.Body,
                Type = StarmapObjectType.LagrangeStation,
                IsArmistice = true
            };
        }

        return null;
    }

    private static ResolvedLocation? TryPlanetary(string rawId, string id)
    {
        var m = PlanetaryRegex.Match(id);
        if (!m.Success) return null;

        var systemToken = m.Groups["system"].Value;
        var bodyToken = m.Groups["body"].Value;
        var rest = m.Groups["rest"].Value;

        var isPyro = systemToken.Equals("Pyro", StringComparison.OrdinalIgnoreCase);
        var sys = isPyro ? "Pyro" : "Stanton";
        var bodies = isPyro ? PyroBodies : StantonBodies;

        var resolvedBody = bodies.TryGetValue(bodyToken, out var b) ? b : null;
        var (name, kind) = DescribeSite(rest, resolvedBody);

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = name,
            SystemName = sys,
            ParentBody = resolvedBody ?? sys,
            Type = kind,
            IsArmistice = !isPyro && !name.Contains("Grim HEX") && !name.Contains("Rehabilitation")
        };
    }

    private static ResolvedLocation? TryOrbital(string rawId, string id)
    {
        var m = OrbitalRegex.Match(id);
        if (!m.Success) return null;

        var systemToken = m.Groups["system"].Value;
        var bodyToken = m.Groups["body"].Value;
        var isPyro = systemToken.Equals("Pyro", StringComparison.OrdinalIgnoreCase);
        var bodies = isPyro ? PyroBodies : StantonBodies;

        var resolvedBody = bodies.TryGetValue(bodyToken, out var b) ? b : Title(m.Groups["name"].Value);
        var type = bodyToken.Length > 1 ? StarmapObjectType.Moon : StarmapObjectType.Planet;

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = resolvedBody,
            SystemName = isPyro ? "Pyro" : "Stanton",
            ParentBody = isPyro ? "Pyro" : "Stanton",
            Type = type,
            IsArmistice = true
        };
    }

    private static ResolvedLocation? TryEmbeddedSystem(string rawId, string id)
    {
        var m = EmbeddedSystemRegex.Match(id);
        if (!m.Success) return null;

        var systemToken = m.Groups["system"].Value;
        var bodyToken = m.Groups["body"].Value;
        var isPyro = systemToken.Equals("Pyro", StringComparison.OrdinalIgnoreCase);
        var bodies = isPyro ? PyroBodies : StantonBodies;

        var body = bodies.TryGetValue(bodyToken, out var b) ? b : null;
        var rest = EmbeddedSystemRegex.Replace(id, "_").Trim('_');
        var (name, kind) = DescribeSite(rest, body);

        return new ResolvedLocation
        {
            RawCode = rawId,
            DisplayName = name,
            SystemName = isPyro ? "Pyro" : "Stanton",
            ParentBody = body ?? (isPyro ? "Pyro" : "Stanton"),
            Type = kind,
            IsArmistice = true
        };
    }

    private static ResolvedLocation? TryWellKnownCity(string rawId, string id)
    {
        var trimmed = id.Replace("_City", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (Cities.TryGetValue(trimmed, out var city))
        {
            var pBody = trimmed switch
            {
                "NewBabbage" => "microTech",
                "Lorville" => "Hurston",
                "Orison" => "Crusader",
                "Area18" or "Area061" => "ArcCorp",
                "Levski" => "Delamar",
                _ => "Stanton"
            };
            var sys = trimmed == "Levski" ? "Nyx" : "Stanton";
            return new ResolvedLocation
            {
                RawCode = rawId,
                DisplayName = city,
                SystemName = sys,
                ParentBody = pBody,
                Type = StarmapObjectType.LandingZone,
                IsArmistice = true
            };
        }
        return null;
    }

    private static (string Name, StarmapObjectType Kind) DescribeSite(string rest, string? body)
    {
        if (Cities.TryGetValue(rest, out var city))
            return (city, StarmapObjectType.LandingZone);

        var parts = rest.Split('_', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (Cities.TryGetValue(part, out var namedCity))
                return (namedCity, StarmapObjectType.LandingZone);
        }

        if (rest.Contains("DistributionCentre", StringComparison.OrdinalIgnoreCase) || rest.Contains("Distribution", StringComparison.OrdinalIgnoreCase))
        {
            var op = parts.Select(p => Operators.GetValueOrDefault(p)).FirstOrDefault(o => o is not null);
            var siteName = parts.LastOrDefault(p => !p.Equals("DistributionCentre", StringComparison.OrdinalIgnoreCase) && !p.Equals("Hurston", StringComparison.OrdinalIgnoreCase) && !p.Equals("MicroTech", StringComparison.OrdinalIgnoreCase));
            return (siteName != null ? $"Verteilzentrum {siteName}" : $"{op ?? "Verteilzentrum"} Distribution Centre", StarmapObjectType.Outpost);
        }

        if (rest.Contains("Rayari", StringComparison.OrdinalIgnoreCase))
        {
            var site = parts.LastOrDefault(p => !p.Equals("RayariHydro", StringComparison.OrdinalIgnoreCase) && !p.Equals("Rayari", StringComparison.OrdinalIgnoreCase)) ?? "Research";
            return ($"Rayari {Title(site)}", StarmapObjectType.Outpost);
        }

        if (rest.Contains("HDMS", StringComparison.OrdinalIgnoreCase) || rest.Contains("HurdynMining", StringComparison.OrdinalIgnoreCase))
        {
            var site = parts.LastOrDefault() ?? "Mining";
            return (site.StartsWith("HDMS", StringComparison.OrdinalIgnoreCase) ? site : $"HDMS-{site}", StarmapObjectType.Outpost);
        }

        if (rest.Contains("Shubin", StringComparison.OrdinalIgnoreCase))
        {
            var site = parts.LastOrDefault() ?? "Mining";
            return ($"Shubin {site}", StarmapObjectType.Outpost);
        }

        // Wikelo Sammler
        if (rest.Contains("TheCollectorsAsteriod", StringComparison.OrdinalIgnoreCase))
        {
            return ("Wikelo Sammler", StarmapObjectType.Outpost);
        }

        // Fallback über SiteKinds
        foreach (var (token, kind) in SiteKinds)
        {
            if (rest.Contains(token, StringComparison.OrdinalIgnoreCase))
                return (Spaced(rest), kind);
        }

        return (Spaced(rest), StarmapObjectType.Outpost);
    }

    private static string Title(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static string Spaced(string value) =>
        CamelBoundaryRegex.Replace(value.Replace('_', ' '), " ").Replace("  ", " ").Trim();

    private static string Prettify(string code)
    {
        var s = code.Replace('_', ' ').Trim();
        return s.Length == 0 ? code : s;
    }

    #endregion

    #region Generated Regular Expressions

    [GeneratedRegex(@"^(?:RR_JP_)(?<route>\w+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RestStopJumpPointRegex { get; }

    [GeneratedRegex(@"^RR_(?<body>[A-Za-z0-9]+)_(?<slot>L\d|LEO|L\d[A-Za-z]?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RestStopRegex { get; }

    [GeneratedRegex(@"^rs_ext_(?<from>[a-z]+)-(?<to>[a-z]+)_jp\d*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex JumpPointRegex { get; }

    [GeneratedRegex(@"^rs_ext_(?<body>[a-z]+\d*)[-_](?<slot>l\d|leo\d?|l\d[a-z]?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RsExtRegex { get; }

    [GeneratedRegex(@"^(?<system>Stanton|Pyro)(?<body>\d[a-z]?)_(?<rest>.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PlanetaryRegex { get; }

    [GeneratedRegex(@"^(?<system>Stanton|Pyro)_(?<body>\d[a-z]?)_(?<name>\w+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex OrbitalRegex { get; }

    [GeneratedRegex(@"_(?<system>Stanton|Pyro)(?<body>\d[a-z]?)(?=_|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmbeddedSystemRegex { get; }

    [GeneratedRegex(@"^(?:Stanton|Pyro)_(?:Nyx_JPStation|JumpPoint_Nyx)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NyxGatewayRegex { get; }

    [GeneratedRegex(@"^NavPoint_\w+_\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NavPointRegex { get; }

    [GeneratedRegex(@"^MISSION_QT_\w*Beacon_\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MissionBeaconRegex { get; }

    [GeneratedRegex(@"_?\{[0-9A-Fa-f-]{36}\}", RegexOptions.Compiled)]
    private static partial Regex GuidSuffixRegex { get; }

    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled)]
    private static partial Regex CamelBoundaryRegex { get; }

    #endregion
}
