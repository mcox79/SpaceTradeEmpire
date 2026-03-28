namespace SimCore.Tweaks;

/// <summary>
/// Combat depth v2 numeric constants (GATE.S7.COMBAT_DEPTH2.TRACKING.001).
/// Tracking, evasion, damage variance, armor penetration, fore soft-kill.
/// </summary>
public static class CombatDepthTweaksV0
{
    // ── GATE.S7.COMBAT_DEPTH2.TRACKING.001: Tracking/Evasion hit probability ──
    // Hit probability = clamp(TrackingBps - EvasionBps, MinHitBps, MaxHitBps) / 10000
    // Higher tracking = more accurate, higher evasion = harder to hit.
    public const int DefaultTrackingBps = 10000;   // 100% base tracking (backward-compatible default)
    public const int DefaultEvasionBps = 0;        // 0% base evasion (backward-compatible default)
    public const int MinHitBps = 2000;             // Floor: 20% hit chance minimum
    public const int MaxHitBps = 10000;            // Ceiling: 100% hit chance maximum

    // Per-ship-class evasion overrides (lighter = more evasive).
    public const int ShuttleEvasionBps = 4000;     // 40% evasion
    public const int ClipperEvasionBps = 3500;     // 35% evasion
    public const int CorvetteEvasionBps = 2500;    // 25% evasion
    public const int FrigateEvasionBps = 2000;     // 20% evasion
    public const int CruiserEvasionBps = 1500;     // 15% evasion
    public const int HaulerEvasionBps = 1000;      // 10% evasion
    public const int CarrierEvasionBps = 1200;     // 12% evasion
    public const int DreadnoughtEvasionBps = 800;  // 8% evasion

    // Per-weapon tracking overrides (heavier weapons track slower).
    public const int LaserTrackingBps = 9500;      // 95% — fast beam, high tracking
    public const int CannonTrackingBps = 7000;     // 70% — ballistic, moderate tracking
    public const int MissileTrackingBps = 8500;    // 85% — guided munition
    public const int TorpedoTrackingBps = 5000;    // 50% — slow, heavy
    public const int PointDefenseTrackingBps = 9000; // 90% — designed for interception
    public const int SpinalTrackingBps = 4000;     // 40% — massive, slow-tracking

    // ── GATE.S7.COMBAT_DEPTH2.DAMAGE_VAR.001: Damage variance ──
    // ±VarianceRangeBps / 10000 applied to base damage after hit check.
    public const int VarianceRangeBps = 2000;      // ±20% damage variance

    // ── GATE.S7.COMBAT_DEPTH2.ARMOR_PEN.001: Armor penetration ──
    // Fraction of damage that bypasses zone armor directly to hull (bps).
    public const int DefaultArmorPenBps = 0;       // No penetration by default
    public const int LaserArmorPenBps = 500;       // 5% — energy diffuses on armor
    public const int CannonArmorPenBps = 2000;     // 20% — kinetic punch-through
    public const int MissileArmorPenBps = 1500;    // 15% — shaped charge
    public const int TorpedoArmorPenBps = 3500;    // 35% — heavy warhead
    public const int PointDefenseArmorPenBps = 0;  // 0% — small caliber
    public const int SpinalArmorPenBps = 5000;     // 50% — devastating penetration

    // ── GATE.S7.COMBAT_DEPTH2.FORE_KILL.001: Fore zone soft-kill ──
    // When fore zone armor HP <= 0, weapons mapped to fore slots produce 0 damage.
    // This threshold is always 0 (depleted). No tweak needed — logic only.

    // ── GATE.T64.COMBAT.SEED_FLOOR.001: Minimum player damage per combat round ──
    // Ensures even unarmed/poorly-equipped players deal at least this much damage per round
    // against tier-1 NPCs. Prevents 0% kill rate on unlucky seeds (seed 77777: 0/12 kills).
    public const int MinPlayerDamageFloor = 5;
}
