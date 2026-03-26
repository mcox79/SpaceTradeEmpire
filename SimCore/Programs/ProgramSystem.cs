using System;
using System.Linq;
using SimCore.Intents;
using SimCore.Events;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Programs;

public static class ProgramSystem
{
    // Hook for Program vs ManualOverride interaction. Must remain deterministic.
    private static void ApplyManualOverrideInteractions(SimState state, long tick)
    {
        if (state is null) return;
        if (state.LogisticsEventLog is null) return;
        if (state.LogisticsEventLog.Count == 0) return;

        // Deterministic: collect fleets that had ManualOverrideSet at this tick.
        // We avoid string parsing in Note and rely on the schema-bound event type.
        var affectedFleets = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (var e in state.LogisticsEventLog)
        {
            if (e is null) continue;
            if (e.Tick != tick) continue;
            if (e.Type != LogisticsEvents.LogisticsEventType.ManualOverrideSet) continue;

            var fid = e.FleetId ?? "";
            if (!string.IsNullOrWhiteSpace(fid)) affectedFleets.Add(fid);
        }

        if (affectedFleets.Count == 0) return;

        if (state.Programs is null) return;
        if (state.Programs.Instances is null) return;

        // Policy: ManualOverride takes authority for the affected fleet only.
        // Pause only programs explicitly bound to that fleet.
        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var p = kv.Value;
            if (p is null) continue;

            var pfid = p.FleetId ?? "";
            if (string.IsNullOrWhiteSpace(pfid)) continue;

            if (p.Status == ProgramStatus.Running && affectedFleets.Contains(pfid))
            {
                p.Status = ProgramStatus.Paused;
            }
        }
    }
    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Programs is null) return;
        if (state.Programs.Instances.Count == 0) return;

        var tick = state.Tick;

        ApplyManualOverrideInteractions(state, tick);

        // Deterministic ordering by program id
        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var p = kv.Value;
            if (p is null) continue;

            if (!p.IsRunnableAt(tick)) continue;

            // Execute: emit intents only, never mutate ledgers directly.
            var qty = p.Quantity;

            // Execute: emit intents only, never mutate ledgers directly.

            if (string.Equals(p.Kind, ProgramKind.ConstrCapModuleV0, StringComparison.Ordinal))
            {
                // Construction program v0:
                // Drive the minimal construction pipeline by supplying missing stage inputs to the site market.
                // Determinism:
                // - Program iteration is by program id (already sorted).
                // - All lookups are ordinal-keyed.
                // - At most one intent emitted per runnable program per tick.

                var siteId = p.SiteId ?? "";
                if (!string.IsNullOrWhiteSpace(siteId) && state.IndustrySites is not null &&
                    state.IndustrySites.TryGetValue(siteId, out var site) && site is not null)
                {
                    if (site.Active && site.ConstructionEnabled)
                    {
                        var marketId = site.NodeId ?? "";
                        if (!string.IsNullOrWhiteSpace(marketId) &&
                            state.Markets is not null && state.Markets.TryGetValue(marketId, out var market) && market is not null)
                        {
                            var build = state.GetOrCreateIndustryBuildState(siteId);

                            // Mirror IndustrySystem clamping to avoid divergent stage interpretation.
                            if (build.StageIndex < IndustryTweaksV0.Zero) build.StageIndex = IndustryTweaksV0.Stage0;
                            if (build.StageIndex > IndustryTweaksV0.Stage1) build.StageIndex = IndustryTweaksV0.Stage1;

                            // If a stage is running, do not interfere.
                            if (build.StageTicksRemaining <= IndustryTweaksV0.Zero)
                            {
                                string inGood;
                                int inQty;

                                if (build.StageIndex == IndustryTweaksV0.Stage0)
                                {
                                    inGood = IndustryTweaksV0.Stage0InGood;
                                    inQty = IndustryTweaksV0.Stage0InQty;
                                }
                                else
                                {
                                    inGood = IndustryTweaksV0.Stage1InGood;
                                    inQty = IndustryTweaksV0.Stage1InQty;
                                }

                                if (inQty > IndustryTweaksV0.Zero && !string.IsNullOrWhiteSpace(inGood))
                                {
                                    var have = 0;
                                    if (market.Inventory is not null && market.Inventory.TryGetValue(inGood, out var haveQty))
                                        have = haveQty;

                                    var missing = inQty - have;

                                    if (missing > IndustryTweaksV0.Zero)
                                    {
                                        // Supply missing input by selling from player cargo to the market.
                                        // The intent pipeline applies the mutation deterministically.
                                        state.EnqueueIntent(new SellIntent(marketId, inGood, missing));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (string.Equals(p.Kind, ProgramKind.ExpeditionV0, StringComparison.Ordinal))
            {
                // GATE.S3_6.EXPEDITION_PROGRAMS.002: EXPEDITION_V0 executor.
                // Ticks down ExpeditionTicksRemaining each cadence step.
                // On completion emits ExpeditionIntentV0 (single-mutation pipeline).
                // ExpeditionSiteId holds the LeadId (key in Intel.Discoveries).
                // FleetId on the program instance is passed to the intent.
                if (p.ExpeditionTicksRemaining <= 0)
                {
                    p.ExpeditionTicksRemaining = IntelTweaksV0.ExpeditionDurationTicks;
                }
                else
                {
                    p.ExpeditionTicksRemaining -= 1;
                    if (p.ExpeditionTicksRemaining <= 0)
                    {
                        var leadId = p.ExpeditionSiteId ?? "";
                        var fleetId = p.FleetId ?? "";
                        if (!string.IsNullOrWhiteSpace(leadId) && !string.IsNullOrWhiteSpace(fleetId))
                        {
                            state.EnqueueIntent(new SimCore.Intents.ExpeditionIntentV0(
                                leadId,
                                SimCore.Intents.ExpeditionKind.Survey,
                                fleetId,
                                state.Tick + 1));
                        }
                        p.ExpeditionTicksRemaining = IntelTweaksV0.ExpeditionDurationTicks;
                    }
                }
            }
            else if (string.Equals(p.Kind, ProgramKind.TradeCharterV0, StringComparison.Ordinal))
            {
                // GATE.S3_6.EXPLOITATION_PACKAGES.002: TRADE_CHARTER_V0 executor.
                // Buy low from SourceMarketId, sell high to MarketId (dest).
                // Budget-bounded via ExploitationTweaksV0. Emits CashDelta(TradePnL) and InventoryDelta tokens.
                // Single-mutation pipeline: all state changes happen in TradeCharterIntentV0.Apply.
                var srcMarket = p.SourceMarketId ?? "";
                var dstMarket = p.MarketId ?? "";
                var buyGood = p.GoodId ?? "";
                var sellGood = p.SellGoodId ?? "";
                if (!string.IsNullOrWhiteSpace(srcMarket) || !string.IsNullOrWhiteSpace(dstMarket))
                {
                    state.EnqueueIntent(new SimCore.Intents.TradeCharterIntentV0(
                        srcMarket, dstMarket, buyGood, sellGood, p.Id));
                }
            }
            else if (string.Equals(p.Kind, ProgramKind.ResourceTapV0, StringComparison.Ordinal))
            {
                // GATE.S3_6.EXPLOITATION_PACKAGES.002: RESOURCE_TAP_V0 executor.
                // Extract and export good from SourceMarketId via IndustrySystem/LogisticsSystem simulation.
                // Emits InventoryDelta(Produced/Unloaded) tokens.
                // Single-mutation pipeline: all state changes happen in ResourceTapIntentV0.Apply.
                var srcMarket = p.SourceMarketId ?? "";
                var extractGood = p.GoodId ?? "";
                if (!string.IsNullOrWhiteSpace(srcMarket) && !string.IsNullOrWhiteSpace(extractGood))
                {
                    state.EnqueueIntent(new SimCore.Intents.ResourceTapIntentV0(
                        srcMarket, extractGood, p.Id));
                }
            }
            else if (string.Equals(p.Kind, ProgramKind.SurveyV0, StringComparison.Ordinal))
            {
                // GATE.T41.SURVEY_PROG.SYSTEM.001: SURVEY_V0 executor.
                // BFS from home node (SiteId) within SurveyRangeHops.
                // Find Seen discoveries matching SurveyFamily, advance to Scanned only.
                // Also generates lightweight trade intel for scanned sites.
                var homeNode = p.SiteId ?? "";
                var family = p.SurveyFamily ?? "";
                int range = p.SurveyRangeHops;
                if (range <= 0) range = Tweaks.SurveyProgramTweaksV0.SurveyRangeHops;

                if (!string.IsNullOrWhiteSpace(homeNode) && !string.IsNullOrWhiteSpace(family)
                    && state.Intel?.Discoveries is not null)
                {
                    // BFS to find nodes within range.
                    var visited = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                    var queue = new System.Collections.Generic.Queue<(string nodeId, int depth)>();
                    visited.Add(homeNode);
                    queue.Enqueue((homeNode, 0));

                    while (queue.Count > 0)
                    {
                        var (nodeId, depth) = queue.Dequeue();
                        if (depth >= range) continue;

                        foreach (var edge in state.Edges.Values)
                        {
                            string? neighbor = null;
                            if (string.Equals(edge.FromNodeId, nodeId, StringComparison.Ordinal))
                                neighbor = edge.ToNodeId;
                            else if (string.Equals(edge.ToNodeId, nodeId, StringComparison.Ordinal))
                                neighbor = edge.FromNodeId;

                            if (neighbor != null && visited.Add(neighbor))
                                queue.Enqueue((neighbor, depth + 1));
                        }
                    }

                    // Find Seen discoveries in range matching family, advance to Scanned.
                    var discoveryKeys = new System.Collections.Generic.List<string>(state.Intel.Discoveries.Keys);
                    discoveryKeys.Sort(StringComparer.Ordinal);

                    foreach (var discKey in discoveryKeys)
                    {
                        if (!state.Intel.Discoveries.TryGetValue(discKey, out var disc)) continue;
                        if (disc.Phase != Entities.DiscoveryPhase.Seen) continue;

                        // Check if this discovery is in range.
                        string discNodeId = "";
                        foreach (var nodeKvp in state.Nodes)
                        {
                            var node = nodeKvp.Value;
                            if (node?.SeededDiscoveryIds is null) continue;
                            if (node.SeededDiscoveryIds.Contains(discKey))
                            {
                                discNodeId = node.Id ?? "";
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(discNodeId) || !visited.Contains(discNodeId)) continue;

                        // Check family match.
                        string kind = SimCore.Systems.DiscoveryOutcomeSystem.ParseDiscoveryKind(discKey);
                        if (!string.Equals(kind, family, StringComparison.Ordinal)
                            && !string.Equals(SimCore.Systems.DiscoveryOutcomeSystem.GenerateFlavorText(family, Entities.DiscoveryPhase.Seen, "x"), family, StringComparison.Ordinal))
                        {
                            // Also check via MapKindToFamily-style matching.
                            string discFamily = kind switch
                            {
                                "RESOURCE_POOL_MARKER" => "RUIN",
                                "CORRIDOR_TRACE" => "SIGNAL",
                                _ => kind
                            };
                            if (!string.Equals(discFamily, family, StringComparison.Ordinal)
                                && !string.Equals(kind, family, StringComparison.Ordinal))
                                continue;
                        }

                        // Advance to Scanned (Phase 1 -> Phase 2 stays manual).
                        disc.Phase = Entities.DiscoveryPhase.Scanned;
                    }
                }
            }
            else if (string.Equals(p.Kind, ProgramKind.FractureExtractionV0, StringComparison.Ordinal))
            {
                // GATE.EXTRACT.FRACTURE_PROGRAM.001: FRACTURE_EXTRACTION_V0 executor.
                // Requires FractureUnlocked + fleet assignment. Emits FractureExtractionIntentV0.
                var fleetId = p.FleetId ?? "";
                if (!string.IsNullOrWhiteSpace(fleetId))
                {
                    state.EnqueueIntent(new SimCore.Intents.FractureExtractionIntentV0(fleetId, p.Id));
                }
            }
            else if (qty > 0 && !string.IsNullOrWhiteSpace(p.MarketId) && !string.IsNullOrWhiteSpace(p.GoodId))
            {
                if (string.Equals(p.Kind, ProgramKind.AutoBuy, StringComparison.Ordinal))
                {
                    state.EnqueueIntent(new BuyIntent(p.MarketId, p.GoodId, qty));
                }
                else if (string.Equals(p.Kind, ProgramKind.AutoSell, StringComparison.Ordinal))
                {
                    state.EnqueueIntent(new SellIntent(p.MarketId, p.GoodId, qty));
                }
            }

            p.LastRunTick = tick;

            // Prevent runaway loops if cadence is invalid
            var cadence = p.CadenceTicks <= 0 ? 1 : p.CadenceTicks;
            p.NextRunTick = checked(tick + cadence);
        }
    }

    // GATE.T57.PIPELINE.MARGIN_BUFFER.001: Calculate effective margin for a trade route,
    // accounting for intel freshness decay. Returns margin in basis points (10000 = 100%).
    // A raw margin of 1500 bps (15%) with 66% decay becomes 1500 - 1500 = 0 bps.
    public static int CalculateEffectiveMarginBps(SimState state, string sourceNodeId, string destNodeId, string goodId)
    {
        if (state is null || string.IsNullOrEmpty(sourceNodeId) || string.IsNullOrEmpty(destNodeId)) return 0; // STRUCTURAL: null guard

        // Get raw prices.
        if (!state.Markets.TryGetValue(sourceNodeId, out var srcMkt)) return 0; // STRUCTURAL: no market
        if (!state.Markets.TryGetValue(destNodeId, out var dstMkt)) return 0; // STRUCTURAL: no market
        if (string.IsNullOrEmpty(goodId)) return 0; // STRUCTURAL: no good

        int buyPrice = srcMkt.GetBuyPrice(goodId);
        int sellPrice = dstMkt.GetSellPrice(goodId);
        if (buyPrice <= 0) return 0; // STRUCTURAL: no buy price

        // Raw margin in basis points.
        int rawMarginBps = (int)((long)(sellPrice - buyPrice) * 10000 / buyPrice); // STRUCTURAL: bps denominator

        // Find worst freshness decay among EconomicIntels relevant to this route.
        int worstDecayBps = GetWorstFreshnessDecayBps(state, sourceNodeId, destNodeId);

        return rawMarginBps - worstDecayBps;
    }

    // GATE.T57.PIPELINE.MARGIN_BUFFER.001: Get worst freshness decay penalty for a route's intel.
    // Scans EconomicIntels at source or dest node. Returns margin widening in basis points.
    public static int GetWorstFreshnessDecayBps(SimState state, string sourceNodeId, string destNodeId)
    {
        if (state?.Intel?.EconomicIntels is null || state.Intel.EconomicIntels.Count == 0) return 0; // STRUCTURAL: no intel

        int worstDecayBps = 0; // STRUCTURAL: no-decay baseline

        foreach (var kv in state.Intel.EconomicIntels)
        {
            var intel = kv.Value;
            if (intel is null) continue;
            if (!string.Equals(intel.NodeId, sourceNodeId, StringComparison.Ordinal)
                && !string.Equals(intel.NodeId, destNodeId, StringComparison.Ordinal))
                continue;

            int decayBps = ComputeDecayPenaltyBps(state.Tick, intel.CreatedTick, intel.FreshnessMaxTicks);
            if (decayBps > worstDecayBps)
                worstDecayBps = decayBps;
        }

        return worstDecayBps;
    }

    // GATE.T57.PIPELINE.MARGIN_BUFFER.001: Compute margin buffer penalty based on freshness decay.
    // 0-33% elapsed: 0 bps. 33-66%: EarlyBps (500). 66-100%: MidBps (1500). 100%+: LateBps (2500).
    private static int ComputeDecayPenaltyBps(int currentTick, int createdTick, int freshnessMaxTicks)
    {
        if (freshnessMaxTicks <= 0) return 0; // STRUCTURAL: never-decay intel (fracture)

        int elapsed = currentTick - createdTick;
        if (elapsed < 0) elapsed = 0; // STRUCTURAL: guard negative

        int thresholdEarly = freshnessMaxTicks / 3; // STRUCTURAL: 33% threshold
        int thresholdMid = (freshnessMaxTicks * 2) / 3; // STRUCTURAL: 66% threshold

        if (elapsed >= freshnessMaxTicks)
            return EconomicIntelTweaksV0.MarginBufferLateBps;
        if (elapsed >= thresholdMid)
            return EconomicIntelTweaksV0.MarginBufferMidBps;
        if (elapsed >= thresholdEarly)
            return EconomicIntelTweaksV0.MarginBufferEarlyBps;

        return 0; // STRUCTURAL: fresh intel, no penalty
    }
}
