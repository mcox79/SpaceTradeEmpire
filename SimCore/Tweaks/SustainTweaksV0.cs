namespace SimCore.Tweaks;

// GATE.S7.SUSTAIN.FUEL_DEDUCT.001: Fleet sustain cost constants.
public static class SustainTweaksV0
{
    // Fuel consumed per tick while fleet is moving (from dedicated fuel tank).
    public const int FuelPerMoveTick = 1;

    // Module sustain cycle: every N ticks, each equipped module consumes 1 unit of its sustain good.
    // 360 ticks ≈ 1 game day at default tick rate.
    public const int SustainCycleTicks = 360;

    // NPC fleets consume fuel at this multiplier of the player rate.
    // 0.5 = NPCs burn fuel at half player rate (softer economy impact).
    public const float NpcFuelRateMultiplier = 0.5f;

    // Default fuel capacity for fleets without a ship class lookup.
    public const int DefaultFuelCapacity = 500;

    // GATE.S19.ONBOARD.FUEL_COST.001: Credit cost per unit of fuel when refueling.
    // 1 cr/unit = gentle pressure (~500cr per full refuel on a 500-fuel Corvette).
    public const int RefuelCreditCostPerUnit = 1;

    // Credit cost per hull HP restored when docked at a station.
    // 2 cr/HP = repairing 50 HP damage costs 100 cr.
    public const int HullRepairCreditCostPerHp = 2;

    // Fuel tank module capacity bonuses.
    public const int FuelTankMk1Capacity = 150;
    public const int FuelTankMk2Capacity = 350;
}
