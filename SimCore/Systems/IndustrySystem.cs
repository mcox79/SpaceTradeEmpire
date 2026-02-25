using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems
{
    public static class IndustrySystem
    {
        // TIME MODEL: 1 tick = 1 minute game time (GATE.TIME.001 already adopted).
        // Buffering is expressed in days of game time.
        public const int TicksPerDay = 1440;
        public const int Bps = 10000;

        public static int ComputeBufferTargetUnits(IndustrySite site, string goodId)
        {
            if (site is null) throw new ArgumentNullException(nameof(site));
            if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId must be non-empty.", nameof(goodId));

            if (!site.Inputs.TryGetValue(goodId, out var perTick) || perTick <= 0) return 0;

            var days = site.BufferDays;
            if (days < 0) days = 0;

            return checked(perTick * days * TicksPerDay);
        }

        public static void Process(SimState state)
        {
            if (state is null) return;

            // Deterministic ordering: IndustrySites is a Dictionary, so iterate by key.
            foreach (var kv in state.IndustrySites.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var site = kv.Value;
                if (site is null) continue;
                if (!site.Active) continue;
                if (string.IsNullOrWhiteSpace(site.NodeId)) continue;
                if (!state.Markets.TryGetValue(site.NodeId, out var market)) continue;

                // --- Slice: sustainment consumption%production (existing behavior) ---

                // Compute efficiency deterministically as basis points:
                // effBps = min over inputs of floor(available * 10000 / required).
                int effBps = Bps;

                if (site.Inputs.Count > 0)
                {
                    foreach (var input in site.Inputs.OrderBy(i => i.Key, StringComparer.Ordinal))
                    {
                        if (input.Value <= 0) continue;

                        int available = InventoryLedger.Get(market.Inventory, input.Key);
                        int required = input.Value;

                        int ratioBps;
                        if (available <= 0) ratioBps = 0;
                        else ratioBps = (int)Math.Min((long)Bps, ((long)available * Bps) / required);

                        if (ratioBps < effBps) effBps = ratioBps;
                        if (effBps == 0) break;
                    }
                }

                if (effBps < 0) effBps = 0;
                if (effBps > Bps) effBps = Bps;

                site.Efficiency = effBps / (float)Bps;

                // Degrade deterministically when undersupplied.
                ApplyDegradation(site, effBps);

                // If completely starved, we still degrade but we do not consume%produce.
                if (effBps > 0)
                {
                    // Consume inputs (preserve zero keys for markets)
                    foreach (var input in site.Inputs.OrderBy(i => i.Key, StringComparer.Ordinal))
                    {
                        if (input.Value <= 0) continue;

                        int available = InventoryLedger.Get(market.Inventory, input.Key);
                        int targetConsume = (int)(((long)input.Value * effBps) / Bps);
                        int consume = Math.Min(available, targetConsume);

                        if (consume > 0)
                        {
                            InventoryLedger.TryRemoveMarket(market.Inventory, input.Key, consume);
                        }
                        else
                        {
                            if (!market.Inventory.ContainsKey(input.Key)) market.Inventory[input.Key] = 0;
                        }
                    }

                    // Produce outputs (preserve zero keys for markets)
                    // Bounded structure rule: do not produce a good that is also an input good.
                    foreach (var output in site.Outputs.OrderBy(o => o.Key, StringComparer.Ordinal))
                    {
                        if (output.Value <= 0) continue;
                        if (site.Inputs.ContainsKey(output.Key)) continue;

                        int produced = (int)(((long)output.Value * effBps) / Bps);

                        if (produced > 0)
                        {
                            InventoryLedger.AddMarket(market.Inventory, output.Key, produced);
                        }
                        else
                        {
                            if (!market.Inventory.ContainsKey(output.Key)) market.Inventory[output.Key] = 0;
                        }
                    }

                    // Produce byproducts (preserve zero keys for markets)
                    // Deterministic precedence: byproducts never override primary outputs; byproducts never produce input goods.
                    // Tweak-routing guard: use tweaks-routed zero token for any zero comparisons%writes introduced by this block.
                    var zero = IndustryTweaksV0.Zero;

                    foreach (var byp in site.Byproducts.OrderBy(b => b.Key, StringComparer.Ordinal))
                    {
                        if (byp.Value <= zero) continue;
                        if (site.Inputs.ContainsKey(byp.Key)) continue;
                        if (site.Outputs.ContainsKey(byp.Key)) continue;

                        int produced = (int)(((long)byp.Value * effBps) / Bps);

                        if (produced > zero)
                        {
                            InventoryLedger.AddMarket(market.Inventory, byp.Key, produced);
                        }
                        else
                        {
                            if (!market.Inventory.ContainsKey(byp.Key)) market.Inventory[byp.Key] = zero;
                        }
                    }
                }

                // --- Slice: minimal construction pipeline v0 (deterministic, schema-bound by contract file) ---
                // Opt-in only to preserve baseline worlds and goldens.
                if (site.ConstructionEnabled)
                {
                    ProcessMinimalConstructionV0(state, siteId: kv.Key, market);
                }
            }
        }

        private static void ProcessMinimalConstructionV0(SimState state, string siteId, Market market)
        {
            // Recipe: CAP_MODULE_V0
            // All numeric tokens are sourced from Tweaks (see SimCore/Tweaks/IndustryTweaksV0.cs) to satisfy the guard.
            var recipeId = IndustryTweaksV0.RecipeId;

            var build = state.GetOrCreateIndustryBuildState(siteId);
            build.RecipeId = recipeId;

            if (!build.Active)
            {
                // Auto-start for minimal loop v0 (only reachable when site.ConstructionEnabled is true).
                build.Active = true;
                build.StageIndex = IndustryTweaksV0.Stage0;
                build.StageTicksRemaining = IndustryTweaksV0.Zero;
                build.BlockerReason = "";
                build.SuggestedAction = "";
            }

            if (build.StageIndex < IndustryTweaksV0.Zero) build.StageIndex = IndustryTweaksV0.Stage0;
            if (build.StageIndex > IndustryTweaksV0.Stage1) build.StageIndex = IndustryTweaksV0.Stage1;

            string stageName;
            string inGood;
            int inQty;
            int duration;
            string outGood;
            int outQty;

            if (build.StageIndex == IndustryTweaksV0.Stage0)
            {
                stageName = IndustryTweaksV0.Stage0Name;
                inGood = IndustryTweaksV0.Stage0InGood;
                inQty = IndustryTweaksV0.Stage0InQty;
                duration = IndustryTweaksV0.Stage0DurationTicks;
                outGood = IndustryTweaksV0.Stage0OutGood;
                outQty = IndustryTweaksV0.Stage0OutQty;
            }
            else
            {
                stageName = IndustryTweaksV0.Stage1Name;
                inGood = IndustryTweaksV0.Stage1InGood;
                inQty = IndustryTweaksV0.Stage1InQty;
                duration = IndustryTweaksV0.Stage1DurationTicks;
                outGood = IndustryTweaksV0.Stage1OutGood;
                outQty = IndustryTweaksV0.Stage1OutQty;
            }

            build.StageName = stageName;

            if (build.StageTicksRemaining > IndustryTweaksV0.Zero)
            {
                // Deterministic countdown.
                build.StageTicksRemaining = build.StageTicksRemaining - IndustryTweaksV0.One;
                if (build.StageTicksRemaining == IndustryTweaksV0.Zero)
                {
                    // Stage completes this tick.
                    if (outQty > IndustryTweaksV0.Zero)
                    {
                        InventoryLedger.AddMarket(market.Inventory, outGood, outQty);
                    }
                    else
                    {
                        if (!market.Inventory.ContainsKey(outGood)) market.Inventory[outGood] = IndustryTweaksV0.Zero;
                    }

                    state.EmitIndustryEvent($"site={siteId} recipe={recipeId} stage={stageName} evt=complete out={outGood}:{outQty}");

                    if (build.StageIndex == IndustryTweaksV0.Stage1)
                    {
                        // Loop repeats for minimal loop v0.
                        build.StageIndex = IndustryTweaksV0.Stage0;
                        build.StageTicksRemaining = IndustryTweaksV0.Zero;
                        build.BlockerReason = "";
                        build.SuggestedAction = "";
                        state.EmitIndustryEvent($"site={siteId} recipe={recipeId} evt=loop_reset");
                    }
                    else
                    {
                        build.StageIndex = build.StageIndex + IndustryTweaksV0.One;
                        build.StageTicksRemaining = IndustryTweaksV0.Zero;
                        build.BlockerReason = "";
                        build.SuggestedAction = "";
                        state.EmitIndustryEvent($"site={siteId} recipe={recipeId} evt=advance next_stage={build.StageIndex}");
                    }
                }
                return;
            }

            // Stage not currently running: check inputs and start deterministically.
            int have = InventoryLedger.Get(market.Inventory, inGood);
            if (have < inQty)
            {
                build.BlockerReason = $"missing_input good={inGood} need={inQty} have={have}";
                build.SuggestedAction = $"acquire good={inGood}";
                state.EmitIndustryEvent($"site={siteId} recipe={recipeId} stage={stageName} evt=blocked {build.BlockerReason}");
                return;
            }

            // Consume on start.
            InventoryLedger.TryRemoveMarket(market.Inventory, inGood, inQty);
            build.BlockerReason = "";
            build.SuggestedAction = "";
            build.StageTicksRemaining = duration;

            state.EmitIndustryEvent($"site={siteId} recipe={recipeId} stage={stageName} evt=start in={inGood}:{inQty} dur={duration}");
        }

        private static void ApplyDegradation(IndustrySite site, int effBps)
        {
            if (site.DegradePerDayBps <= 0) return;
            if (site.HealthBps <= 0) return;

            int deficitBps = Bps - effBps;
            if (deficitBps <= 0) return;

            // Health loss per day at full deficit is DegradePerDayBps.
            // Per tick health loss = DegradePerDayBps * deficitBps / (TicksPerDay * 10000).
            long numer = (long)site.DegradePerDayBps * deficitBps;
            long denom = (long)TicksPerDay * Bps;

            site.DegradeRemainder = checked(site.DegradeRemainder + numer);

            int dec = (int)(site.DegradeRemainder / denom);
            site.DegradeRemainder = site.DegradeRemainder % denom;

            if (dec <= 0) return;

            site.HealthBps = Math.Max(0, site.HealthBps - dec);
        }
    }
}
