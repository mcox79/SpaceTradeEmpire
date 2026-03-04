namespace SimCore.Tweaks;

/// <summary>
/// Combat numeric constants v0 (GATE.S5.COMBAT_LOCAL.DAMAGE_MODEL.001).
/// All combat-related magic numbers routed through here for tweak guard compliance.
/// </summary>
public static class CombatTweaksV0
{
    // Default HP values for hero ship
    public const int DefaultHullHpMax = 100;
    public const int DefaultShieldHpMax = 50;

    // Default HP values for AI/pirate fleets
    public const int AiHullHpMax = 80;
    public const int AiShieldHpMax = 30;

    // Counter family multipliers (fixed-point: 150 = 1.5x, 50 = 0.5x, 100 = 1.0x)
    // Stored as int percentages to keep deterministic (no float).
    public const int KineticVsHullPct = 150;
    public const int KineticVsShieldPct = 50;
    public const int EnergyVsHullPct = 50;
    public const int EnergyVsShieldPct = 150;
    public const int NeutralPct = 100;

    // Weapon base damage defaults (used when content registry doesn't specify)
    public const int DefaultWeaponBaseDamage = 10;
}
