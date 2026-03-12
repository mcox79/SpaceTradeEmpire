using SimCore.Entities;
using SimCore.Content;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;

namespace SimCore.Systems;

// GATE.S6.OUTCOME.REWARD_MODEL.001: When a discovery reaches Analyzed phase, generate outcome rewards.
// GATE.S6.ANOMALY.REWARD_LOOT.001: Family-specific loot on encounter completion.
// GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001: Template-driven FlavorText for discoveries.
public static class DiscoveryOutcomeSystem
{
    // GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001: Narrative templates keyed by (family, phase).
    // Presentation text only — not gameplay-affecting, not included in GetSignature().
    // {system} is replaced with the node display name; {family} with the discovery family.
    private static readonly Dictionary<(string Family, DiscoveryPhase Phase), string> FlavorTemplates
        = new()
    {
        // Anomaly (RESOURCE_POOL_MARKER -> RUIN family)
        { ("RUIN", DiscoveryPhase.Seen),     "Sensor echoes suggest ancient ruins in the {system} system." },
        { ("RUIN", DiscoveryPhase.Scanned),  "Scans reveal a ruin site in {system} with traces of exotic matter." },
        { ("RUIN", DiscoveryPhase.Analyzed), "Analysis complete: the {system} ruins yield valuable exotic matter samples." },

        // Corridor (CORRIDOR_TRACE -> SIGNAL family)
        { ("SIGNAL", DiscoveryPhase.Seen),    "A faint signal trace detected near {system}." },
        { ("SIGNAL", DiscoveryPhase.Scanned), "Signal triangulation in {system} points to a hidden corridor." },
        { ("SIGNAL", DiscoveryPhase.Analyzed),"The {system} signal resolves into a navigable shortcut between systems." },

        // Fracture derelict (GATE.S6.FRACTURE_DISCOVERY.UNLOCK.001)
        { ("DERELICT", DiscoveryPhase.Seen),    "A derelict vessel drifts at the edge of the {system} system, hull scorched by fracture energy." },
        { ("DERELICT", DiscoveryPhase.Scanned), "Scans of the {system} derelict reveal an intact fracture drive core — technology thought impossible." },
        { ("DERELICT", DiscoveryPhase.Analyzed),"Analysis complete: the {system} derelict's fracture drive is operational. Off-lane travel is now possible." },

        // Generic / OUTCOME family
        { ("OUTCOME", DiscoveryPhase.Seen),    "An unidentified discovery detected in the {system} system." },
        { ("OUTCOME", DiscoveryPhase.Scanned), "Preliminary scan of the {system} discovery reveals promising readings." },
        { ("OUTCOME", DiscoveryPhase.Analyzed),"Full analysis of the {system} discovery is complete." },
    };

    // Called after IntelSystem. Checks for newly Analyzed discoveries and generates outcomes.
    // Also populates FlavorText for all discoveries at any phase.
    public static void Process(SimState state)
    {
        if (state.Intel?.Discoveries is null) return;

        foreach (var kvp in state.Intel.Discoveries)
        {
            var disc = kvp.Value;
            if (disc is null) continue;

            // GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001:
            // Populate or update FlavorText based on family + current phase.
            // Re-generated each tick to track phase transitions (Seen -> Scanned -> Analyzed).
            // Pure function: deterministic, no RNG, presentation-only.
            {
                string kind = ParseDiscoveryKind(disc.DiscoveryId);
                string family = MapKindToFamily(kind);
                string nodeId = FindNodeForDiscovery(state, disc.DiscoveryId);
                string systemName = ResolveSystemName(state, nodeId);
                string expected = GenerateFlavorText(family, disc.Phase, systemName);
                if (!string.Equals(disc.FlavorText, expected, StringComparison.Ordinal))
                    disc.FlavorText = expected;
            }

            if (disc.Phase != DiscoveryPhase.Analyzed) continue;

            // Check if we already generated an outcome for this discovery.
            string outcomeKey = "OUTCOME_" + disc.DiscoveryId;
            if (state.AnomalyEncounters.ContainsKey(outcomeKey)) continue;

            // Find the discovery's node.
            string nodeId2 = FindNodeForDiscovery(state, disc.DiscoveryId);
            if (string.IsNullOrEmpty(nodeId2)) continue;

            // Parse kind from discovery ID: "disc_v0|<KIND>|<NodeId>|<RefId>|<SourceId>"
            string kind2 = ParseDiscoveryKind(disc.DiscoveryId);

            var outcome = new AnomalyEncounter
            {
                EncounterId = outcomeKey,
                NodeId = nodeId2,
                DiscoveryId = disc.DiscoveryId,
                Family = MapKindToFamily(kind2),
                Status = AnomalyEncounterStatus.Completed,
                CreatedTick = state.Tick
            };

            // Generate kind-specific rewards.
            ApplyRewardByKind(state, outcome, kind2, nodeId2);

            state.AnomalyEncounters[outcomeKey] = outcome;
        }

        // GATE.S6.FRACTURE_DISCOVERY.UNLOCK.001: Check VoidSites for analyzed FractureDerelict.
        // When a FractureDerelict VoidSite is Surveyed and tick >= FractureDiscoveryMinTick, unlock fracture.
        CheckFractureDerelictUnlock(state);
    }

    // GATE.S6.FRACTURE_DISCOVERY.UNLOCK.001: Analyze derelict -> unlock fracture.
    // Deterministic: iterates VoidSites in ordinal order.
    public static void CheckFractureDerelictUnlock(SimState state)
    {
        if (state.FractureUnlocked) return;
        if (state.Tick < Tweaks.FractureTweaksV0.FractureDiscoveryMinTick) return;

        var siteIds = new List<string>(state.VoidSites.Keys);
        siteIds.Sort(StringComparer.Ordinal);

        foreach (var siteId in siteIds)
        {
            if (!state.VoidSites.TryGetValue(siteId, out var site)) continue;
            if (site.Family != Entities.VoidSiteFamily.FractureDerelict) continue;
            if (site.MarkerState != Entities.VoidSiteMarkerState.Surveyed) continue;

            // Unlock fracture system.
            state.FractureUnlocked = true;
            state.FractureDiscoveryTick = state.Tick;
            return;
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

    // GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001: Resolve node display name for templates.
    private static string ResolveSystemName(SimState state, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return "unknown space";
        if (state.Nodes.TryGetValue(nodeId, out var node) && !string.IsNullOrEmpty(node.Name))
            return node.Name;
        return nodeId;
    }

    // GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001: Generate flavor text from templates.
    // Pure function of (family, phase, systemName). Deterministic and presentation-only.
    public static string GenerateFlavorText(string family, DiscoveryPhase phase, string systemName)
    {
        if (string.IsNullOrEmpty(family)) family = "OUTCOME";
        if (string.IsNullOrEmpty(systemName)) systemName = "unknown space";

        if (FlavorTemplates.TryGetValue((family, phase), out var template))
            return template.Replace("{system}", systemName, StringComparison.Ordinal);

        // Fallback for unmapped families: generic description.
        return $"A {family.ToLowerInvariant()} discovery in {systemName}.";
    }
}
