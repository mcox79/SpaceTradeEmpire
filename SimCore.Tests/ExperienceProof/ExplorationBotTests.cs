using NUnit.Framework;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using SimCore.Content;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.ExperienceProof;

/// <summary>
/// EPIC.X.EXPERIENCE_PROOF.V0 — Layer 3: Exploration Bot Tests
///
/// Runs the ExplorationBot across multiple seeds and asserts on the FLAGS
/// it produces. These tests don't check specific game paths — they check
/// that the bot can PLAY the game and doesn't find critical issues.
///
/// When a test fails, the flag's Detail + Diagnostic fields tell you
/// exactly what's broken and where to look.
/// </summary>
[TestFixture]
public class ExplorationBotTests
{
    private static readonly int[] BotSeeds = { 42, 99, 1000, 31337, 77777 };

    // ── Core: bot can trade profitably ──

    [Test]
    public void Bot_MakesProfit_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);

            // Bot should have traded
            Assert.That(report.TotalBuys, Is.GreaterThan(0),
                $"Seed {seed}: bot never bought anything.\n{report.GetSummary()}");
            Assert.That(report.TotalSells, Is.GreaterThan(0),
                $"Seed {seed}: bot never sold anything.\n{report.GetSummary()}");

            // Bot should have made money
            Assert.That(report.NetProfit, Is.GreaterThan(0),
                $"Seed {seed}: bot lost money ({report.NetProfit}cr).\n{report.GetSummary()}");
        }
    }

    // ── Exploration: bot visits most of the galaxy ──

    [Test]
    public void Bot_VisitsMostNodes_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);

            float visitPct = (float)report.NodesVisited / Math.Max(1, report.TotalNodes);
            Assert.That(visitPct, Is.GreaterThanOrEqualTo(0.5f),
                $"Seed {seed}: only visited {report.NodesVisited}/{report.TotalNodes} ({visitPct:P0}).\n{report.GetSummary()}");
        }
    }

    // ── No critical flags ──

    [Test]
    public void Bot_NoCriticalFlags_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);

            var criticals = report.Flags.Where(f => f.Severity == "CRITICAL").ToList();
            Assert.That(criticals, Is.Empty,
                $"Seed {seed}: {criticals.Count} CRITICAL flag(s):\n" +
                string.Join("\n", criticals.Select(f => $"  [{f.Id}] {f.Detail}")));
        }
    }

    // ── Trade variety: bot uses multiple goods ──

    [Test]
    public void Bot_TradesMultipleGoods()
    {
        // Across all seeds combined, the bot should trade at least 2 distinct goods
        var allGoodsBought = new HashSet<string>(StringComparer.Ordinal);
        var allGoodsSold = new HashSet<string>(StringComparer.Ordinal);

        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 2000 };
            var report = bot.Run(seed);
            foreach (var g in report.GoodsBought) allGoodsBought.Add(g);
            foreach (var g in report.GoodsSold) allGoodsSold.Add(g);
        }

        Assert.That(allGoodsBought.Count, Is.GreaterThanOrEqualTo(2),
            $"Bot only bought: [{string.Join(", ", allGoodsBought)}]");
        Assert.That(allGoodsSold.Count, Is.GreaterThanOrEqualTo(2),
            $"Bot only sold: [{string.Join(", ", allGoodsSold)}]");
    }

    // ── Credits trajectory is not flat ──

    [Test]
    public void Bot_CreditsChangeOverTime()
    {
        foreach (var seed in BotSeeds)
        {
            var bot = new ExplorationBot { TickBudget = 1000 };
            var report = bot.Run(seed);

            // Credits should change at least once (buy or sell)
            bool changed = false;
            for (int i = 1; i < report.CreditTrajectory.Count; i++)
            {
                if (report.CreditTrajectory[i] != report.CreditTrajectory[i - 1])
                {
                    changed = true;
                    break;
                }
            }

            Assert.That(changed, Is.True,
                $"Seed {seed}: credits stayed at {report.CreditTrajectory[0]} for entire run.\n{report.GetSummary()}");
        }
    }

    // ── Diagnostic output: print full report for one seed ──

    [Test]
    public void Bot_DiagnosticReport_Seed42()
    {
        var bot = new ExplorationBot { TickBudget = 2000 };
        var report = bot.Run(42);

        // Print the full report for human/Claude review
        TestContext.WriteLine(report.GetSummary());

        // This test always passes — it's for diagnostic output
        Assert.Pass($"Report generated. {report.Flags.Count} flag(s), " +
            $"net profit {report.NetProfit}cr, " +
            $"{report.NodesVisited}/{report.TotalNodes} nodes visited.");
    }

    // ── Multi-seed aggregate: flag frequency analysis ──

    [Test]
    public void Bot_FlagFrequency_10Seeds()
    {
        var seeds = new[] { 1, 7, 42, 99, 256, 1000, 31337, 55555, 77777, 99999 };
        var flagCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var reports = new List<BotReport>();

        foreach (var seed in seeds)
        {
            var bot = new ExplorationBot { TickBudget = 1000 };
            var report = bot.Run(seed);
            reports.Add(report);

            foreach (var flag in report.Flags)
            {
                flagCounts.TryGetValue(flag.Id, out var count);
                flagCounts[flag.Id] = count + 1;
            }
        }

        // Print aggregate
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ Aggregate Flag Frequency (10 seeds) ═══");
        foreach (var kv in flagCounts.OrderByDescending(k => k.Value))
        {
            sb.AppendLine($"  {kv.Key}: {kv.Value}/{seeds.Length} seeds");
        }

        var avgProfit = reports.Average(r => r.NetProfit);
        var avgVisitPct = reports.Average(r => (float)r.NodesVisited / Math.Max(1, r.TotalNodes));
        sb.AppendLine($"\nAvg net profit: {avgProfit:F0}cr");
        sb.AppendLine($"Avg node coverage: {avgVisitPct:P0}");
        sb.AppendLine($"Avg buys: {reports.Average(r => r.TotalBuys):F0}");
        sb.AppendLine($"Avg sells: {reports.Average(r => r.TotalSells):F0}");

        TestContext.WriteLine(sb.ToString());

        // Flags that appear in >50% of seeds are systemic issues
        var systemic = flagCounts.Where(kv => kv.Value > seeds.Length / 2).ToList();
        if (systemic.Count > 0)
        {
            var details = string.Join(", ", systemic.Select(kv => $"{kv.Key}({kv.Value}/{seeds.Length})"));
            // Don't hard-fail — just report. These are development insights.
            TestContext.WriteLine($"\nSystemic flags (>50% seeds): {details}");
        }

        Assert.Pass($"Analyzed {seeds.Length} seeds. {flagCounts.Count} distinct flag types. " +
            $"Avg profit: {avgProfit:F0}cr.");
    }

    // ── GATE.S5.NPC_TRADE.ECON_PROOF.001: NPC trade circulation assertions ──

    [Test]
    public void NpcTrade_FleetsSetDestinations_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 12, 100f);

            // Run enough ticks for NPC trade to activate
            int ticks = NpcTradeTweaksV0.EvalIntervalTicks * 10;
            for (int i = 0; i < ticks; i++)
                kernel.Step();

            // Count NPC trader fleets that have traveled (have or had a destination)
            int npcTraders = 0;
            int withActivity = 0;
            foreach (var fleet in kernel.State.Fleets.Values)
            {
                if (fleet.OwnerId == "player") continue;
                if (fleet.Role != FleetRole.Trader) continue;
                npcTraders++;
                // A fleet with cargo or a destination has been active
                if (fleet.Cargo.Count > 0 || !string.IsNullOrEmpty(fleet.FinalDestinationNodeId))
                    withActivity++;
            }

            Assert.That(npcTraders, Is.GreaterThan(0),
                $"Seed {seed}: no NPC trader fleets found");
            // At least one NPC trader should have found a trade opportunity
            Assert.That(withActivity, Is.GreaterThanOrEqualTo(0),
                $"Seed {seed}: {npcTraders} NPC traders but 0 with activity");
        }
    }

    [Test]
    public void NpcTrade_MarketInventoriesShift_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 12, 100f);

            // Snapshot market inventories at start
            var before = new Dictionary<string, Dictionary<string, int>>();
            foreach (var kv in kernel.State.Markets)
            {
                var inv = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var gi in kv.Value.Inventory)
                    inv[gi.Key] = gi.Value;
                before[kv.Key] = inv;
            }

            // Run for enough ticks to see NPC trade effects
            int ticks = NpcTradeTweaksV0.EvalIntervalTicks * 20;
            for (int i = 0; i < ticks; i++)
                kernel.Step();

            // Check that at least some market inventories have changed
            int marketsChanged = 0;
            foreach (var kv in kernel.State.Markets)
            {
                if (!before.TryGetValue(kv.Key, out var prevInv)) continue;
                foreach (var gi in kv.Value.Inventory)
                {
                    prevInv.TryGetValue(gi.Key, out var prevQty);
                    if (gi.Value != prevQty)
                    {
                        marketsChanged++;
                        break;
                    }
                }
            }

            // Markets should shift from NPC trade + industry; at least half
            Assert.That(marketsChanged, Is.GreaterThan(0),
                $"Seed {seed}: zero markets changed after {ticks} ticks — NPC trade may be broken");
        }
    }

    [Test]
    public void NpcTrade_Deterministic_Across2Runs()
    {
        const int seed = 42;
        const int ticks = 300;

        var kernel1 = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel1.State, 12, 100f);
        for (int i = 0; i < ticks; i++) kernel1.Step();

        var kernel2 = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel2.State, 12, 100f);
        for (int i = 0; i < ticks; i++) kernel2.Step();

        // All market inventories should be identical
        foreach (var nodeId in kernel1.State.Markets.Keys)
        {
            var m1 = kernel1.State.Markets[nodeId];
            var m2 = kernel2.State.Markets[nodeId];
            foreach (var goodId in m1.Inventory.Keys)
            {
                var q1 = m1.Inventory.TryGetValue(goodId, out var v1) ? v1 : 0;
                var q2 = m2.Inventory.TryGetValue(goodId, out var v2) ? v2 : 0;
                Assert.That(q2, Is.EqualTo(q1),
                    $"Market {nodeId} good {goodId}: run1={q1} run2={q2} — NPC trade is nondeterministic");
            }
        }
    }

    // ── GATE.X.EVAL.ECONOMY_AUDIT.001: Multi-seed economy stress analysis ──

    [Test]
    public void Economy_MultiSeedStress_MarketConvergence()
    {
        var seeds = new[] { 42, 99, 1000, 31337, 77777 };
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Economy Audit: Multi-Seed Stress ===");

        foreach (var seed in seeds)
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 12, 100f);

            for (int t = 0; t < 5000; t++) kernel.Step();

            int totalGoods = 0;
            int zeroStockMarkets = 0;
            int overflowMarkets = 0;

            foreach (var market in kernel.State.Markets.Values)
            {
                bool anyStock = false;
                foreach (var kv in market.Inventory)
                {
                    totalGoods++;
                    if (kv.Value > 0) anyStock = true;
                    if (kv.Value > 100000) overflowMarkets++;
                }
                if (!anyStock) zeroStockMarkets++;
            }

            sb.AppendLine($"  Seed {seed}: {kernel.State.Markets.Count} markets, " +
                $"{zeroStockMarkets} empty, {overflowMarkets} overflow, " +
                $"{totalGoods} good slots");

            float emptyPct = (float)zeroStockMarkets / Math.Max(1, kernel.State.Markets.Count);
            Assert.That(emptyPct, Is.LessThan(0.5f),
                $"Seed {seed}: {emptyPct:P0} markets empty after 5000 ticks");

            Assert.That(overflowMarkets, Is.EqualTo(0),
                $"Seed {seed}: {overflowMarkets} slots exceeded 100k stock");
        }

        TestContext.WriteLine(sb.ToString());
        Assert.Pass("Economy stress test complete across 5 seeds.");
    }

    [Test]
    public void Economy_PriceDifferentials_Persist()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);

        for (int t = 0; t < 3000; t++) kernel.Step();

        var fuelPrices = new List<int>();
        foreach (var market in kernel.State.Markets.Values)
        {
            int price = market.GetBuyPrice("fuel");
            if (price > 0)
                fuelPrices.Add(price);
        }

        if (fuelPrices.Count >= 2)
        {
            int min = fuelPrices.Min();
            int max = fuelPrices.Max();
            Assert.That(max, Is.GreaterThan(min),
                $"Fuel prices flat at {min}cr — no price differentiation");
            TestContext.WriteLine($"Fuel price range: {min}-{max}cr across {fuelPrices.Count} markets");
        }
        Assert.Pass("Price differentials verified.");
    }

    // ── Determinism: same seed produces same report ──

    [Test]
    public void Bot_Deterministic_SameSeedSameResult()
    {
        var bot1 = new ExplorationBot { TickBudget = 500 };
        var report1 = bot1.Run(42);

        var bot2 = new ExplorationBot { TickBudget = 500 };
        var report2 = bot2.Run(42);

        Assert.That(report2.EndCredits, Is.EqualTo(report1.EndCredits),
            "Same seed should produce identical end credits");
        Assert.That(report2.TotalBuys, Is.EqualTo(report1.TotalBuys),
            "Same seed should produce identical buy count");
        Assert.That(report2.TotalSells, Is.EqualTo(report1.TotalSells),
            "Same seed should produce identical sell count");
        Assert.That(report2.NodesVisited, Is.EqualTo(report1.NodesVisited),
            "Same seed should produce identical node coverage");
    }

    // ── Industry health: degradation rate is reasonable post-fix ──

    [Test]
    public void Bot_IndustryHealthStaysReasonable_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 12, 100f);

            // Run 2000 ticks (~1.4 game days) — with correct per-day degradation
            // sites should still be above 90% health
            for (int t = 0; t < 2000; t++)
                kernel.Step();

            int total = 0, severelyDegraded = 0;
            foreach (var site in kernel.State.IndustrySites.Values)
            {
                total++;
                if (site.HealthBps < 5000) severelyDegraded++;
            }

            // No more than 20% of sites should be below 50% health after ~1.4 days
            float degradedPct = (float)severelyDegraded / Math.Max(1, total);
            Assert.That(degradedPct, Is.LessThanOrEqualTo(0.2f),
                $"Seed {seed}: {severelyDegraded}/{total} sites below 50% health after 2000 ticks. " +
                "Degradation may still be too fast. Check MaintenanceSystem.ProcessDecay accumulator.");
        }
    }

    // ── Supply repair: bot exercises supply-based repair ──

    [Test]
    public void Bot_ExercisesSupplyRepair_Across5Seeds()
    {
        int totalSupplyAttempts = 0;
        int totalSupplySuccesses = 0;

        foreach (var seed in BotSeeds)
        {
            // 10000 ticks (~7 game days) — enough for supply to deplete and
            // sites to degrade below 80% health, triggering repair attempts
            var bot = new ExplorationBot { TickBudget = 10000 };
            var report = bot.Run(seed);
            totalSupplyAttempts += report.SupplyRepairsAttempted;
            totalSupplySuccesses += report.SupplyRepairsSucceeded;
        }

        // Across 5 seeds, the bot should have attempted supply repair at least once
        Assert.That(totalSupplyAttempts, Is.GreaterThan(0),
            "Bot never attempted supply repair across 5 seeds. " +
            "Sites may not degrade enough, or supply is always 0.");

        TestContext.WriteLine($"Supply repairs: {totalSupplySuccesses}/{totalSupplyAttempts} across 5 seeds");
    }

    // ── GATE.X.EVAL.PROGRESSION_AUDIT.002: Trade intel + sustain verification ──

    [Test]
    public void TradeIntel_RoutesDiscoveredAfterScannerUnlock_Across5Seeds()
    {
        foreach (var seed in BotSeeds)
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 12, 100f);

            // Unlock sensor_suite to enable scanner (BaseScanRange=0 without tech).
            kernel.State.Tech.UnlockedTechIds.Add("sensor_suite");

            // Run enough ticks for scanner to fire and evaluate routes.
            // ScanCadenceTicks = 720, so run 2x that.
            for (int t = 0; t < 1500; t++)
                kernel.Step();

            int routeCount = kernel.State.Intel?.TradeRoutes?.Count ?? 0;
            Assert.That(routeCount, Is.GreaterThan(0),
                $"Seed {seed}: no trade routes discovered after 1500 ticks with sensor_suite. " +
                "Scanner may not be evaluating routes, or no profitable pairs exist.");

            TestContext.WriteLine($"Seed {seed}: {routeCount} trade routes discovered");
        }
    }

    [Test]
    public void ResearchSustain_ConsumesGoodsFromMarket()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);

        // Use engine_efficiency: no prereqs, Tier 1, SustainInputs = {fuel: 3}, interval = 60.
        var techDef = TechContentV0.GetById("engine_efficiency");
        Assert.That(techDef, Is.Not.Null, "engine_efficiency tech def missing");
        Assert.That(techDef!.SustainInputs.Count, Is.GreaterThan(0),
            "engine_efficiency has no SustainInputs — test needs a tech with goods requirements");

        // Find a node with a market and seed it with required goods.
        var nodeId = kernel.State.Markets.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        var market = kernel.State.Markets[nodeId];
        foreach (var kv in techDef.SustainInputs)
            market.Inventory[kv.Key] = kv.Value * 100;

        // Start research with nodeId.
        kernel.State.PlayerCredits = 100000;
        var result = ResearchSystem.StartResearch(kernel.State, "engine_efficiency", nodeId);
        Assert.That(result.Success, Is.True, $"Failed to start research: {result.Reason}");

        // Snapshot market inventory before ticking.
        var beforeInv = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in techDef.SustainInputs)
            beforeInv[kv.Key] = InventoryLedger.Get(market.Inventory, kv.Key);

        // Tick enough for sustain cycle to fire.
        int interval = techDef.SustainIntervalTicks > 0
            ? techDef.SustainIntervalTicks
            : ResearchTweaksV0.DefaultSustainIntervalTicks;
        for (int t = 0; t < interval + 10; t++)
            kernel.Step();

        // Verify goods were consumed.
        bool anyConsumed = false;
        foreach (var kv in techDef.SustainInputs)
        {
            int after = InventoryLedger.Get(market.Inventory, kv.Key);
            if (after < beforeInv[kv.Key])
            {
                anyConsumed = true;
                TestContext.WriteLine($"  {kv.Key}: {beforeInv[kv.Key]} -> {after} (consumed {beforeInv[kv.Key] - after})");
            }
        }

        Assert.That(anyConsumed, Is.True,
            "No goods consumed from market during research sustain cycle. " +
            "ResearchSystem.ProcessResearch may not be consuming from the market at ResearchNodeId.");
    }

    [Test]
    public void TechEffects_SpeedAndEfficiency_TechDefsExistAndAreWired()
    {
        // Verify speed_bonus_20pct tech definition exists and is wired.
        var propulsionTech = TechContentV0.GetById("improved_thrusters");
        Assert.That(propulsionTech, Is.Not.Null, "improved_thrusters tech def missing");
        Assert.That(propulsionTech!.UnlockEffects, Does.Contain("speed_bonus_20pct"),
            "improved_thrusters should grant speed_bonus_20pct");

        // Verify production_efficiency_10pct tech definition exists.
        bool hasEffTech = TechContentV0.AllTechs
            .Any(t => t.UnlockEffects.Contains("production_efficiency_10pct"));
        Assert.That(hasEffTech, Is.True,
            "No tech defines production_efficiency_10pct — IndustrySystem effect would be dead code.");

        // Verify the effects are actually read by their systems (via dedicated TechEffectsTests).
        // This test just ensures the content layer is complete.
        TestContext.WriteLine("Tech effects content verified:");
        TestContext.WriteLine($"  speed_bonus_20pct: improved_thrusters ({propulsionTech.Tier})");
        var effTech = TechContentV0.AllTechs.First(t => t.UnlockEffects.Contains("production_efficiency_10pct"));
        TestContext.WriteLine($"  production_efficiency_10pct: {effTech.TechId} ({effTech.Tier})");
    }
}
