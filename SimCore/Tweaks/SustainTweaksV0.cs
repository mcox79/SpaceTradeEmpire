namespace SimCore.Tweaks;

// GATE.S7.SUSTAIN.FUEL_DEDUCT.001: Fleet sustain cost constants.
public static class SustainTweaksV0
{
    // Fuel consumed per tick while fleet is moving (from cargo "fuel" good).
    public const int FuelPerMoveTick = 1;

    // Module sustain cycle: every N ticks, each equipped module consumes 1 unit of its sustain good.
    // 360 ticks ≈ 1 game day at default tick rate.
    public const int SustainCycleTicks = 360;

    // NPC fleets consume fuel at this multiplier of the player rate.
    // 0.5 = NPCs burn fuel at half player rate (softer economy impact).
    public const float NpcFuelRateMultiplier = 0.5f;
}
