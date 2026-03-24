namespace SimCore.Tweaks;

// GATE.X.FLEET_UPKEEP.DRAIN.001: Fleet upkeep cost constants.
public static class FleetUpkeepTweaksV0
{
    // GATE.T52.ECON.UPKEEP_TUNE.001: Upkeep 10x increase for credit tension.
    // Per-cycle upkeep cost in credits by ship class.
    // With trade profits ~500-1000cr, 20cr/cycle shuttle makes upkeep noticeable.
    // Player feels pressure to trade efficiently or automate.
    public const int ShuttleUpkeep = 20;
    public const int CorvetteUpkeep = 50;
    public const int ClipperUpkeep = 40;
    public const int FrigateUpkeep = 80;
    public const int HaulerUpkeep = 60;
    public const int CruiserUpkeep = 150;
    public const int CarrierUpkeep = 120;
    public const int DreadnoughtUpkeep = 500;
    // Ancient hulls: high upkeep (exotic maintenance).
    public const int AncientBastionUpkeep = 300;
    public const int AncientSeekerUpkeep = 250;
    public const int AncientThresholdUpkeep = 350;

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
    // GATE.T52.ECON.UPKEEP_TUNE.001: Wages 10x for credit tension.
    public const int WagePerCycleShuttle = 10;
    public const int WagePerCycleCorvette = 20;
    public const int WagePerCycleClipper = 20;
    public const int WagePerCycleFrigate = 40;
    public const int WagePerCycleHauler = 30;
    public const int WagePerCycleCruiser = 70;
    public const int WagePerCycleCarrier = 60;
    public const int WagePerCycleDreadnought = 150;
    public const int WagePerCycleDefault = 20;
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
