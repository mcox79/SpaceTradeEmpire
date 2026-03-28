namespace SimCore.Tweaks;

// GATE.T61.SALVAGE.LOOT_TABLE.001: Salvage loot table constants.
// Drop rates and quantities scale with fleet role and equipment.
public static class SalvageTweaksV0
{
    // Base salvage credits by fleet role (minimal — salvage is things, not magic money).
    // Credits represent small valuables (data chips, nav tokens) found in wreckage.
    // The real value is in the material drops that the player sells at stations.
    public const int TraderCreditsMin = 5;
    public const int TraderCreditsMax = 25;
    public const int PatrolCreditsMin = 10;
    public const int PatrolCreditsMax = 50;
    // GATE.T63.COMBAT.LOOT_GUARANTEE.001: Raised from 0→5 so every kill has base credits.
    public const int HaulerCreditsMin = 5;
    public const int HaulerCreditsMax = 15;

    // Salvaged tech drops: quantity range by role.
    public const int TraderSalvageTechMin = 1;
    public const int TraderSalvageTechMax = 3;
    public const int PatrolSalvageTechMin = 2;
    public const int PatrolSalvageTechMax = 5;
    // GATE.T63.COMBAT.LOOT_GUARANTEE.001: Raised from 0→1 so every kill drops at least 1 tech.
    public const int HaulerSalvageTechMin = 1;
    public const int HaulerSalvageTechMax = 2;

    // Hull material salvage: metal/composites stripped from destroyed hull plating.
    public const int HullMetalMin = 1;
    public const int HullMetalMax = 4;
    public const int HullCompositesMin = 0;
    public const int HullCompositesMax = 2;

    // Equipment bonus: per installed module, add this many bps to credit drop.
    // 3 modules × 1000 bps = 30% bonus credits.
    public const int EquipmentBonusBpsPerModule = 1000;

    // Rarity escalation: chance (out of 10000) that salvage rarity upgrades per module.
    public const int RarityUpgradeBpsPerModule = 500;

    // Cargo spillage: if destroyed fleet had cargo, percentage that drops as loot.
    public const int CargoSpillPct = 50;
}
