using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.HAVEN.MARKET_EVOLUTION.001: Haven market tier-based periodic restocking.
public static class HavenMarketSystem
{
    // Per-tick processing: restock Haven market periodically based on tier.
    public static void Process(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;
        if (string.IsNullOrEmpty(haven.MarketId)) return;
        if (!state.Markets.TryGetValue(haven.MarketId, out var mkt)) return;

        // Restock every MarketRestockIntervalTicks.
        if (state.Tick % HavenTweaksV0.MarketRestockIntervalTicks != 0) return;

        // Delegate to existing tier-based refresh logic.
        Gen.GalaxyGenerator.RefreshHavenMarketV0(state, haven.Tier);
    }
}
