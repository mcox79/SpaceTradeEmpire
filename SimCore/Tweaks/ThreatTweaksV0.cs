namespace SimCore.Tweaks;

// GATE.S8.THREAT.SUPPLY_SHOCK.001: Warfront threat impact on production.
public static class ThreatTweaksV0
{
    // Output reduction at Skirmish intensity (percentage).
    public const int SkirmishOutputReductionPct = 40;

    // Output reduction at OpenWar/TotalWar intensity (percentage — 100 = full halt).
    public const int BattleOutputReductionPct = 100;
}
