using System;
using System.Text.RegularExpressions;
using SCLogMate.Models;

namespace SCLogMate.Core;

public record ItemArmorMeta(
    string ArmorClass,
    int DamageReductionPercent,
    int TempMinCelsius,
    int TempMaxCelsius,
    string BadgeColor
);

public static class LoadoutCatalog
{
    public static (string armorClass, int dmgRed, string tempRange, string badgeColor, string attachments, string capacity) GetItemMeta(LoadoutSlotType slot, string rawClass, string cleanName)
    {
        var lower = (rawClass + " " + cleanName).ToLowerInvariant();

        // 1. Rüstungsteile (Helm, Torso, Arme, Beine)
        if (slot is LoadoutSlotType.Helmet or LoadoutSlotType.Torso or LoadoutSlotType.Arms or LoadoutSlotType.Legs)
        {
            if (lower.Contains("heavy") || lower.Contains("_h_") || lower.Contains("defiance") || lower.Contains("citadel") || lower.Contains("morozov") || lower.Contains("novikov") || lower.Contains("pembroke"))
            {
                var isHazmat = lower.Contains("novikov") || lower.Contains("pembroke");
                var minTemp = isHazmat ? -225 : -75;
                var maxTemp = isHazmat ? 225 : 95;
                return ("Schwer (Heavy)", 40, $"{minTemp}°C bis +{maxTemp}°C", "#F59E0B", "", "");
            }
            if (lower.Contains("medium") || lower.Contains("_m_") || lower.Contains("inquisitor") || lower.Contains("lynx") || lower.Contains("macflex") || lower.Contains("orca") || lower.Contains("dustup"))
            {
                return ("Mittel (Medium)", 30, "-50°C bis +75°C", "#38BDF8", "", "");
            }
            if (lower.Contains("light") || lower.Contains("_l_") || lower.Contains("truedefense") || lower.Contains("calva") || lower.Contains("avent") || lower.Contains("stitcher") || lower.Contains("carnifex"))
            {
                return ("Leicht (Light)", 20, "-35°C bis +60°C", "#34D399", "", "");
            }
            // Standard Rüstung
            return ("Mittel (Medium)", 30, "-40°C bis +65°C", "#38BDF8", "", "");
        }

        // 2. Undersuit / Fliegeranzug
        if (slot == LoadoutSlotType.Undersuit)
        {
            if (lower.Contains("novikov") || lower.Contains("pembroke"))
                return ("Spezialanzug (Hazmat)", 10, "-225°C bis +225°C", "#F59E0B", "", "10k µSCU");
            return ("Fliegeranzug (Flightsuit)", 0, "-40°C bis +60°C", "#A78BFA", "", "5k µSCU");
        }

        // 3. Rucksack
        if (slot == LoadoutSlotType.Backpack)
        {
            if (lower.Contains("heavy") || lower.Contains("morozov") || lower.Contains("novikov") || lower.Contains("pembroke"))
                return ("Großer Rucksack", 0, "—", "#F59E0B", "", "60k µSCU");
            if (lower.Contains("medium") || lower.Contains("macflex"))
                return ("Mittlerer Rucksack", 0, "—", "#38BDF8", "", "40k µSCU");
            return ("Leichter Rucksack", 0, "—", "#34D399", "", "25k µSCU");
        }

        // 4. Waffen & Aufsätze
        if (slot is LoadoutSlotType.Primary1 or LoadoutSlotType.Primary2 or LoadoutSlotType.Sidearm)
        {
            string attachments = "Standard-Visier";
            if (lower.Contains("sniper") || lower.Contains("p6lr") || lower.Contains("arrowhead") || lower.Contains("atls"))
                attachments = "🔭 4x-8x Teleskop-Optik · 🔇 Schalldämpfer";
            else if (lower.Contains("shotgun") || lower.Contains("devastator") || lower.Contains("br2"))
                attachments = "🔴 Holographisches Red-Dot · 🔦 Taktisches Licht";
            else if (lower.Contains("smg") || lower.Contains("custodian") || lower.Contains("p8sc") || lower.Contains("c54"))
                attachments = "🔍 1x Reflex-Visier · 🔦 Laser-Modul · 🔋 Magazin";
            else if (lower.Contains("lmg") || lower.Contains("f55") || lower.Contains("fs9") || lower.Contains("demeco"))
                attachments = "🔭 2x Holo-Visier · ⚙ Kompensator · 🔋 Trommelmagazin";
            else if (lower.Contains("rifle") || lower.Contains("karna") || lower.Contains("s71") || lower.Contains("p4ar") || lower.Contains("gallant"))
                attachments = "🔭 2x-3x Optik · 🔇 Schalldämpfer · 🔦 Laser";
            else if (slot == LoadoutSlotType.Sidearm)
                attachments = "🔴 1x Pistolen-Rotpunkt · 🔋 Standard-Magazin";

            return ("Feuerwaffe", 0, "—", "#EF4444", attachments, "");
        }

        // 5. Tools & Meds
        if (slot == LoadoutSlotType.MultiTool)
            return ("Werkzeug", 0, "—", "#FBBF24", "🧲 Traktorstrahl-Aufsatz", "");

        if (slot == LoadoutSlotType.MedItem)
            return ("Medizinisch", 0, "—", "#EC4899", "💉 Hemozal / Resuscitation", "");

        return ("Ausrüstung", 0, "—", "#8B949E", "", "");
    }
}
