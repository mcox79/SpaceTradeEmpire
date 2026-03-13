using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.ADAPTATION.COLLECTION.001: Adaptation fragment collection + resonance pair resolution.
public static class AdaptationFragmentSystem
{
    public sealed class CollectResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
        public string? CompletedPairId { get; set; }
    }

    // Collect a fragment at the player's current node.
    public static CollectResult CollectFragment(SimState state, string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId))
            return new CollectResult { Success = false, Reason = "empty_fragment_id" };

        if (!state.AdaptationFragments.TryGetValue(fragmentId, out var fragment))
            return new CollectResult { Success = false, Reason = "fragment_not_found" };

        if (fragment.IsCollected)
            return new CollectResult { Success = false, Reason = "already_collected" };

        // Must be at the fragment's node.
        if (!state.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            return new CollectResult { Success = false, Reason = "fleet_not_found" };

        if (!string.Equals(fleet.CurrentNodeId, fragment.NodeId, StringComparison.Ordinal))
            return new CollectResult { Success = false, Reason = "wrong_node" };

        fragment.CollectedTick = state.Tick;

        // Check if this completes a resonance pair.
        string? completedPair = CheckResonancePairCompletion(state, fragment.ResonancePairId);

        return new CollectResult { Success = true, CompletedPairId = completedPair };
    }

    // Deposit a collected fragment into Haven's Trophy Wall.
    public static CollectResult DepositFragment(SimState state, string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId))
            return new CollectResult { Success = false, Reason = "empty_fragment_id" };

        if (!state.AdaptationFragments.TryGetValue(fragmentId, out var fragment))
            return new CollectResult { Success = false, Reason = "fragment_not_found" };

        if (!fragment.IsCollected)
            return new CollectResult { Success = false, Reason = "not_collected" };

        if (state.Haven?.TrophyWall == null)
            return new CollectResult { Success = false, Reason = "haven_not_available" };

        if (state.Haven.TrophyWall.ContainsKey(fragmentId))
            return new CollectResult { Success = false, Reason = "already_deposited" };

        state.Haven.TrophyWall[fragmentId] = state.Tick;

        return new CollectResult { Success = true };
    }

    // Check if both fragments in a resonance pair are collected.
    private static string? CheckResonancePairCompletion(SimState state, string pairId)
    {
        if (string.IsNullOrEmpty(pairId)) return null;

        var pair = AdaptationFragmentContentV0.GetPairById(pairId);
        if (pair == null) return null;

        bool hasA = state.AdaptationFragments.TryGetValue(pair.FragmentA, out var fragA) && fragA.IsCollected;
        bool hasB = state.AdaptationFragments.TryGetValue(pair.FragmentB, out var fragB) && fragB.IsCollected;

        return hasA && hasB ? pairId : null;
    }

    // Get all completed resonance pair IDs for the player.
    public static List<string> GetCompletedResonancePairs(SimState state)
    {
        var completed = new List<string>();
        foreach (var pair in AdaptationFragmentContentV0.AllResonancePairs)
        {
            bool hasA = state.AdaptationFragments.TryGetValue(pair.FragmentA, out var fragA) && fragA.IsCollected;
            bool hasB = state.AdaptationFragments.TryGetValue(pair.FragmentB, out var fragB) && fragB.IsCollected;
            if (hasA && hasB)
                completed.Add(pair.PairId);
        }
        return completed;
    }

    // Get the trade margin bonus in basis points from completed resonance pairs.
    public static int GetTradeMarginBonusBps(SimState state)
    {
        var completed = GetCompletedResonancePairs(state);
        // pair_01 grants trade margin bonus.
        return completed.Contains("pair_01") ? AdaptationTweaksV0.TradeMarginBonusBps : 0;
    }

    // Get scan range bonus percentage from completed resonance pairs.
    public static int GetScanRangeBonusPct(SimState state)
    {
        var completed = GetCompletedResonancePairs(state);
        return completed.Contains("pair_02") ? AdaptationTweaksV0.ScanRangeBonusPct : 0;
    }

    // Get extra hangar bay count from completed resonance pairs.
    public static int GetHangarBayBonus(SimState state)
    {
        var completed = GetCompletedResonancePairs(state);
        return completed.Contains("pair_03") ? AdaptationTweaksV0.HangarBayBonus : 0;
    }

    // Get fracture travel cost reduction percentage from completed resonance pairs.
    public static int GetFractureCostReductionPct(SimState state)
    {
        var completed = GetCompletedResonancePairs(state);
        return completed.Contains("pair_04") ? AdaptationTweaksV0.FractureCostReductionPct : 0;
    }
}
