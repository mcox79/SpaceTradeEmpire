namespace SimCore.Tweaks;

// GATE.X.FLEET_UPKEEP.DRAIN.001: Fleet upkeep cost constants.
public static class FleetUpkeepTweaksV0
{
    // Per-cycle upkeep cost in credits by ship class (index = class order in ShipClassContentV0).
    // Shuttle=2, Corvette=5, Clipper=4, Frigate=8, Hauler=6, Cruiser=15, Carrier=12, Dreadnought=50.
    public const int ShuttleUpkeep = 2;
    public const int CorvetteUpkeep = 5;
    public const int ClipperUpkeep = 4;
    public const int FrigateUpkeep = 8;
    public const int HaulerUpkeep = 6;
    public const int CruiserUpkeep = 15;
    public const int CarrierUpkeep = 12;
    public const int DreadnoughtUpkeep = 50;
    // Ancient hulls: high upkeep (exotic maintenance).
    public const int AncientBastionUpkeep = 30;
    public const int AncientSeekerUpkeep = 25;
    public const int AncientThresholdUpkeep = 35;

    // Docked multiplier in basis points (5000 = 50% = half cost when docked).
    public const int DockedMultiplierBps = 5000;

    // Upkeep cycle interval in ticks (same cadence as sustain cycle).
    public const int UpkeepCycleTicks = 60;

    // Default upkeep for unknown ship classes.
    public const int DefaultUpkeep = 5;

    // BPS divisor for multiplier math (10000 bps = 100%).
    public const int BpsDivisor = 10000;

    // GATE.X.FLEET_UPKEEP.DELINQUENCY.001: Grace period before module disable.
    public const int GracePeriodCycles = 3;

    // GATE.T48.TENSION.MAINTENANCE.001: Fuel consumption per cycle.
    // Calibrated so a shuttle (FuelCapacity ~20) lasts 50-80 ticks with FuelBurnCycleTicks=15, FuelPerCycleShuttle=1.
    public const int FuelBurnCycleTicks = 15;
    public const int FuelPerCycleShuttle = 1;
    public const int FuelPerCycleCorvette = 1;
    public const int FuelPerCycleClipper = 1;
    public const int FuelPerCycleFrigate = 2;
    public const int FuelPerCycleHauler = 2;
    public const int FuelPerCycleCruiser = 3;
    public const int FuelPerCycleCarrier = 3;
    public const int FuelPerCycleDreadnought = 5;
    public const int FuelPerCycleDefault = 1;

    // GATE.T48.TENSION.MAINTENANCE.001: Crew wages per cycle (credits).
    public const int WageCycleTicks = 30;
    public const int WagePerCycleShuttle = 1;
    public const int WagePerCycleCorvette = 2;
    public const int WagePerCycleClipper = 2;
    public const int WagePerCycleFrigate = 4;
    public const int WagePerCycleHauler = 3;
    public const int WagePerCycleCruiser = 7;
    public const int WagePerCycleCarrier = 6;
    public const int WagePerCycleDreadnought = 15;
    public const int WagePerCycleDefault = 2;
    // Docked wage multiplier in bps (5000 = 50%).
    public const int DockedWageMultiplierBps = 5000;

    // GATE.T48.TENSION.MAINTENANCE.001: Hull degradation per cycle (wear and tear).
    public const int HullDegradCycleTicks = 45;
    public const int HullDegradPerCycleShuttle = 1;
    public const int HullDegradPerCycleCorvette = 1;
    public const int HullDegradPerCycleClipper = 1;
    public const int HullDegradPerCycleFrigate = 2;
    public const int HullDegradPerCycleHauler = 2;
    public const int HullDegradPerCycleCruiser = 3;
    public const int HullDegradPerCycleCarrier = 3;
    public const int HullDegradPerCycleDreadnought = 5;
    public const int HullDegradPerCycleDefault = 1;
}
