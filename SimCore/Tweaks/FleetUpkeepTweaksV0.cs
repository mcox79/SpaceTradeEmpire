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
}
