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
}
