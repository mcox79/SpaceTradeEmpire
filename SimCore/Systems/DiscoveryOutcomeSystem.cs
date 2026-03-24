using SimCore.Entities;
using SimCore.Content;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimCore.Systems;

// GATE.S6.OUTCOME.REWARD_MODEL.001: When a discovery reaches Analyzed phase, generate outcome rewards.
// GATE.S6.ANOMALY.REWARD_LOOT.001: Family-specific loot on encounter completion.
// GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001: Template-driven FlavorText for discoveries.
public static class DiscoveryOutcomeSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedKeys = new();
        public readonly List<string> Links = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
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

            // GATE.T41.INSTAB_REVEAL.VISIBILITY.001: Skip flavor text for gated discoveries
            // where local instability is below the gate threshold.
            if (disc.InstabilityGate > 0)
            {
                string gateNodeId = FindNodeForDiscovery(state, disc.DiscoveryId);
                int localInstability = 0;
                if (!string.IsNullOrEmpty(gateNodeId) && state.Nodes.TryGetValue(gateNodeId, out var gateNode))
                    localInstability = gateNode.InstabilityLevel;

                if (localInstability < disc.InstabilityGate)
                {
                    // Discovery is hidden — suppress flavor text.
                    if (!string.IsNullOrEmpty(disc.FlavorText))
                        disc.FlavorText = "";
                    continue; // Skip outcome generation too.
                }
            }

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

            // GATE.T41.DISCOVERY_INTEL.SYSTEM.001: Generate trade intel from analyzed discovery.
            GenerateDiscoveryTradeIntel(state, nodeId2, kind2, disc.DiscoveryId);

            // GATE.T41.ANOMALY_CHAIN.ADVANCE.001: Try to advance anomaly chains.
            TryAdvanceChains(state, disc.DiscoveryId, kind2, nodeId2);

            state.AnomalyEncounters[outcomeKey] = outcome;

            // GATE.T53.BOT.DISCOVERY_LOOT.001: Create LootDrop from encounter loot so player can collect.
            if (outcome.LootItems.Count > 0 || outcome.CreditReward > 0)
            {
                var dropId = "loot_disc_" + outcomeKey;
                if (!state.LootDrops.ContainsKey(dropId))
                {
                    var drop = new Entities.LootDrop
                    {
                        Id = dropId,
                        NodeId = nodeId2,
                        Rarity = Entities.LootRarity.Uncommon,
                        TickCreated = state.Tick,
                        Credits = outcome.CreditReward,
                    };
                    foreach (var kv in outcome.LootItems)
                        drop.Goods[kv.Key] = kv.Value;
                    state.LootDrops[dropId] = drop;
                }
            }
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

        var scratch = s_scratch.GetOrCreateValue(state);
        var siteIds = scratch.SortedKeys;
        siteIds.Clear();
        foreach (var k in state.VoidSites.Keys) siteIds.Add(k);
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
        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedEdgeKeys = scratch.SortedKeys;
        sortedEdgeKeys.Clear();
        foreach (var k in state.Edges.Keys) sortedEdgeKeys.Add(k);
        sortedEdgeKeys.Sort(StringComparer.Ordinal);
        foreach (var edgeKey in sortedEdgeKeys)
        {
            var edge = state.Edges[edgeKey];
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

    // GATE.T41.DISCOVERY_INTEL.SYSTEM.001: Generate trade intel from an analyzed discovery.
    // Scans adjacent markets for profitable trade routes and creates TradeRouteIntel entries.
    private static void GenerateDiscoveryTradeIntel(SimState state, string nodeId, string kind, string discoveryId)
    {
        if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(discoveryId)) return;
        if (state.Intel?.TradeRoutes is null) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var adjacentNodes = scratch.Links;
        adjacentNodes.Clear();

        // Find adjacent nodes via edges (deterministic: sorted).
        var sortedEdgeKeys = scratch.SortedKeys;
        sortedEdgeKeys.Clear();
        foreach (var k in state.Edges.Keys) sortedEdgeKeys.Add(k);
        sortedEdgeKeys.Sort(StringComparer.Ordinal);

        foreach (var edgeKey in sortedEdgeKeys)
        {
            var edge = state.Edges[edgeKey];
            if (string.Equals(edge.FromNodeId, nodeId, StringComparison.Ordinal) && !adjacentNodes.Contains(edge.ToNodeId))
                adjacentNodes.Add(edge.ToNodeId);
            else if (string.Equals(edge.ToNodeId, nodeId, StringComparison.Ordinal) && !adjacentNodes.Contains(edge.FromNodeId))
                adjacentNodes.Add(edge.FromNodeId);
        }

        if (adjacentNodes.Count == 0) return;

        // For RESOURCE_POOL_MARKER: find the best sell price at adjacent nodes for the associated good.
        // For CORRIDOR_TRACE: create route between the two connected endpoints.
        // For default (AnomalyFamily): scan adjacent for highest profit differential.
        if (string.Equals(kind, "RESOURCE_POOL_MARKER", StringComparison.Ordinal))
        {
            // Associated good is in the discovery RefId: disc_v0|RESOURCE_POOL_MARKER|nodeId|goodId|sourceId
            string goodId = ParseDiscoveryRefId(discoveryId);
            if (string.IsNullOrEmpty(goodId)) return;

            // Find the local buy price.
            if (!state.Markets.TryGetValue(nodeId, out var localMarket)) return;
            int localBuyPrice = localMarket.GetPrice(goodId);
            if (localBuyPrice <= 0) return;

            // Find best sell price at adjacent nodes.
            string bestDest = "";
            int bestProfit = 0;
            foreach (var adjNode in adjacentNodes)
            {
                if (!state.Markets.TryGetValue(adjNode, out var adjMarket)) continue;
                int sellPrice = adjMarket.GetPrice(goodId);
                int profit = sellPrice - localBuyPrice;
                if (profit > bestProfit)
                {
                    bestProfit = profit;
                    bestDest = adjNode;
                }
            }

            if (bestProfit >= Tweaks.DiscoveryIntelTweaksV0.DiscoveryRouteMinProfit && !string.IsNullOrEmpty(bestDest))
            {
                CreateDiscoveryRoute(state, nodeId, bestDest, goodId, bestProfit, discoveryId);
            }
        }
        else if (string.Equals(kind, "CORRIDOR_TRACE", StringComparison.Ordinal))
        {
            // Corridor connects two endpoints — parse from discoveryId.
            // Format: disc_v0|CORRIDOR_TRACE|nodeA|nodeB|laneId
            string refId = ParseDiscoveryRefId(discoveryId);
            if (string.IsNullOrEmpty(refId)) return;

            // Find any shared good with profitable differential.
            if (!state.Markets.TryGetValue(nodeId, out var mktA)) return;
            if (!state.Markets.TryGetValue(refId, out var mktB)) return;

            // Check each good at both endpoints.
            var goodIds = new List<string>();
            foreach (var g in mktA.Inventory.Keys) if (!goodIds.Contains(g)) goodIds.Add(g);
            goodIds.Sort(StringComparer.Ordinal);

            foreach (var g in goodIds)
            {
                int priceA = mktA.GetPrice(g);
                int priceB = mktB.GetPrice(g);
                if (priceA <= 0 || priceB <= 0) continue;

                // Route in whichever direction is profitable.
                int profitAB = priceB - priceA;
                int profitBA = priceA - priceB;

                if (profitAB >= Tweaks.DiscoveryIntelTweaksV0.DiscoveryRouteMinProfit)
                    CreateDiscoveryRoute(state, nodeId, refId, g, profitAB, discoveryId);
                else if (profitBA >= Tweaks.DiscoveryIntelTweaksV0.DiscoveryRouteMinProfit)
                    CreateDiscoveryRoute(state, refId, nodeId, g, profitBA, discoveryId);
            }
        }
        else
        {
            // Generic (AnomalyFamily): scan adjacent markets for highest profit differential.
            if (!state.Markets.TryGetValue(nodeId, out var localMkt)) return;

            string bestSrc = "";
            string bestDst = "";
            string bestGood = "";
            int bestProfit = 0;

            foreach (var adjNode in adjacentNodes)
            {
                if (!state.Markets.TryGetValue(adjNode, out var adjMkt)) continue;

                var goodIds = new List<string>();
                foreach (var g in localMkt.Inventory.Keys) if (!goodIds.Contains(g)) goodIds.Add(g);
                foreach (var g in adjMkt.Inventory.Keys) if (!goodIds.Contains(g)) goodIds.Add(g);
                goodIds.Sort(StringComparer.Ordinal);

                foreach (var g in goodIds)
                {
                    int localPrice = localMkt.GetPrice(g);
                    int adjPrice = adjMkt.GetPrice(g);
                    if (localPrice <= 0 || adjPrice <= 0) continue;

                    int profit = adjPrice - localPrice;
                    if (profit > bestProfit)
                    {
                        bestProfit = profit;
                        bestSrc = nodeId;
                        bestDst = adjNode;
                        bestGood = g;
                    }

                    int reverseProfit = localPrice - adjPrice;
                    if (reverseProfit > bestProfit)
                    {
                        bestProfit = reverseProfit;
                        bestSrc = adjNode;
                        bestDst = nodeId;
                        bestGood = g;
                    }
                }
            }

            if (bestProfit >= Tweaks.DiscoveryIntelTweaksV0.DiscoveryRouteMinProfit
                && !string.IsNullOrEmpty(bestSrc) && !string.IsNullOrEmpty(bestDst) && !string.IsNullOrEmpty(bestGood))
            {
                CreateDiscoveryRoute(state, bestSrc, bestDst, bestGood, bestProfit, discoveryId);
            }
        }
    }

    private static void CreateDiscoveryRoute(SimState state, string srcNode, string dstNode, string goodId, int profit, string discoveryId)
    {
        var routeId = IntelBook.RouteKey(srcNode, dstNode, goodId);
        if (state.Intel.TradeRoutes.ContainsKey(routeId)) return;

        state.Intel.TradeRoutes[routeId] = new TradeRouteIntel
        {
            RouteId = routeId,
            SourceNodeId = srcNode,
            DestNodeId = dstNode,
            GoodId = goodId,
            EstimatedProfitPerUnit = profit,
            DiscoveredTick = state.Tick,
            LastValidatedTick = state.Tick,
            Status = TradeRouteStatus.Discovered,
            SourceDiscoveryId = discoveryId
        };
    }

    // Parse "disc_v0|KIND|nodeId|refId|sourceId" → refId (parts[3]).
    private static string ParseDiscoveryRefId(string discoveryId)
    {
        if (string.IsNullOrEmpty(discoveryId)) return "";
        var parts = discoveryId.Split('|');
        return parts.Length >= 4 ? parts[3] : "";
    }

    // GATE.T41.ANOMALY_CHAIN.ADVANCE.001: Advance anomaly chains when a matching discovery is analyzed.
    private static void TryAdvanceChains(SimState state, string discoveryId, string kind, string nodeId)
    {
        if (state.AnomalyChains is null || state.AnomalyChains.Count == 0) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedChainIds = scratch.SortedKeys;
        sortedChainIds.Clear();
        foreach (var k in state.AnomalyChains.Keys) sortedChainIds.Add(k);
        sortedChainIds.Sort(StringComparer.Ordinal);

        foreach (var chainId in sortedChainIds)
        {
            if (!state.AnomalyChains.TryGetValue(chainId, out var chain)) continue;
            if (chain.Status != AnomalyChainStatus.Active) continue;
            if (chain.CurrentStepIndex >= chain.Steps.Count) continue;

            var step = chain.Steps[chain.CurrentStepIndex];
            if (step.IsCompleted) continue;

            // Match: discovery must be at the placed site for this step.
            if (!string.Equals(step.PlacedDiscoveryId, discoveryId, StringComparison.Ordinal)) continue;

            // Mark step completed.
            step.IsCompleted = true;

            // Create a RumorLead for the next step (breadcrumb).
            if (chain.CurrentStepIndex + 1 < chain.Steps.Count)
            {
                var nextStep = chain.Steps[chain.CurrentStepIndex + 1];
                if (!string.IsNullOrEmpty(step.LeadText) && !string.IsNullOrEmpty(nextStep.PlacedDiscoveryId))
                {
                    string leadId = $"LEAD.CHAIN.{chainId}.{chain.CurrentStepIndex}";
                    if (!state.Intel.RumorLeads.ContainsKey(leadId))
                    {
                        // Extract the node for the next step's discovery.
                        string nextNodeId = FindNodeForDiscovery(state, nextStep.PlacedDiscoveryId);
                        state.Intel.RumorLeads[leadId] = new RumorLead
                        {
                            LeadId = leadId,
                            Status = RumorLeadStatus.Active,
                            SourceVerbToken = "CHAIN_ADVANCE",
                            Hint = new HintPayloadV0
                            {
                                ImpliedPayoffToken = nextStep.DiscoveryKind,
                                CoarseLocationToken = !string.IsNullOrEmpty(nextNodeId) ? nextNodeId : "UNKNOWN"
                            }
                        };
                    }
                }

                // Create KnowledgeConnection between this step and the next.
                string connId = $"KC.CHAIN.{chainId}.{chain.CurrentStepIndex}";
                state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
                {
                    ConnectionId = connId,
                    SourceDiscoveryId = discoveryId,
                    TargetDiscoveryId = nextStep.PlacedDiscoveryId,
                    ConnectionType = KnowledgeConnectionType.Lead,
                    Description = step.LeadText,
                    IsRevealed = true
                });
            }

            chain.CurrentStepIndex++;

            // Check if chain is complete.
            if (chain.CurrentStepIndex >= chain.Steps.Count)
            {
                chain.Status = AnomalyChainStatus.Completed;

                // Apply climax loot overrides from the final step.
                if (step.LootOverrides.Count > 0)
                {
                    foreach (var loot in step.LootOverrides)
                    {
                        if (string.Equals(loot.Key, "credits", StringComparison.Ordinal))
                            state.PlayerCredits += loot.Value;
                        else if (string.Equals(loot.Key, Content.WellKnownGoodIds.ExoticMatter, StringComparison.Ordinal))
                            InventoryLedger.AddCargo(state.PlayerCargo, Content.WellKnownGoodIds.ExoticMatter, loot.Value);
                    }
                }
            }
        }
    }

    // GATE.T41.SURVEY_PROG.UNLOCK.001: Count discoveries at Phase >= Scanned for a given family.
    public static int GetManualScanCountByFamily(SimState state, string family)
    {
        if (state?.Intel?.Discoveries is null || string.IsNullOrEmpty(family)) return 0;

        int count = 0;
        foreach (var kvp in state.Intel.Discoveries)
        {
            var disc = kvp.Value;
            if (disc is null || disc.Phase < DiscoveryPhase.Scanned) continue;
            string kind = ParseDiscoveryKind(disc.DiscoveryId);
            string discFamily = MapKindToFamily(kind);
            if (string.Equals(discFamily, family, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    // GATE.S7.REVEALS.DISCOVERY_REVEAL.001: Progressive content reveal per discovery phase.
    // Seen: surface data only. Scanned: deeper structural detail. Analyzed: knowledge graph links.
    public sealed class RevealContentResult
    {
        public string SurfaceText { get; init; } = "";
        public string DeepText { get; init; } = "";
        public string ConnectionText { get; init; } = "";
        public string[] LinkedKnowledgeNodeIds { get; init; } = Array.Empty<string>();
        public DiscoveryPhase Phase { get; init; }
    }

    // GATE.S7.REVEALS.DISCOVERY_REVEAL.001: Recontextualization templates per (kind, phase).
    // Deeper than FlavorText — provides structural interpretation that changes as understanding deepens.
    private static readonly Dictionary<(string Kind, DiscoveryPhase Phase), string> RecontextTemplates
        = new()
    {
        { ("RESOURCE_POOL_MARKER", DiscoveryPhase.Seen),     "Surface scans detect mineral concentrations." },
        { ("RESOURCE_POOL_MARKER", DiscoveryPhase.Scanned),  "Subsurface analysis reveals an ancient excavation site — someone was here before." },
        { ("RESOURCE_POOL_MARKER", DiscoveryPhase.Analyzed), "The extraction patterns match no known faction technology. This predates current civilization." },

        { ("CORRIDOR_TRACE", DiscoveryPhase.Seen),     "Unusual energy readings detected along a narrow band." },
        { ("CORRIDOR_TRACE", DiscoveryPhase.Scanned),  "The energy band is a stable tunnel through fracture-space, artificially maintained." },
        { ("CORRIDOR_TRACE", DiscoveryPhase.Analyzed), "This corridor was engineered. Its maintenance systems still function after millennia." },

        { ("FRACTURE_DERELICT", DiscoveryPhase.Seen),     "A vessel of unknown origin, dead in space." },
        { ("FRACTURE_DERELICT", DiscoveryPhase.Scanned),  "The hull material doesn't match any known alloy. Drive core readings are unprecedented." },
        { ("FRACTURE_DERELICT", DiscoveryPhase.Analyzed), "This ship traveled through fracture-space routinely. Its drive design rewrites physics." },
    };

    // GATE.S7.REVEALS.DISCOVERY_REVEAL.001: Get progressive reveal content for a discovery.
    public static RevealContentResult GetRevealContent(SimState state, string discoveryId)
    {
        if (state?.Intel?.Discoveries is null || string.IsNullOrEmpty(discoveryId))
            return new RevealContentResult { SurfaceText = "No data available." };

        if (!state.Intel.Discoveries.TryGetValue(discoveryId, out var disc))
            return new RevealContentResult { SurfaceText = "Discovery not found." };

        string kind = ParseDiscoveryKind(discoveryId);
        string nodeId = FindNodeForDiscovery(state, discoveryId);
        string systemName = ResolveSystemName(state, nodeId);

        // Surface text: always available.
        string surfaceText = "";
        if (RecontextTemplates.TryGetValue((kind, DiscoveryPhase.Seen), out var st))
            surfaceText = st;
        else
            surfaceText = $"An anomalous reading in {systemName}.";

        // Deep text: available at Scanned+.
        string deepText = "";
        if (disc.Phase >= DiscoveryPhase.Scanned)
        {
            if (RecontextTemplates.TryGetValue((kind, DiscoveryPhase.Scanned), out var dt))
                deepText = dt;
            else
                deepText = $"Detailed scans reveal complex underlying structure in {systemName}.";
        }

        // Connection text + knowledge graph links: available at Analyzed.
        string connectionText = "";
        string[] linkedNodes = Array.Empty<string>();
        if (disc.Phase >= DiscoveryPhase.Analyzed)
        {
            if (RecontextTemplates.TryGetValue((kind, DiscoveryPhase.Analyzed), out var ct))
                connectionText = ct;
            else
                connectionText = $"Full analysis complete. Cross-referencing with knowledge database.";

            // Find knowledge graph connections involving this discovery.
            linkedNodes = FindKnowledgeGraphLinks(state, discoveryId);
        }

        return new RevealContentResult
        {
            SurfaceText = surfaceText,
            DeepText = deepText,
            ConnectionText = connectionText,
            LinkedKnowledgeNodeIds = linkedNodes,
            Phase = disc.Phase
        };
    }

    // GATE.S7.REVEALS.DISCOVERY_REVEAL.001: Find knowledge graph connections for a discovery.
    private static string[] FindKnowledgeGraphLinks(SimState state, string discoveryId)
    {
        var connections = state.Intel?.KnowledgeConnections;
        if (connections is null || connections.Count == 0)
            return Array.Empty<string>();

        var scratch = s_scratch.GetOrCreateValue(state);
        var links = scratch.Links;
        links.Clear();
        foreach (var conn in connections)
        {
            if (conn is null) continue;
            if (string.Equals(conn.SourceDiscoveryId, discoveryId, StringComparison.Ordinal) ||
                string.Equals(conn.TargetDiscoveryId, discoveryId, StringComparison.Ordinal))
            {
                links.Add(conn.ConnectionId);
            }
        }
        links.Sort(StringComparer.Ordinal);
        return links.ToArray();
    }
}
