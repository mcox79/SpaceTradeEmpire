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

    // GATE.S5.COMBAT.COUNTER_FAMILY.001: PointDefense weapon family constants
    // PointDefense does 2x damage against missile/torpedo targets; base damage vs all others.
    public const int PointDefenseBaseDamage = 8;
    public const int PointDefenseCounterMultiplierPct = 200; // 2x = 200%

    // GATE.S5.COMBAT.ESCORT_DOCTRINE.001: Escort doctrine shield bonus
    // Escorted fleet receives +25% shield damage reduction (expressed as pct: 25 means 25%).
    public const int EscortShieldDamageReductionPct = 25;

    // GATE.S18.SHIP_MODULES.ZONE_ARMOR.001: Default zone armor HP (Fore/Port/Stbd/Aft).
    public const int DefaultZoneArmorFore = 25;
    public const int DefaultZoneArmorPort = 20;
    public const int DefaultZoneArmorStbd = 20;
    public const int DefaultZoneArmorAft  = 15;

    public const int AiZoneArmorFore = 20;
    public const int AiZoneArmorPort = 15;
    public const int AiZoneArmorStbd = 15;
    public const int AiZoneArmorAft  = 10;

    // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Stance hit distribution (pct of hits per zone).
    // Charge: 50% Fore, 20% Port, 20% Stbd, 10% Aft
    public const int ChargeForePct = 50;
    public const int ChargePortPct = 20;
    public const int ChargeStbdPct = 20;
    public const int ChargeAftPct  = 10;

    // Broadside: 15% Fore, 35% Port, 35% Stbd, 15% Aft
    public const int BroadsideForePct = 15;
    public const int BroadsidePortPct = 35;
    public const int BroadsideStbdPct = 35;
    public const int BroadsideAftPct  = 15;

    // Kite: 10% Fore, 15% Port, 15% Stbd, 60% Aft (showing stern)
    public const int KiteForePct = 10;
    public const int KitePortPct = 15;
    public const int KiteStbdPct = 15;
    public const int KiteAftPct  = 60;

    // GATE.S5.COMBAT.STRATEGIC_RESOLVER.001: Strategic resolver max rounds cap.
    // Attrition resolver terminates at this round count and declares Draw if both fleets still alive.
    public const int StrategicMaxRounds = 50;

    // GATE.S5.COMBAT_RES.SYSTEM.001: Flee logic — minimum rounds before flee is possible.
    public const int FleeMinRounds = 3;
}
