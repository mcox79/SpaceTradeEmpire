using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.WAR_CONSEQUENCE.001: When the player delivers war goods
// (munitions, composites, fuel) to a warfront node, create a WarConsequence
// with a delay. After delay ticks, the consequence resolves — showing the
// downstream effect of the player's delivery. Wars have faces.
public static class WarConsequenceSystem
{
    // War goods that trigger consequences
    private static readonly HashSet<string> WarGoodIds = new(StringComparer.Ordinal)
    {
        WellKnownGoodIds.Munitions, WellKnownGoodIds.Composites, WellKnownGoodIds.Fuel
    };

    /// <summary>
    /// Process pending war consequences. Resolve any whose delay has elapsed.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.WarConsequences.Count == 0) return;

        foreach (var kv in state.WarConsequences)
        {
            var wc = kv.Value;
            if (wc.IsResolved) continue;

            int elapsedTicks = state.Tick - wc.CreatedTick;
            if (elapsedTicks >= wc.DelayTicks)
            {
                wc.IsResolved = true;
                wc.ResolvedTick = state.Tick;

                // Generate consequence text based on kind
                if (string.IsNullOrEmpty(wc.ConsequenceText))
                {
                    wc.ConsequenceText = GenerateConsequenceText(state, wc);
                }
            }
        }
    }

    /// <summary>
    /// Check if a trade at a node should create a war consequence, and if so, create one.
    /// Called after player sells war goods at a warfront-adjacent node.
    /// </summary>
    public static void CheckAndCreateConsequence(
        SimState state, string nodeId, string goodId, int quantity)
    {
        if (!WarGoodIds.Contains(goodId)) return;
        if (quantity <= 0) return;

        // Check if this node is part of any active warfront
        string? warfrontId = FindWarfrontForNode(state, nodeId);
        if (warfrontId == null) return;

        string consequenceId = $"WC_{state.Tick}_{nodeId}_{goodId}";

        var consequence = new WarConsequence
        {
            Id = consequenceId,
            Kind = WarConsequenceKind.SupplyDelivered,
            SourceNodeId = nodeId,
            TargetNodeId = FindOpposingSideNode(state, warfrontId, nodeId),
            GoodId = goodId,
            Quantity = quantity,
            CreatedTick = state.Tick,
            DelayTicks = NarrativeTweaksV0.WarConsequenceDelayTicks,
            ManifestText = GenerateManifestText(goodId, quantity, nodeId),
            ConsequenceText = "", // filled on resolution
            IsResolved = false
        };

        state.WarConsequences[consequenceId] = consequence;
    }

    private static string? FindWarfrontForNode(SimState state, string nodeId)
    {
        foreach (var kv in state.Warfronts)
        {
            var wf = kv.Value;
            // Check if the node is in either combatant's territory
            if (state.NodeFactionId.TryGetValue(nodeId, out var factionId))
            {
                if (factionId == wf.CombatantA || factionId == wf.CombatantB)
                    return kv.Key;
            }
        }
        return null;
    }

    private static string FindOpposingSideNode(SimState state, string warfrontId, string sourceNodeId)
    {
        if (!state.Warfronts.TryGetValue(warfrontId, out var wf)) return "";

        string sourceFaction = "";
        if (state.NodeFactionId.TryGetValue(sourceNodeId, out var fid))
            sourceFaction = fid;

        string opposingFaction = sourceFaction == wf.CombatantA ? wf.CombatantB : wf.CombatantA;

        // Find any node belonging to the opposing faction
        foreach (var kv in state.NodeFactionId)
        {
            if (kv.Value == opposingFaction)
                return kv.Key;
        }
        return "";
    }

    private static string GenerateManifestText(string goodId, int quantity, string nodeId)
    {
        return goodId switch
        {
            WellKnownGoodIds.Munitions => $"{quantity} units of munitions delivered to the front.",
            WellKnownGoodIds.Composites => $"{quantity} units of composites delivered for fortification.",
            WellKnownGoodIds.Fuel => $"{quantity} units of fuel delivered to sustain operations.",
            _ => $"{quantity} units of {goodId} delivered."
        };
    }

    private static string GenerateConsequenceText(SimState state, WarConsequence wc)
    {
        return wc.Kind switch
        {
            WarConsequenceKind.SupplyDelivered => wc.GoodId switch
            {
                WellKnownGoodIds.Munitions => "Your munitions fueled an offensive. A civilian transport was caught in the crossfire.",
                WellKnownGoodIds.Composites => "Your composites reinforced a military station. The school module was converted to barracks.",
                WellKnownGoodIds.Fuel => "Your fuel extended patrol range. Three smugglers were intercepted. One was carrying medicine.",
                _ => "Your delivery had consequences downstream."
            },
            WarConsequenceKind.CounteroffensiveDamage =>
                "The counteroffensive you supplied reached its target. Damage reports are still coming in.",
            WarConsequenceKind.CivilianCasualties =>
                "Civilian casualties reported in the sector your supplies reached.",
            _ => "The consequences of your delivery have become apparent."
        };
    }
}
