using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.PENTAGON.DETECT.001: Detects pentagon break and triggers economic cascade.
// GATE.S8.PENTAGON.CASCADE.001: Communion food self-production + downstream GDP shift.
public static class PentagonBreakSystem
{
    public static void Process(SimState state)
    {
        var ss = state.StoryState;
        if (ss == null) return;

        // Detect: R3 fired + fracture unlocked + Communion exists in the galaxy.
        if (!ss.PentagonCascadeActive && ss.HasRevelation(RevelationFlags.R3_Pentagon))
        {
            if (state.FractureUnlocked && HasCommunionNode(state))
            {
                ss.PentagonCascadeActive = true;
                ss.PentagonCascadeTick = state.Tick;
            }
        }

        // Cascade: periodic food injection into Communion markets.
        if (ss.PentagonCascadeActive)
        {
            ProcessCascade(state);
        }
    }

    private static bool HasCommunionNode(SimState state)
    {
        foreach (var kv in state.NodeFactionId)
        {
            if (kv.Value == FactionTweaksV0.CommunionId)
                return true;
        }
        return false;
    }

    private static void ProcessCascade(SimState state)
    {
        if (state.Tick % PentagonBreakTweaksV0.CascadeFoodIntervalTicks != 0)
            return;

        // Inject food into Communion-owned markets (simulates self-production).
        foreach (var kv in state.NodeFactionId)
        {
            if (kv.Value != FactionTweaksV0.CommunionId) continue;

            var nodeId = kv.Key;
            if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;
            var marketId = node.MarketId;
            if (string.IsNullOrEmpty(marketId)) continue;
            if (!state.Markets.TryGetValue(marketId, out var market)) continue;

            if (!market.Inventory.TryGetValue(Content.WellKnownGoodIds.Food, out var foodQty))
                foodQty = 0;
            market.Inventory[Content.WellKnownGoodIds.Food] = foodQty + PentagonBreakTweaksV0.CommunionFoodSelfProductionQty;
        }
    }
}
