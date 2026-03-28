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
    // GATE.T64.ECON.FRICTION_SINKS.001: Raised from 3→5 to increase sink/faucet ratio.
    // 5 cr/unit = ~2500cr per full refuel on a 500-fuel Corvette.
    public const int RefuelCreditCostPerUnit = 5;

    // Credit cost per hull HP restored when docked at a station.
    // GATE.T65.ECON.SINK_BOOST.001: Raised from 8→12 to push sink_faucet ≥ 0.35.
    // 12 cr/HP = repairing 50 HP damage costs 600 cr.
    public const int HullRepairCreditCostPerHp = 12;

    // Fuel tank module capacity bonuses.
    public const int FuelTankMk1Capacity = 150;
    public const int FuelTankMk2Capacity = 350;
}
