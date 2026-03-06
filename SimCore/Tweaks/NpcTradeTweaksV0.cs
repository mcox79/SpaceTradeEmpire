namespace SimCore.Tweaks;

// GATE.S5.NPC_TRADE.SYSTEM.001: NPC trade circulation pacing constants.
public static class NpcTradeTweaksV0
{
    // How often NPC trade evaluation runs (every N ticks).
    public const int EvalIntervalTicks = 15;

    // Minimum price difference (sell price at dest - buy price at source) to justify a trade.
    public const int ProfitThresholdCredits = 5;

    // Max goods an NPC trader can carry per trip.
    public const int MaxTradeUnitsPerTrip = 10;

    // Max edges from current node to evaluate for trade opportunities.
    public const int EvalRadiusEdges = 1;
}
