using System;
using System.Collections.Generic;
using System.Linq;

namespace SCLogReader.Core;

public record ShipCatalogEntry(
    string NormalizedName,
    string Manufacturer,
    string ManufacturerBadge,
    string ManufacturerColor,
    string Role,
    long EstimatedValueAuec,
    int PledgeValueUsd = 0,
    string DefaultInsurance = "LTI (Lifetime)"
);

public static class FleetCatalog
{
    public static IEnumerable<ShipCatalogEntry> AllShips => Catalog.Values.OrderBy(c => c.NormalizedName);

    private static readonly Dictionary<string, ShipCatalogEntry> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Clipper"] = new("Clipper · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Leichter Frachter & Starter", 1_850_000, 65, "LTI (Lifetime)"),
        ["Cutter"] = new("Cutter · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Starter / Expedition", 950_000, 45, "LTI (Lifetime)"),
        ["Cutter Rambler"] = new("Cutter Rambler · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Expedition & Erkundung", 1_100_000, 50, "LTI (Lifetime)"),
        ["Cutter Scout"] = new("Cutter Scout · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Aufklärung & Radar", 1_050_000, 50, "LTI (Lifetime)"),
        ["Cutlass Black"] = new("Cutlass Black · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Mehrzweck / Gunship", 2_150_000, 110, "120 Monate (IAE)"),
        ["Cutlass Red"] = new("Cutlass Red · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Medizinisches Rettungsschiff", 2_850_000, 135, "120 Monate (IAE)"),
        ["Cutlass Blue"] = new("Cutlass Blue · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Polizei & Gefangenentransport", 3_450_000, 175, "120 Monate (IAE)"),
        ["Corsair"] = new("Corsair · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Schwere Erkundung & Gunship", 6_500_000, 250, "LTI (Lifetime)"),
        ["Vulture"] = new("Vulture · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Leichter Bergungsfrachter (Salvage)", 2_600_000, 175, "LTI (Lifetime)"),
        ["Caterpillar"] = new("Caterpillar · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Modularer Schwerlastfrachter", 12_400_000, 330, "120 Monate (IAE)"),
        ["Buccaneer"] = new("Buccaneer · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Leichter Abfangjäger", 1_650_000, 110, "120 Monate (IAE)"),
        ["Herald"] = new("Herald · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Datenkurier & Informationsläufer", 1_450_000, 85, "120 Monate (IAE)"),

        // Aegis Dynamics (#F87171)
        ["Avenger Titan"] = new("Avenger Titan · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Leichter Mehrzweck-Frachter", 1_250_000, 60, "120 Monate (IAE)"),
        ["Avenger Stalker"] = new("Avenger Stalker · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Kopfgeldjagd & Arrestzellen", 1_350_000, 65, "120 Monate (IAE)"),
        ["Avenger Warlock"] = new("Avenger Warlock · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "EMP-Störsender", 1_650_000, 85, "120 Monate (IAE)"),
        ["Gladius"] = new("Gladius · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Leichter Raumüberlegenheitsjäger", 2_340_000, 90, "120 Monate (IAE)"),
        ["Sabre"] = new("Sabre · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Tarnkappen-Jäger", 3_250_000, 170, "120 Monate (IAE)"),
        ["Vanguard Warden"] = new("Vanguard Warden · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schwerer Langstreckenjäger", 4_850_000, 260, "120 Monate (IAE)"),
        ["Vanguard Sentinel"] = new("Vanguard Sentinel · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Elektronische Kampfführung", 5_250_000, 275, "120 Monate (IAE)"),
        ["Vanguard Harbinger"] = new("Vanguard Harbinger · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schwerer Bomber", 5_750_000, 290, "120 Monate (IAE)"),
        ["Eclipse"] = new("Eclipse · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Tarnkappen-Torpedobomber", 6_250_000, 300, "120 Monate (IAE)"),
        ["Retaliator"] = new("Retaliator · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schwerer Torpedobomber", 8_500_000, 275, "120 Monate (IAE)"),
        ["Hammerhead"] = new("Hammerhead · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schweres Flak-Kanonenboot", 24_500_000, 725, "LTI (Lifetime)"),
        ["Reclaimer"] = new("Reclaimer · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Industrielles Bergungsschiff", 28_000_000, 400, "LTI (Lifetime)"),
        ["Redeemer"] = new("Redeemer · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Gepanzertes Gunship", 9_800_000, 325, "120 Monate (IAE)"),

        // Crusader Industries (#38BDF8)
        ["C1 Spirit"] = new("C1 Spirit · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Mittlerer Frachttransporter", 3_150_000, 125, "LTI (Lifetime)"),
        ["A1 Spirit"] = new("A1 Spirit · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Präzisions-Bomber", 3_650_000, 175, "LTI (Lifetime)"),
        ["E1 Spirit"] = new("E1 Spirit · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "VIP-Personentransport", 3_450_000, 150, "LTI (Lifetime)"),
        ["Mercury Star Runner"] = new("Mercury Star Runner · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Daten- & Kurierfrachter", 7_950_000, 260, "120 Monate (IAE)"),
        ["MSR"] = new("Mercury Star Runner · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Daten- & Kurierfrachter", 7_950_000, 260, "120 Monate (IAE)"),
        ["C2 Hercules"] = new("C2 Hercules · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Schwerer Großraum-Frachter", 19_800_000, 400, "120 Monate (IAE)"),
        ["M2 Hercules"] = new("M2 Hercules · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Militärischer Fahrzeug- & Panzertransporter", 22_500_000, 520, "120 Monate (IAE)"),
        ["A2 Hercules"] = new("A2 Hercules · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Fliegende Festung & Bombardement", 29_500_000, 750, "LTI (Lifetime)"),
        ["Ares Inferno"] = new("Ares Inferno · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Schwerer Ballistik-Jäger (S7)", 4_500_000, 250, "120 Monate (IAE)"),
        ["Ares Ion"] = new("Ares Ion · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Schwerer Laser-Jäger (S7)", 4_500_000, 250, "120 Monate (IAE)"),

        // Anvil Aerospace (#FB923C)
        ["Arrow"] = new("Arrow · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Agiler Abfangjäger", 1_450_000, 75, "120 Monate (IAE)"),
        ["Pisces C8X"] = new("Pisces C8X · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Expeditions-Shuttle", 850_000, 45, "LTI (Lifetime)"),
        ["Pisces C8R"] = new("Pisces C8R · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Notfall-Rettungsshuttle", 1_150_000, 65, "LTI (Lifetime)"),
        ["Hornet F7C"] = new("Hornet F7C · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Mittlerer Raumkampffighter", 2_450_000, 110, "120 Monate (IAE)"),
        ["Hornet F7C-M"] = new("Super Hornet · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Schwerer 2-Sitzer Kampffighter", 3_150_000, 180, "120 Monate (IAE)"),
        ["F8C Lightning"] = new("F8C Lightning · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Elite-Schwerjäger", 15_000_000, 300, "LTI (Lifetime)"),
        ["Gladiator"] = new("Gladiator · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Leichter Bomber", 2_850_000, 165, "120 Monate (IAE)"),
        ["Hurricane"] = new("Hurricane · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Turm-Schwerjäger", 3_650_000, 210, "120 Monate (IAE)"),
        ["Valkyrie"] = new("Valkyrie · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Truppentransporter & Gunship", 9_500_000, 375, "120 Monate (IAE)"),
        ["Carrack"] = new("Carrack · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Militärisches Erkundungsschiff", 45_000_000, 600, "LTI (Lifetime)"),

        // RSI - Roberts Space Industries (#60A5FA)
        ["Aurora MR"] = new("Aurora MR · RSI", "RSI", "RSI", "#60A5FA", "Starter / Leichter Transporter", 450_000, 30, "6 Monate"),
        ["Aurora LN"] = new("Aurora LN · RSI", "RSI", "RSI", "#60A5FA", "Kampf-Starter", 650_000, 40, "6 Monate"),
        ["Constellation Andromeda"] = new("Constellation Andromeda · RSI", "RSI", "RSI", "#60A5FA", "Mehrzweck-Kanonenboot", 6_850_000, 240, "120 Monate (IAE)"),
        ["Constellation Taurus"] = new("Constellation Taurus · RSI", "RSI", "RSI", "#60A5FA", "Mittlerer Frachttransporter", 5_750_000, 200, "120 Monate (IAE)"),
        ["Constellation Aquila"] = new("Constellation Aquila · RSI", "RSI", "RSI", "#60A5FA", "Erkundung & Scanner", 7_450_000, 315, "120 Monate (IAE)"),
        ["Constellation Phoenix"] = new("Constellation Phoenix · RSI", "RSI", "RSI", "#60A5FA", "Luxus-Transporter", 9_200_000, 350, "120 Monate (IAE)"),
        ["Scorpius"] = new("Scorpius · RSI", "RSI", "RSI", "#60A5FA", "Schwerer 2-Sitzer Jäger", 4_250_000, 240, "LTI (Lifetime)"),
        ["Scorpius Antares"] = new("Scorpius Antares · RSI", "RSI", "RSI", "#60A5FA", "EMP & Quantum-Jammer", 4_850_000, 230, "LTI (Lifetime)"),
        ["Zeus Mk II CL"] = new("Zeus Mk II CL · RSI", "RSI", "RSI", "#60A5FA", "Mittlerer Frachter (128 SCU)", 4_800_000, 175, "LTI (Lifetime)"),
        ["Zeus Mk II ES"] = new("Zeus Mk II ES · RSI", "RSI", "RSI", "#60A5FA", "Erkundung & Radar", 4_600_000, 175, "LTI (Lifetime)"),

        // MISC - Musashi Industrial & Starflight Concern (#A78BFA)
        ["Prospector"] = new("Prospector · MISC", "MISC", "MISC", "#A78BFA", "Industrieller Bergbaulaser (Mining)", 2_850_000, 155, "120 Monate (IAE)"),
        ["Freelancer"] = new("Freelancer · MISC", "MISC", "MISC", "#A78BFA", "Mittlerer Frachttransporter", 2_450_000, 110, "120 Monate (IAE)"),
        ["Freelancer MAX"] = new("Freelancer MAX · MISC", "MISC", "MISC", "#A78BFA", "Erweiterter Frachttransporter (120 SCU)", 3_650_000, 150, "120 Monate (IAE)"),
        ["Hull A"] = new("Hull A · MISC", "MISC", "MISC", "#A78BFA", "Modularer Zubringerfrachter", 1_850_000, 90, "LTI (Lifetime)"),
        ["Hull C"] = new("Hull C · MISC", "MISC", "MISC", "#A78BFA", "Interstellarer Großfrachter (4608 SCU)", 35_000_000, 500, "LTI (Lifetime)"),
        ["Reliant Kore"] = new("Reliant Kore · MISC", "MISC", "MISC", "#A78BFA", "Leichter Starter-Frachter", 1_150_000, 65, "120 Monate (IAE)"),
        ["Starfarer"] = new("Starfarer · MISC", "MISC", "MISC", "#A78BFA", "Betankung & Treibstoff-Raffinerie", 14_500_000, 300, "120 Monate (IAE)"),

        // Origin Jumpworks (#E2E8F0)
        ["100i"] = new("100i · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Touring Starter", 1_100_000, 50, "LTI (Lifetime)"),
        ["300i"] = new("300i · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Luxus-Touring", 1_450_000, 60, "120 Monate (IAE)"),
        ["325a"] = new("325a · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Leichter Kampffighter", 1_750_000, 70, "120 Monate (IAE)"),
        ["400i"] = new("400i · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Luxus-Erkundungsschiff", 7_800_000, 250, "LTI (Lifetime)"),
        ["600i Explorer"] = new("600i Explorer · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Großraum-Erkundung", 18_500_000, 475, "120 Monate (IAE)"),
        ["890 Jump"] = new("890 Jump · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Flaggschiff-Superyacht", 65_000_000, 950, "LTI (Lifetime)"),

        // Argo Astronautics (#F59E0B)
        ["MOLE"] = new("MOLE · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Mehrpersonen-Bergbauschiff (Mining)", 8_500_000, 315, "120 Monate (IAE)"),
        ["RAFT"] = new("RAFT · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Schwerer Frachtcontainer-Transporter", 3_450_000, 125, "LTI (Lifetime)"),
        ["SRV"] = new("SRV · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Schlepper & Traktorstrahl-Rettung", 3_850_000, 165, "LTI (Lifetime)"),
        ["MPUV Cargo"] = new("MPUV Cargo · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Frachtshuttle", 450_000, 35, "LTI (Lifetime)"),

        // Mirai (#38BDF8)
        ["Fury"] = new("Fury · Mirai", "Mirai", "MIRAI", "#38BDF8", "Snub-Kampffighter", 1_250_000, 55, "LTI (Lifetime)"),
        ["Fury MX"] = new("Fury MX · Mirai", "Mirai", "MIRAI", "#38BDF8", "Raketen-Snubfighter", 1_450_000, 55, "LTI (Lifetime)"),
        ["Pulse"] = new("Pulse · Mirai", "Mirai", "MIRAI", "#38BDF8", "Hoverbike / Grav-Bike", 150_000, 30, "LTI (Lifetime)")
    };

    public static ShipCatalogEntry Lookup(string shipName)
    {
        if (string.IsNullOrWhiteSpace(shipName))
            return new("Unbekannt", "Unbekannt", "SC", "#58A6FF", "Raumschiff", 1_500_000, 50, "6 Monate");

        var clean = shipName.Trim();

        // Direkte Suche
        foreach (var (key, val) in Catalog)
        {
            if (clean.Contains(key, StringComparison.OrdinalIgnoreCase))
                return val;
        }

        // Hersteller ableiten aus Name
        string mfg = "Unbekannt";
        string badge = "SC";
        string color = "#58A6FF";
        string role = "Mehrzweck / Raumschiff";
        long price = 2_000_000;
        int usd = 80;

        if (clean.Contains("Drake", StringComparison.OrdinalIgnoreCase)) { mfg = "Drake Interplanetary"; badge = "DRAKE"; color = "#2DD4BF"; role = "Frachter & Gunship"; price = 2_500_000; usd = 100; }
        else if (clean.Contains("Aegis", StringComparison.OrdinalIgnoreCase)) { mfg = "Aegis Dynamics"; badge = "AEGIS"; color = "#F87171"; role = "Militär & Kampf"; price = 3_500_000; usd = 140; }
        else if (clean.Contains("Crusader", StringComparison.OrdinalIgnoreCase)) { mfg = "Crusader Industries"; badge = "CRUSADER"; color = "#38BDF8"; role = "Frachter & Transporter"; price = 5_000_000; usd = 180; }
        else if (clean.Contains("Anvil", StringComparison.OrdinalIgnoreCase)) { mfg = "Anvil Aerospace"; badge = "ANVIL"; color = "#FB923C"; role = "Raumüberlegenheit & Erkundung"; price = 3_000_000; usd = 120; }
        else if (clean.Contains("RSI", StringComparison.OrdinalIgnoreCase)) { mfg = "RSI"; badge = "RSI"; color = "#60A5FA"; role = "Mehrzweck & Kanonenboot"; price = 4_500_000; usd = 160; }
        else if (clean.Contains("MISC", StringComparison.OrdinalIgnoreCase)) { mfg = "MISC"; badge = "MISC"; color = "#A78BFA"; role = "Industrie & Mining"; price = 3_000_000; usd = 130; }
        else if (clean.Contains("Origin", StringComparison.OrdinalIgnoreCase)) { mfg = "Origin Jumpworks"; badge = "ORIGIN"; color = "#E2E8F0"; role = "Luxus & Touring"; price = 4_000_000; usd = 150; }
        else if (clean.Contains("Argo", StringComparison.OrdinalIgnoreCase)) { mfg = "Argo Astronautics"; badge = "ARGO"; color = "#F59E0B"; role = "Industrie & Bergung"; price = 2_800_000; usd = 110; }
        else if (clean.Contains("Mirai", StringComparison.OrdinalIgnoreCase)) { mfg = "Mirai"; badge = "MIRAI"; color = "#38BDF8"; role = "High-Tech Snub"; price = 1_200_000; usd = 50; }

        return new(clean, mfg, badge, color, role, price, usd, "120 Monate (IAE)");
    }
}
