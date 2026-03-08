namespace SimCore.Tweaks;

// GATE.S6.OUTCOME.REWARD_MODEL.001: Discovery outcome reward tuning.
// GATE.S6.ANOMALY.REWARD_LOOT.001: Family-specific loot quantities.
public static class DiscoveryOutcomeTweaksV0
{
    // --- Family-specific loot (GenerateLootByFamily) ---
    public static int DerelictSalvagedTechQty { get; } = 2;
    public static int DerelictCredits { get; } = 25;
    public static int RuinExoticMatterQty { get; } = 3;
    public static int RuinCredits { get; } = 75;
    public static int SignalCredits { get; } = 15;

    // --- Kind-specific outcome rewards (ApplyRewardByKind) ---
    public static int ResourcePoolMarkerSamplesQty { get; } = 3;
    public static int ResourcePoolMarkerCredits { get; } = 100;
    public static int CorridorTraceCredits { get; } = 50;
    public static int GenericBaseCredits { get; } = 50;
    public static int GenericPerEncounterBonus { get; } = 10;
}
