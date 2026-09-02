using System;
using System.Collections.Generic;
using System.Linq;

namespace SCLogMate.Core;

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
        // Drake Interplanetary (#2DD4BF)
        ["Clipper"] = new("Clipper · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Leichter Frachter & Starter", 1_850_000, 65, "LTI (Lifetime)"),
        ["Cutter"] = new("Cutter · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Starter / Expedition", 950_000, 45, "LTI (Lifetime)"),
        ["Cutter Rambler"] = new("Cutter Rambler · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Expedition & Erkundung", 1_100_000, 50, "LTI (Lifetime)"),
        ["Cutter Scout"] = new("Cutter Scout · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Aufklärung & Radar", 1_050_000, 50, "LTI (Lifetime)"),
        ["Cutlass Black"] = new("Cutlass Black · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Mehrzweck / Gunship", 2_150_000, 110, "120 Monate (IAE)"),
        ["Cutlass Red"] = new("Cutlass Red · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Medizinisches Rettungsschiff", 2_850_000, 135, "120 Monate (IAE)"),
        ["Cutlass Blue"] = new("Cutlass Blue · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Polizei & Gefangenentransport", 3_450_000, 175, "120 Monate (IAE)"),
        ["Cutlass Steel"] = new("Cutlass Steel · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Truppentransporter & Gunship", 4_100_000, 260, "120 Monate (IAE)"),
        ["Corsair"] = new("Corsair · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Schwere Erkundung & Gunship", 6_500_000, 250, "LTI (Lifetime)"),
        ["Vulture"] = new("Vulture · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Leichter Bergungsfrachter (Salvage)", 2_600_000, 175, "LTI (Lifetime)"),
        ["Caterpillar"] = new("Caterpillar · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Modularer Schwerlastfrachter", 12_400_000, 330, "120 Monate (IAE)"),
        ["Buccaneer"] = new("Buccaneer · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Leichter Abfangjäger", 1_650_000, 110, "120 Monate (IAE)"),
        ["Herald"] = new("Herald · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Datenkurier & Informationsläufer", 1_450_000, 85, "120 Monate (IAE)"),
        ["Mule"] = new("Mule · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Boden-Frachtlader", 250_000, 45, "LTI (Lifetime)"),
        ["Dragonfly"] = new("Dragonfly · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Open-Canopy Grav-Bike", 180_000, 40, "LTI (Lifetime)"),
        ["Ironclad"] = new("Ironclad · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Schwerer Panzer-Frachter", 28_000_000, 450, "LTI (Lifetime)"),
        ["Ironclad Assault"] = new("Ironclad Assault · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Gepanzerte Landungsfestung", 32_000_000, 535, "LTI (Lifetime)"),
        ["Kraken"] = new("Kraken · Drake", "Drake Interplanetary", "DRAKE", "#2DD4BF", "Fliegender Flugzeugträger", 120_000_000, 1650, "LTI (Lifetime)"),

        // Aegis Dynamics (#F87171)
        ["Avenger Titan"] = new("Avenger Titan · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Leichter Mehrzweck-Frachter", 1_250_000, 60, "120 Monate (IAE)"),
        ["Avenger Stalker"] = new("Avenger Stalker · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Kopfgeldjagd & Arrestzellen", 1_350_000, 65, "120 Monate (IAE)"),
        ["Avenger Warlock"] = new("Avenger Warlock · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "EMP-Störsender", 1_650_000, 85, "120 Monate (IAE)"),
        ["Gladius"] = new("Gladius · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Leichter Raumüberlegenheitsjäger", 2_340_000, 90, "120 Monate (IAE)"),
        ["Gladius Valiant"] = new("Gladius Valiant · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Spezial-Dogfighter", 2_650_000, 110, "120 Monate (IAE)"),
        ["Sabre"] = new("Sabre · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Tarnkappen-Jäger", 3_250_000, 170, "120 Monate (IAE)"),
        ["Sabre Comet"] = new("Sabre Comet · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Tarnkappen-Dogfighter", 3_500_000, 185, "120 Monate (IAE)"),
        ["Sabre Raven"] = new("Sabre Raven · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Tarn-Aufklärer & EMP", 4_500_000, 200, "LTI (Lifetime)"),
        ["Sabre Firebird"] = new("Sabre Firebird · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Raketen-Abfangjäger", 3_600_000, 185, "LTI (Lifetime)"),
        ["Sabre Peregrine"] = new("Sabre Peregrine · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Renn-Tarnjäger", 3_500_000, 185, "LTI (Lifetime)"),
        ["Vanguard Warden"] = new("Vanguard Warden · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schwerer Langstreckenjäger", 4_850_000, 260, "120 Monate (IAE)"),
        ["Vanguard Sentinel"] = new("Vanguard Sentinel · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Elektronische Kampfführung", 5_250_000, 275, "120 Monate (IAE)"),
        ["Vanguard Harbinger"] = new("Vanguard Harbinger · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schwerer Bomber", 5_750_000, 290, "120 Monate (IAE)"),
        ["Vanguard Hoplite"] = new("Vanguard Hoplite · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Truppentransporter", 4_500_000, 240, "120 Monate (IAE)"),
        ["Eclipse"] = new("Eclipse · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Tarnkappen-Torpedobomber", 6_250_000, 300, "120 Monate (IAE)"),
        ["Retaliator"] = new("Retaliator · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schwerer Torpedobomber", 8_500_000, 275, "120 Monate (IAE)"),
        ["Retaliator Base"] = new("Retaliator Base · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Modularer Schwerlast-Bomber", 5_200_000, 175, "120 Monate (IAE)"),
        ["Hammerhead"] = new("Hammerhead · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Schweres Flak-Kanonenboot", 24_500_000, 725, "LTI (Lifetime)"),
        ["Reclaimer"] = new("Reclaimer · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Industrielles Bergungsschiff", 28_000_000, 400, "LTI (Lifetime)"),
        ["Redeemer"] = new("Redeemer · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Gepanzertes Gunship", 9_800_000, 330, "120 Monate (IAE)"),
        ["Vulcan"] = new("Vulcan · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Reparatur, Betankung & Re-Arm", 5_800_000, 200, "LTI (Lifetime)"),
        ["Nautilus"] = new("Nautilus · Aegis", "Aegis Dynamics", "AEGIS", "#F87171", "Minenleger & Abfangkreuzer", 26_000_000, 725, "LTI (Lifetime)"),

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
        ["Intrepid"] = new("Intrepid · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Starter Frachter", 1_400_000, 65, "LTI (Lifetime)"),
        ["Genesis Starliner"] = new("Genesis Starliner · Crusader", "Crusader Industries", "CRUSADER", "#38BDF8", "Passagier-Linienflieger", 16_000_000, 400, "LTI (Lifetime)"),

        // Anvil Aerospace (#FB923C)
        ["Arrow"] = new("Arrow · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Agiler Abfangjäger", 1_450_000, 75, "120 Monate (IAE)"),
        ["Pisces C8X"] = new("Pisces C8X · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Expeditions-Shuttle", 850_000, 45, "LTI (Lifetime)"),
        ["Pisces C8R"] = new("Pisces C8R · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Notfall-Rettungsshuttle", 1_150_000, 65, "LTI (Lifetime)"),
        ["C8 Pisces"] = new("C8 Pisces · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Standard Shuttle", 750_000, 45, "120 Monate (IAE)"),
        ["Hornet F7C"] = new("Hornet F7C · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Mittlerer Raumkampffighter", 2_450_000, 110, "120 Monate (IAE)"),
        ["Hornet F7C-M"] = new("Super Hornet · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Schwerer 2-Sitzer Kampffighter", 3_150_000, 180, "120 Monate (IAE)"),
        ["Hornet F7C-R"] = new("Hornet Tracker · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Radar & Aufklärungsjäger", 2_850_000, 140, "120 Monate (IAE)"),
        ["Hornet F7C-S"] = new("Hornet Ghost · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Tarnkappen-Jäger", 2_750_000, 125, "120 Monate (IAE)"),
        ["Hornet F7C Mk II"] = new("F7C Mk II · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Next-Gen Mittlerer Jäger", 3_400_000, 175, "LTI (Lifetime)"),
        ["F8C Lightning"] = new("F8C Lightning · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Elite-Schwerjäger", 15_000_000, 300, "LTI (Lifetime)"),
        ["Gladiator"] = new("Gladiator · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Leichter Bomber", 2_850_000, 165, "120 Monate (IAE)"),
        ["Hawk"] = new("Hawk · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Kopfgeldjagd & EMP", 2_100_000, 100, "120 Monate (IAE)"),
        ["Hurricane"] = new("Hurricane · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Turm-Schwerjäger", 3_650_000, 210, "120 Monate (IAE)"),
        ["Terrapin"] = new("Terrapin · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Gepanzerte Aufklärung", 4_500_000, 220, "120 Monate (IAE)"),
        ["Valkyrie"] = new("Valkyrie · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Truppentransporter & Gunship", 9_500_000, 375, "120 Monate (IAE)"),
        ["Carrack"] = new("Carrack · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Militärisches Erkundungsschiff", 45_000_000, 600, "LTI (Lifetime)"),
        ["Crucible"] = new("Crucible · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Fliegende Raumschiff-Werkstatt", 14_000_000, 350, "LTI (Lifetime)"),
        ["Liberator"] = new("Liberator · Anvil", "Anvil Aerospace", "ANVIL", "#FB923C", "Militärischer Fahrzeugträger", 22_000_000, 575, "LTI (Lifetime)"),

        // RSI - Roberts Space Industries (#60A5FA)
        ["Aurora MR"] = new("Aurora MR · RSI", "RSI", "RSI", "#60A5FA", "Starter / Leichter Transporter", 450_000, 30, "6 Monate"),
        ["Aurora LN"] = new("Aurora LN · RSI", "RSI", "RSI", "#60A5FA", "Kampf-Starter", 650_000, 40, "6 Monate"),
        ["Aurora CL"] = new("Aurora CL · RSI", "RSI", "RSI", "#60A5FA", "Fracht-Starter", 720_000, 45, "6 Monate"),
        ["Aurora LX"] = new("Aurora LX · RSI", "RSI", "RSI", "#60A5FA", "Luxus-Starter", 550_000, 35, "6 Monate"),
        ["Aurora ES"] = new("Aurora ES · RSI", "RSI", "RSI", "#60A5FA", "Basis-Starter", 350_000, 20, "6 Monate"),
        ["Constellation Andromeda"] = new("Constellation Andromeda · RSI", "RSI", "RSI", "#60A5FA", "Mehrzweck-Kanonenboot", 6_850_000, 240, "120 Monate (IAE)"),
        ["Constellation Taurus"] = new("Constellation Taurus · RSI", "RSI", "RSI", "#60A5FA", "Mittlerer Frachttransporter (174 SCU)", 5_750_000, 200, "120 Monate (IAE)"),
        ["Constellation Aquila"] = new("Constellation Aquila · RSI", "RSI", "RSI", "#60A5FA", "Erkundung & Scanner", 7_450_000, 315, "120 Monate (IAE)"),
        ["Constellation Phoenix"] = new("Constellation Phoenix · RSI", "RSI", "RSI", "#60A5FA", "Luxus-Transporter", 9_200_000, 350, "120 Monate (IAE)"),
        ["Scorpius"] = new("Scorpius · RSI", "RSI", "RSI", "#60A5FA", "Schwerer 2-Sitzer Jäger", 4_250_000, 240, "LTI (Lifetime)"),
        ["Scorpius Antares"] = new("Scorpius Antares · RSI", "RSI", "RSI", "#60A5FA", "EMP & Quantum-Jammer", 4_850_000, 230, "LTI (Lifetime)"),
        ["Mantis"] = new("Mantis · RSI", "RSI", "RSI", "#60A5FA", "Quantum Interdiction & Snare", 3_800_000, 150, "120 Monate (IAE)"),
        ["Zeus Mk II CL"] = new("Zeus Mk II CL · RSI", "RSI", "RSI", "#60A5FA", "Mittlerer Frachter (128 SCU)", 4_800_000, 175, "LTI (Lifetime)"),
        ["Zeus Mk II ES"] = new("Zeus Mk II ES · RSI", "RSI", "RSI", "#60A5FA", "Erkundung & Radar", 4_600_000, 175, "LTI (Lifetime)"),
        ["Zeus Mk II MR"] = new("Zeus Mk II MR · RSI", "RSI", "RSI", "#60A5FA", "Kopfgeldjagd & EMP", 5_200_000, 190, "LTI (Lifetime)"),
        ["Apollo Triage"] = new("Apollo Triage · RSI", "RSI", "RSI", "#60A5FA", "Fliegende Klinik & Rettung", 6_200_000, 250, "LTI (Lifetime)"),
        ["Apollo Medivac"] = new("Apollo Medivac · RSI", "RSI", "RSI", "#60A5FA", "Gepanzerte Notfallklinik", 6_800_000, 275, "LTI (Lifetime)"),
        ["Galaxy"] = new("Galaxy · RSI", "RSI", "RSI", "#60A5FA", "Modularer Großraum-Kreuzer", 18_000_000, 380, "LTI (Lifetime)"),
        ["Perseus"] = new("Perseus · RSI", "RSI", "RSI", "#60A5FA", "Schweres Kanonenboot (S7)", 25_000_000, 675, "LTI (Lifetime)"),
        ["Arrastra"] = new("Arrastra · RSI", "RSI", "RSI", "#60A5FA", "Industrieller Tiefen-Bergbaukreuzer", 22_000_000, 575, "LTI (Lifetime)"),
        ["Polaris"] = new("Polaris · RSI", "RSI", "RSI", "#60A5FA", "Korvette & Torpedo-Flaggschiff", 55_000_000, 750, "LTI (Lifetime)"),

        // MISC - Musashi Industrial & Starflight Concern (#A78BFA)
        ["Prospector"] = new("Prospector · MISC", "MISC", "MISC", "#A78BFA", "Industrieller Bergbaulaser (Mining)", 2_850_000, 155, "120 Monate (IAE)"),
        ["Freelancer"] = new("Freelancer · MISC", "MISC", "MISC", "#A78BFA", "Mittlerer Frachttransporter", 2_450_000, 110, "120 Monate (IAE)"),
        ["Freelancer MAX"] = new("Freelancer MAX · MISC", "MISC", "MISC", "#A78BFA", "Erweiterter Frachttransporter (120 SCU)", 3_650_000, 150, "120 Monate (IAE)"),
        ["Freelancer DUR"] = new("Freelancer DUR · MISC", "MISC", "MISC", "#A78BFA", "Langstrecken-Erkundung", 3_100_000, 135, "120 Monate (IAE)"),
        ["Freelancer MIS"] = new("Freelancer MIS · MISC", "MISC", "MISC", "#A78BFA", "Schweres Raketen-Gunship", 3_900_000, 175, "120 Monate (IAE)"),
        ["Hull A"] = new("Hull A · MISC", "MISC", "MISC", "#A78BFA", "Modularer Zubringerfrachter (64 SCU)", 1_850_000, 90, "LTI (Lifetime)"),
        ["Hull B"] = new("Hull B · MISC", "MISC", "MISC", "#A78BFA", "Mittlerer Frachter (384 SCU)", 3_800_000, 140, "LTI (Lifetime)"),
        ["Hull C"] = new("Hull C · MISC", "MISC", "MISC", "#A78BFA", "Interstellarer Großfrachter (4608 SCU)", 35_000_000, 500, "LTI (Lifetime)"),
        ["Hull D"] = new("Hull D · MISC", "MISC", "MISC", "#A78BFA", "Schwerer Großraumfrachter", 48_000_000, 550, "LTI (Lifetime)"),
        ["Hull E"] = new("Hull E · MISC", "MISC", "MISC", "#A78BFA", "Gigantischer Mega-Frachter", 75_000_000, 750, "LTI (Lifetime)"),
        ["Reliant Kore"] = new("Reliant Kore · MISC", "MISC", "MISC", "#A78BFA", "Leichter Starter-Frachter", 1_150_000, 65, "120 Monate (IAE)"),
        ["Reliant Tana"] = new("Reliant Tana · MISC", "MISC", "MISC", "#A78BFA", "Leichter Scharmützel-Jäger", 1_350_000, 75, "120 Monate (IAE)"),
        ["Reliant Sen"] = new("Reliant Sen · MISC", "MISC", "MISC", "#A78BFA", "Forschung & Wissenschaft", 1_450_000, 85, "120 Monate (IAE)"),
        ["Reliant Mako"] = new("Reliant Mako · MISC", "MISC", "MISC", "#A78BFA", "Nachrichten & Berichterstattung", 1_550_000, 95, "120 Monate (IAE)"),
        ["Starfarer"] = new("Starfarer · MISC", "MISC", "MISC", "#A78BFA", "Betankung & Treibstoff-Raffinerie", 14_500_000, 300, "120 Monate (IAE)"),
        ["Starfarer Gemini"] = new("Starfarer Gemini · MISC", "MISC", "MISC", "#A78BFA", "Militärischer Tanker & Raketenschiff", 17_500_000, 340, "120 Monate (IAE)"),
        ["Expanse"] = new("Expanse · MISC", "MISC", "MISC", "#A78BFA", "Mobile Erz-Raffinerie", 4_200_000, 150, "LTI (Lifetime)"),
        ["Odyssey"] = new("Odyssey · MISC", "MISC", "MISC", "#A78BFA", "Autonomes Erkundungsschiff & Hangar", 38_000_000, 700, "LTI (Lifetime)"),

        // Origin Jumpworks (#E2E8F0)
        ["100i"] = new("100i · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Touring Starter", 1_100_000, 50, "LTI (Lifetime)"),
        ["125a"] = new("125a · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Leichter Kampffighter", 1_350_000, 60, "LTI (Lifetime)"),
        ["135c"] = new("135c · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Leichter Frachter", 1_450_000, 65, "LTI (Lifetime)"),
        ["300i"] = new("300i · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Luxus-Touring", 1_450_000, 60, "120 Monate (IAE)"),
        ["315p"] = new("315p · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Erkundung & Traktorstrahl", 1_650_000, 65, "120 Monate (IAE)"),
        ["325a"] = new("325a · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Leichter Kampffighter", 1_750_000, 70, "120 Monate (IAE)"),
        ["350r"] = new("350r · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Rennflieger", 2_450_000, 125, "120 Monate (IAE)"),
        ["400i"] = new("400i · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Luxus-Erkundungsschiff", 7_800_000, 250, "LTI (Lifetime)"),
        ["600i Explorer"] = new("600i Explorer · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Großraum-Erkundung & Rover", 18_500_000, 475, "120 Monate (IAE)"),
        ["600i Touring"] = new("600i Touring · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Luxus-Passagierkreuzer", 16_500_000, 435, "120 Monate (IAE)"),
        ["890 Jump"] = new("890 Jump · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Flaggschiff-Superyacht", 65_000_000, 950, "LTI (Lifetime)"),
        ["85X"] = new("85X · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "2-Sitzer Luxus-Runabout", 850_000, 50, "120 Monate (IAE)"),
        ["M50"] = new("M50 · Origin", "Origin Jumpworks", "ORIGIN", "#E2E8F0", "Rennflieger & Abfangjäger", 2_100_000, 100, "120 Monate (IAE)"),

        // Argo Astronautics (#F59E0B)
        ["MOLE"] = new("MOLE · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Mehrpersonen-Bergbauschiff (Mining)", 8_500_000, 315, "120 Monate (IAE)"),
        ["RAFT"] = new("RAFT · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Schwerer Frachtcontainer-Transporter", 3_450_000, 125, "LTI (Lifetime)"),
        ["SRV"] = new("SRV · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Schlepper & Traktorstrahl-Rettung", 3_850_000, 165, "LTI (Lifetime)"),
        ["MPUV Cargo"] = new("MPUV Cargo · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Frachtshuttle", 450_000, 35, "LTI (Lifetime)"),
        ["MPUV Personnel"] = new("MPUV Personnel · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Personenshuttle", 450_000, 35, "LTI (Lifetime)"),
        ["MPUV Tractor"] = new("MPUV Tractor · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Traktorstrahl-Shuttle", 550_000, 40, "LTI (Lifetime)"),
        ["CSV-SM"] = new("CSV-SM · Argo", "Argo Astronautics", "ARGO", "#F59E0B", "Boden-Frachtfahrzeug", 250_000, 45, "LTI (Lifetime)"),

        // Mirai (#38BDF8)
        ["Fury"] = new("Fury · Mirai", "Mirai", "MIRAI", "#38BDF8", "Snub-Kampffighter", 1_250_000, 55, "LTI (Lifetime)"),
        ["Fury MX"] = new("Fury MX · Mirai", "Mirai", "MIRAI", "#38BDF8", "Raketen-Snubfighter", 1_450_000, 55, "LTI (Lifetime)"),
        ["Fury LX"] = new("Fury LX · Mirai", "Mirai", "MIRAI", "#38BDF8", "Renn-Snubfighter", 1_350_000, 55, "LTI (Lifetime)"),
        ["Pulse"] = new("Pulse · Mirai", "Mirai", "MIRAI", "#38BDF8", "Hoverbike / Grav-Bike", 150_000, 30, "LTI (Lifetime)"),
        ["Pulse LX"] = new("Pulse LX · Mirai", "Mirai", "MIRAI", "#38BDF8", "Renn-Grav-Bike", 150_000, 30, "LTI (Lifetime)"),

        // Alien & Spezial (Aopoa, Banu, Esperia, Gatac)
        ["BMM"] = new("Banu Merchantman · Banu", "Banu", "BANU", "#10B981", "Flaggschiff-Basar & Großfrachter", 45_000_000, 650, "LTI (Lifetime)"),
        ["Merchantman"] = new("Banu Merchantman · Banu", "Banu", "BANU", "#10B981", "Flaggschiff-Basar & Großfrachter", 45_000_000, 650, "LTI (Lifetime)"),
        ["Defender"] = new("Banu Defender · Banu", "Banu", "BANU", "#10B981", "Eskort-Dogfighter", 4_200_000, 220, "LTI (Lifetime)"),
        ["Syulen"] = new("Syulen · Gatac", "Gatac Manufacture", "GATAC", "#8B5CF6", "Alien-Starter & Frachter", 1_850_000, 70, "LTI (Lifetime)"),
        ["Railen"] = new("Railen · Gatac", "Gatac Manufacture", "GATAC", "#8B5CF6", "Grav-Container Frachter", 5_800_000, 225, "LTI (Lifetime)"),
        ["San'tok.yāi"] = new("San'tok.yāi · Aopoa", "Aopoa", "AOPOA", "#EC4899", "Mittlerer Alien-Dogfighter", 5_200_000, 240, "LTI (Lifetime)"),
        ["Khartu-al"] = new("Khartu-al · Aopoa", "Aopoa", "AOPOA", "#EC4899", "Leichter Alien-Aufklärer", 3_200_000, 170, "120 Monate (IAE)"),
        ["Talon"] = new("Talon · Esperia", "Esperia", "ESPERIA", "#EF4444", "Tevarin Tarnkappen-Dogfighter", 2_600_000, 115, "120 Monate (IAE)"),
        ["Talon Shrike"] = new("Talon Shrike · Esperia", "Esperia", "ESPERIA", "#EF4444", "Tevarin Raketen-Dogfighter", 2_750_000, 115, "120 Monate (IAE)"),
        ["Prowler"] = new("Prowler · Esperia", "Esperia", "ESPERIA", "#EF4444", "Gepanzerter Tevarin Dropship", 9_500_000, 440, "LTI (Lifetime)"),
        ["Blade"] = new("Blade · Esperia", "Esperia", "ESPERIA", "#EF4444", "Vanduul Leichter Jäger", 5_200_000, 275, "120 Monate (IAE)"),
        ["Glaive"] = new("Glaive · Esperia", "Esperia", "ESPERIA", "#EF4444", "Vanduul Mittlerer Jäger (Dual S5)", 7_500_000, 350, "120 Monate (IAE)"),
        ["Scythe"] = new("Scythe · Esperia", "Esperia", "ESPERIA", "#EF4444", "Original Vanduul Abfangjäger", 8_500_000, 300, "LTI (Lifetime)"),
        ["M80"] = new("M80", "Aegis Dynamics", "AEGIS", "#F87171", "Elite-Schiff", 15_000_000, 300, "LTI (Lifetime)")
    };

    public static ShipCatalogEntry Lookup(string shipName)
    {
        if (string.IsNullOrWhiteSpace(shipName))
            return new("Unbekannt", "Unbekannt", "SC", "#58A6FF", "Raumschiff", 1_500_000, 50, "6 Monate");

        var clean = shipName.Trim();

        // 1. Exakter & Teilstring-Abgleich
        foreach (var (key, val) in Catalog)
        {
            if (clean.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                clean.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(clean, StringComparison.OrdinalIgnoreCase))
            {
                return val;
            }
        }

        // 2. Hersteller ableiten aus Name
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
