using System;
using System.Collections.Generic;

namespace SCLogReader.Core;

public static class LootValuation
{
    public static long EstimateItemValue(string itemName, string rawClass)
    {
        if (string.IsNullOrWhiteSpace(itemName) && string.IsNullOrWhiteSpace(rawClass)) return 0;
        var text = (itemName + " " + rawClass).ToLowerInvariant();

        // 1. Schwere Rüstungen / Boss Gear
        if (text.Contains("morozov") || text.Contains("citadel") || text.Contains("defiance") || text.Contains("novikov") || text.Contains("pembroke"))
        {
            if (text.Contains("core") || text.Contains("torso")) return 14500;
            if (text.Contains("helmet") || text.Contains("helm")) return 8200;
            if (text.Contains("backpack") || text.Contains("pack")) return 12000;
            return 6500;
        }

        // 2. Mittlere / Leichte Rüstungen
        if (text.Contains("inquisitor") || text.Contains("orca") || text.Contains("lynx") || text.Contains("truedefense") || text.Contains("calva") || text.Contains("stitcher"))
        {
            if (text.Contains("core") || text.Contains("torso")) return 8500;
            if (text.Contains("helmet") || text.Contains("helm")) return 5200;
            if (text.Contains("backpack") || text.Contains("pack")) return 7500;
            return 4200;
        }

        // 3. Sniper / Heavy Weapons / Railgun / Missile Launcher
        if (text.Contains("animus") || text.Contains("scourge") || text.Contains("railgun") || text.Contains("fs9") || text.Contains("f55") || text.Contains("p6lr") || text.Contains("arrowhead"))
        {
            return 18500;
        }

        // 4. Rifles / Shotguns / SMGs
        if (text.Contains("karna") || text.Contains("custodian") || text.Contains("p8sc") || text.Contains("p4ar") || text.Contains("s71") || text.Contains("c54") || text.Contains("devastator") || text.Contains("br2"))
        {
            return 7200;
        }

        // 5. Pistolen / Sidearms
        if (text.Contains("lh86") || text.Contains("arclight") || text.Contains("coda") || text.Contains("yubarev") || text.Contains("salvo"))
        {
            return 3200;
        }

        // 6. Tools & Medguns
        if (text.Contains("multitool") || text.Contains("pyro_ryt") || text.Contains("medgun") || text.Contains("paramed"))
        {
            return 2800;
        }

        // 7. Medpens / Utility
        if (text.Contains("medpen") || text.Contains("oxypen") || text.Contains("hemozal"))
        {
            return 450;
        }

        return 1500;
    }
}
