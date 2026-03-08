using SimCore.Entities;
using SimCore.Content;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S6.OUTCOME.REWARD_MODEL.001: When a discovery reaches Analyzed phase, generate outcome rewards.
// GATE.S6.ANOMALY.REWARD_LOOT.001: Family-specific loot on encounter completion.
public static class DiscoveryOutcomeSystem
{
    // Called after IntelSystem. Checks for newly Analyzed discoveries and generates outcomes.
    public static void Process(SimState state)
    {
        if (state.Intel?.Discoveries is null) return;

        foreach (var kvp in state.Intel.Discoveries)
        {
            var disc = kvp.Value;
            if (disc is null) continue;
            if (disc.Phase != DiscoveryPhase.Analyzed) continue;

            // Check if we already generated an outcome for this discovery.
            string outcomeKey = "OUTCOME_" + disc.DiscoveryId;
            if (state.AnomalyEncounters.ContainsKey(outcomeKey)) continue;

            // Find the discovery's node.
            string nodeId = FindNodeForDiscovery(state, disc.DiscoveryId);
            if (string.IsNullOrEmpty(nodeId)) continue;

            // Parse kind from discovery ID: "disc_v0|<KIND>|<NodeId>|<RefId>|<SourceId>"
            string kind = ParseDiscoveryKind(disc.DiscoveryId);

            var outcome = new AnomalyEncounter
            {
                EncounterId = outcomeKey,
                NodeId = nodeId,
                DiscoveryId = disc.DiscoveryId,
                Family = MapKindToFamily(kind),
                Status = AnomalyEncounterStatus.Completed,
                CreatedTick = state.Tick
            };

            // Generate kind-specific rewards.
            ApplyRewardByKind(state, outcome, kind, nodeId);

            state.AnomalyEncounters[outcomeKey] = outcome;
        }
    }

    // GATE.S6.ANOMALY.REWARD_LOOT.001: Generate loot for a completed anomaly encounter by family.
    // DERELICT: salvage goods (scrap, salvaged_tech).
    // RUIN: data samples (anomaly_samples) + credits.
    // SIGNAL: discovery lead at adjacent node.
    public static void GenerateLootByFamily(AnomalyEncounter encounter, SimState state)
    {
        if (encounter is null) return;

        switch (encounter.Family)
        {
            case "DERELICT":
                encounter.LootItems[WellKnownGoodIds.SalvagedTech] = DiscoveryOutcomeTweaksV0.DerelictSalvagedTechQty;
                encounter.CreditReward += DiscoveryOutcomeTweaksV0.DerelictCredits;
                break;
            case "RUIN":
                encounter.LootItems[WellKnownGoodIds.ExoticMatter] = DiscoveryOutcomeTweaksV0.RuinExoticMatterQty;
                encounter.CreditReward += DiscoveryOutcomeTweaksV0.RuinCredits;
                break;
            case "SIGNAL":
                // Signal: discovery lead — pick an adjacent node for further exploration.
                string leadNode = FindAdjacentNode(state, encounter.NodeId);
                if (!string.IsNullOrEmpty(leadNode))
                    encounter.DiscoveryLeadNodeId = leadNode;
                encounter.CreditReward += DiscoveryOutcomeTweaksV0.SignalCredits;
                break;
            default:
                // OUTCOME / unknown: base credit reward only.
                break;
        }
    }

    private static void ApplyRewardByKind(SimState state, AnomalyEncounter outcome, string kind, string nodeId)
    {
        switch (kind)
        {
            case "RESOURCE_POOL_MARKER":
                // Permanent trade bonus: credit reward + goods.
                outcome.Family = "RUIN";
                outcome.LootItems[WellKnownGoodIds.ExoticMatter] = DiscoveryOutcomeTweaksV0.ResourcePoolMarkerSamplesQty;
                outcome.CreditReward = DiscoveryOutcomeTweaksV0.ResourcePoolMarkerCredits;
                state.PlayerCredits += DiscoveryOutcomeTweaksV0.ResourcePoolMarkerCredits;
                break;
            case "CORRIDOR_TRACE":
                // Reveal hidden lane shortcut: credit reward + discovery lead.
                outcome.Family = "SIGNAL";
                string leadNode = FindAdjacentNode(state, nodeId);
                if (!string.IsNullOrEmpty(leadNode))
                    outcome.DiscoveryLeadNodeId = leadNode;
                outcome.CreditReward = DiscoveryOutcomeTweaksV0.CorridorTraceCredits;
                state.PlayerCredits += DiscoveryOutcomeTweaksV0.CorridorTraceCredits;
                break;
            default:
                // Generic outcome: base credits.
                int creditReward = DiscoveryOutcomeTweaksV0.GenericBaseCredits
                    + (state.AnomalyEncounters.Count * DiscoveryOutcomeTweaksV0.GenericPerEncounterBonus);
                outcome.CreditReward = creditReward;
                state.PlayerCredits += creditReward;
                break;
        }
    }

    // Parse "disc_v0|<KIND>|..." → "<KIND>". Returns "" if format doesn't match.
    public static string ParseDiscoveryKind(string discoveryId)
    {
        if (string.IsNullOrEmpty(discoveryId)) return "";
        var parts = discoveryId.Split('|');
        return parts.Length >= 2 ? parts[1] : "";
    }

    private static string MapKindToFamily(string kind)
    {
        return kind switch
        {
            "RESOURCE_POOL_MARKER" => "RUIN",
            "CORRIDOR_TRACE" => "SIGNAL",
            _ => "OUTCOME"
        };
    }

    private static string FindNodeForDiscovery(SimState state, string discoveryId)
    {
        foreach (var nodeKvp in state.Nodes)
        {
            var node = nodeKvp.Value;
            if (node?.SeededDiscoveryIds is null) continue;
            if (node.SeededDiscoveryIds.Contains(discoveryId))
                return node.Id ?? "";
        }
        return "";
    }

    // Find the first adjacent node by scanning edges (deterministic: sorted by edge id).
    private static string FindAdjacentNode(SimState state, string nodeId)
    {
        foreach (var edgeKvp in state.Edges.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var edge = edgeKvp.Value;
            if (string.Equals(edge.FromNodeId, nodeId, StringComparison.Ordinal))
                return edge.ToNodeId;
            if (string.Equals(edge.ToNodeId, nodeId, StringComparison.Ordinal))
                return edge.FromNodeId;
        }
        return "";
    }
}
