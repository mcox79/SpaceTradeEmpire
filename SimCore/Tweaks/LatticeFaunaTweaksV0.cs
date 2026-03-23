namespace SimCore.Tweaks;

// GATE.T45.DEEP_DREAD.FAUNA_TWEAKS.001: Lattice Fauna tuning constants.
// Emergent computational processes on degrading Thread Lattice infrastructure.
// NOT biological — interference patterns that behave like predators.
public static class LatticeFaunaTweaksV0
{
    // Minimum instability phase for fauna spawning (3 = Fracture).
    public const int SpawnMinPhase = 3;

    // Detection radius: hops from player's fracture signature source.
    public const int DetectionRadiusHops = 3;

    // Fracture signature decay: ticks after last fracture jump before signature fades.
    public const int SignatureDecayTicks = 60;

    // Arrival delay: ticks between detection and fauna arrival at node.
    public const int ArrivalDelayTicks = 30;

    // Instrument interference: basis points added to InstrumentDisagreement magnitude.
    public const int InterferenceMagnitudeBps = 3000; // +30% disagreement

    // Fuel drain per tick while fauna is at player's node.
    public const int FuelDrainPerTick = 1;

    // Route uncertainty increase while fauna present (BPS added).
    public const int RouteUncertaintyBps = 2000;

    // Going dark: ticks player must stay stationary (no fracture drive) to lose fauna.
    public const int GoDarkTicks = 15;

    // Residue: ticks after fauna departs that the node remains "marked" (attracts more fauna).
    public const int ResidueDurationTicks = 100;

    // Max concurrent fauna entities in the simulation.
    public const int MaxConcurrent = 3;

    // Spawn cooldown: minimum ticks between fauna spawn attempts.
    public const int SpawnCooldownTicks = 40;

    // Fauna check interval (ticks between spawn evaluations).
    public const int CheckIntervalTicks = 10;
}
