using System.Collections.Generic;

namespace SimCore.Tweaks;

// GATE.S5.NPC_TRADE.SYSTEM.001: NPC trade circulation pacing constants.
public static class NpcTradeTweaksV0
{
    // How often NPC trade evaluation runs (every N ticks).
    public const int EvalIntervalTicks = 15;

    // Minimum price difference (sell price at dest - buy price at source) to justify a trade.
    // Lowered from 5 to 3: ensures NPC traders move even when adjacent same-type nodes
    // have moderate stock differences (fuel/metal variance within mining/refinery clusters).
    public const int ProfitThresholdCredits = 3;

    // Max goods an NPC trader can carry per trip.
    public const int MaxTradeUnitsPerTrip = 10;

    // Max edges from current node to evaluate for trade opportunities.
    public const int EvalRadiusEdges = 1;

    // GATE.S18.TRADE_GOODS.NPC_TRADE_UPDATE.001: Good-specific trade priority weights.
    // Higher weight = NPC more likely to haul this good. Multiplied against profit to rank opportunities.
    // Weights encourage geographic arbitrage: extraction goods flow from producing regions to consumers.
    public static readonly IReadOnlyDictionary<string, int> GoodTradeWeights = new Dictionary<string, int>(System.StringComparer.Ordinal)
    {
        // Extraction tier — flow from source nodes outward
        [SimCore.Content.WellKnownGoodIds.Fuel]           = 100,
        [SimCore.Content.WellKnownGoodIds.Ore]            = 120,
        [SimCore.Content.WellKnownGoodIds.Organics]       = 130, // agri → industrial
        [SimCore.Content.WellKnownGoodIds.RareMetals]     = 150, // deposit → military

        // Processed tier — inter-region trade
        [SimCore.Content.WellKnownGoodIds.Metal]          = 100,
        [SimCore.Content.WellKnownGoodIds.Food]           = 110,
        [SimCore.Content.WellKnownGoodIds.Composites]     = 100,
        [SimCore.Content.WellKnownGoodIds.Electronics]    = 100,
        [SimCore.Content.WellKnownGoodIds.Munitions]      = 140, // military demand

        // Manufactured tier
        [SimCore.Content.WellKnownGoodIds.Components]     = 120,

        // Exotic tier — high value, low volume
        [SimCore.Content.WellKnownGoodIds.ExoticCrystals] = 80,
        [SimCore.Content.WellKnownGoodIds.SalvagedTech]   = 90,
        [SimCore.Content.WellKnownGoodIds.ExoticMatter]   = 80,
    };

    // Default weight for goods not in the table.
    public const int DefaultGoodWeight = 100;
}
