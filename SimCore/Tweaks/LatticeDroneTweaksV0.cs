namespace SimCore.Tweaks;

/// <summary>
/// Lattice drone spawn/combat constants (GATE.S8.LATTICE_DRONES.ENTITY.001).
/// Drones spawn near void sites in instability phase 2+, become hostile at phase 3.
/// </summary>
public static class LatticeDroneTweaksV0
{
    // ── Spawn thresholds (linked to instability phase index from InstabilityTweaksV0.GetPhaseIndex) ──
    public const int SpawnPhaseMin = 2;            // Drift phase — territorial drones appear
    public const int HostilePhaseMin = 3;          // Fracture phase — drones attack on sight
    public const int DespawnPhaseMax = 4;           // Void phase — drones absent (space too unstable)

    // ── Spawn timing ──
    public const int SpawnCheckIntervalTicks = 10; // Check for spawns every N ticks
    public const int MaxDronesPerNode = 3;         // Maximum drones at a single node
    public const int RespawnCooldownTicks = 50;    // Ticks before destroyed drone respawns
    public const int WarningGraceTicks = 1;        // Territorial drones warn before attacking
    public const int EngagementCooldownTicks = 30; // Ticks between drone re-engagements (prevent per-tick combat)

    // ── Drone fleet stats ──
    public const int DroneHullHp = 40;
    public const int DroneShieldHp = 20;
    public const int DroneWeaponBaseDamage = 8;
    public const int DroneTrackingBps = 8500;      // High tracking — small precise weapons
    public const int DroneEvasionBps = 3000;       // Moderate evasion — small fast craft
    public const int DroneArmorPenBps = 1000;      // 10% pen
    public const int DroneHeatCapacity = 500;
    public const int DroneRejectionRate = 200;
    public const int DroneHeatPerShot = 80;

    // ── Drone ship class ──
    public const string DroneShipClassId = "lattice_drone";
    public const string DroneWeaponModuleId = "weapon_lattice_pulse";
    public const string DroneFleetIdPrefix = "ld_";
}
