namespace SimCore.Tweaks;

// GATE.X.FLEET_UPKEEP.DRAIN.001: Fleet upkeep cost constants.
public static class FleetUpkeepTweaksV0
{
    // GATE.T52.ECON.UPKEEP_TUNE.001: Upkeep scaled for meaningful credit tension.
    // Per-cycle upkeep cost in credits by ship class.
    // GATE.T67.ECON.SINK_UPKEEP.001: Shuttle upkeep raised from 0 to create early-game sink.
    // Solo explorer in a starter ship pays minimal upkeep as investment, not punishment.
    // EVE/X4 pattern: small ongoing costs encourage trade engagement.
    // fh_14: Reduced 50→20. Visual bot showed death spiral at 50 — too aggressive for
    // new players who explore more than trade. 20 is nonzero sink but survivable.
    public const int ShuttleUpkeep = 20;
    public const int CorvetteUpkeep = 250;
    public const int ClipperUpkeep = 200;
    public const int FrigateUpkeep = 400;
    public const int HaulerUpkeep = 300;
    public const int CruiserUpkeep = 750;
    public const int CarrierUpkeep = 600;
    public const int DreadnoughtUpkeep = 2500;
    // Ancient hulls: high upkeep (exotic maintenance).
    public const int AncientBastionUpkeep = 1500;
    public const int AncientSeekerUpkeep = 1250;
    public const int AncientThresholdUpkeep = 1750;

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
    // GATE.T52.ECON.UPKEEP_TUNE.001: Wages scaled for credit tension.
    // Shuttle = 0: solo pilot, no crew to pay. Empire wages start at corvette.
    public const int WagePerCycleShuttle = 0;
    public const int WagePerCycleCorvette = 100;
    public const int WagePerCycleClipper = 100;
    public const int WagePerCycleFrigate = 200;
    public const int WagePerCycleHauler = 150;
    public const int WagePerCycleCruiser = 350;
    public const int WagePerCycleCarrier = 300;
    public const int WagePerCycleDreadnought = 750;
    public const int WagePerCycleDefault = 100;
    // Docked wage multiplier in bps (5000 = 50%).
    public const int DockedWageMultiplierBps = 5000;

    // GATE.T65.ECON.SINK_BOOST.001: Per-hop lane transit fee (credits).
    // EVE/X4 pattern: meaningful credit drain per jump to prevent infinite free travel.
    // fh_11: sink_faucet=0.001-0.004 (target >0.05). Transit fee was negligible (35 cr vs 750K earnings).
    // Raised to 150 cr/hop. 2 hops/trade × 150 = 300 cr friction (~5% of a 6000 cr trade).
    public const int LaneTransitFeeCr = 150;

    // GATE.T67.ECON.SINK_UPKEEP.001: Docking fee per dock event (credits).
    // EVE station tax pattern: small fee per dock encourages efficient route planning.
    // fh_11: Raised from 20→75 cr. Combined with transit fee, total per-trade overhead = 375 cr.
    public const int DockingFeeCr = 75;

    // fh_14: Raised from 250→350 bps (3.5%). Safety net waives flat fees for low-credit players,
    // reducing effective sink ratio to 0.038. Trade tax is proportional and only fires on
    // profitable actions, so it compensates without punishing struggling players.
    // At 200 decisions with ~107K earned: 3.5% = ~3750 cr sink → sink_faucet ~0.06.
    public const int TradeTaxBps = 350; // 3.5% tax on all trades

    // fh_14: Economy safety net — waive passive costs when credits critically low.
    // Prevents death spiral where slow-trading players bleed to negative credits.
    // Per design: "costs as investment, not punishment" and "no time pressure on player ship."
    // When PlayerCredits < threshold: upkeep, transit fees, and docking fees waived.
    // Trade tax stays (proportional to income, only triggers on profitable action).
    public const int LowFundsThreshold = 500;

    // GATE.T48.TENSION.MAINTENANCE.001: Hull degradation per cycle (wear and tear).
    public const int HullDegradCycleTicks = 45;
    // Shuttle = 0: starter ship doesn't degrade passively. Damage comes from combat.
    public const int HullDegradPerCycleShuttle = 0;
    public const int HullDegradPerCycleCorvette = 1;
    public const int HullDegradPerCycleClipper = 1;
    public const int HullDegradPerCycleFrigate = 2;
    public const int HullDegradPerCycleHauler = 2;
    public const int HullDegradPerCycleCruiser = 3;
    public const int HullDegradPerCycleCarrier = 3;
    public const int HullDegradPerCycleDreadnought = 5;
    public const int HullDegradPerCycleDefault = 1;
}
