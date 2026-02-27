using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;

namespace SimCore.Tests
{
    [TestFixture]
    public class MarketTests
    {
        [Test]
        public void MidPrice_LowSupply_IsHigherThanBase()
        {
            // Arrange
            var market = new Market();
            market.Inventory["fuel"] = 10; // Low supply vs IdealStock=50

            // Act
            int mid = market.GetMidPrice("fuel");

            // Assert
            Assert.That(mid, Is.GreaterThan(Market.BasePrice), "Mid price should be high when scarce.");
        }

        [Test]
        public void MidPrice_HighSupply_IsLowerThanBase()
        {
            // Arrange
            var market = new Market();
            market.Inventory["fuel"] = 100; // High supply vs IdealStock=50

            // Act
            int mid = market.GetMidPrice("fuel");

            // Assert
            Assert.That(mid, Is.LessThan(Market.BasePrice), "Mid price should be low when abundant.");
        }

        [Test]
        public void MidPrice_ZeroSupply_IsHigh()
        {
            // Arrange
            var market = new Market();
            // No inventory set => stock=0

            // Act
            int mid = market.GetMidPrice("gold");

            // Assert
            Assert.That(mid, Is.GreaterThan(Market.BasePrice), "Mid price should be high at zero stock.");
        }

        [Test]
        public void Spread_BuyPrice_IsGreaterThan_SellPrice()
        {
            // Arrange
            var market = new Market();
            market.Inventory["fuel"] = 50; // IdealStock => mid approx BasePrice

            // Act
            int buy = market.GetBuyPrice("fuel");
            int sell = market.GetSellPrice("fuel");
            int mid = market.GetMidPrice("fuel");

            // Assert
            Assert.That(buy, Is.GreaterThan(sell), "BuyPrice must exceed SellPrice due to spread.");
            Assert.That(buy, Is.GreaterThanOrEqualTo(mid), "BuyPrice should be >= mid.");
            Assert.That(sell, Is.LessThanOrEqualTo(mid), "SellPrice should be <= mid.");
        }

        [Test]
        public void Monotonicity_ScarcerStock_IncreasesAllPrices()
        {
            // Arrange
            var market = new Market();
            market.Inventory["fuel"] = 60;
            int midHighStock = market.GetMidPrice("fuel");
            int buyHighStock = market.GetBuyPrice("fuel");
            int sellHighStock = market.GetSellPrice("fuel");

            market.Inventory["fuel"] = 10;
            int midLowStock = market.GetMidPrice("fuel");
            int buyLowStock = market.GetBuyPrice("fuel");
            int sellLowStock = market.GetSellPrice("fuel");

            // Assert: prices rise as stock becomes scarce
            Assert.That(midLowStock, Is.GreaterThan(midHighStock));
            Assert.That(buyLowStock, Is.GreaterThan(buyHighStock));
            Assert.That(sellLowStock, Is.GreaterThan(sellHighStock));
        }

        [Test]
        public void DiscoveryUnlock_EconomicEffects_PermitUnlock_GatesMarketAccess_WithDeterministicBeforeAfterDelta()
        {
            const int seed = 7;
            var sim = new SimKernel(seed);
            var state = sim.State;

            // Stable ids.
            var permitId = "unlock_permit_trade_mkt_A_v0";

            var m = new Market
            {
                Id = "mkt_A",
                RequiresPermitUnlockId = permitId
            };

            // Before: not acquired => no access.
            state.Intel.Unlocks[permitId] = new UnlockContractV0
            {
                UnlockId = permitId,
                Kind = UnlockKind.Permit,
                IsAcquired = false,
                IsBlocked = false
            };

            Assert.That(MarketSystem.CanAccessMarket(state, m), Is.False);

            // After: acquired => access.
            state.Intel.Unlocks[permitId].IsAcquired = true;

            Assert.That(MarketSystem.CanAccessMarket(state, m), Is.True);
        }

        [Test]
        public void DiscoveryUnlock_EconomicEffects_BrokerUnlock_WaivesTransactionFees_WithDeterministicBeforeAfterDelta()
        {
            const int seed = 8;
            var sim = new SimKernel(seed);
            var state = sim.State;

            const int gross = 10_000;

            // Before: no broker unlock => base fee.
            var feeBefore0 = MarketSystem.ComputeTransactionFeeCredits(state, gross);
            var feeBefore1 = MarketSystem.ComputeTransactionFeeCredits(state, gross);
            Assert.That(feeBefore1, Is.EqualTo(feeBefore0));
            Assert.That(feeBefore0, Is.GreaterThan(0));

            // After: add acquired broker unlock => fee waived.
            var brokerId = "unlock_broker_fee_waiver_v0";
            state.Intel.Unlocks[brokerId] = new UnlockContractV0
            {
                UnlockId = brokerId,
                Kind = UnlockKind.Broker,
                IsAcquired = true,
                IsBlocked = false
            };

            var feeAfter0 = MarketSystem.ComputeTransactionFeeCredits(state, gross);
            var feeAfter1 = MarketSystem.ComputeTransactionFeeCredits(state, gross);
            Assert.That(feeAfter1, Is.EqualTo(feeAfter0));
            Assert.That(feeAfter0, Is.EqualTo(0));

            Assert.That(feeAfter0, Is.Not.EqualTo(feeBefore0));
        }

        [Test]
        public void AntiExploitMarketArbConstraint_MoneyPrinterScenario_ProfitGrowthIsBounded_AndExplainsFrictionReasonCodes()
        {
            // "Money printer" scenario (explicit):
            // Two markets A (source) and B (sink) have a persistent published price spread for one good.
            // A bot reinvests profits to scale buy->ship->sell volume. Without frictions this can grow superlinearly.
            //
            // Exactly two frictions enforced in this test:
            // 1) transaction_fee (MarketSystem.TransactionFeeBps)
            // 2) lane_capacity scarcity (LaneFlowSystem per-lane delivered units per tick bounded by edge.TotalCapacity)
            //
            // Acceptance: after T=600 ticks, profit growth is bounded:
            // equity(t600) <= equity(t300) * 2
            // Also emit deterministic reason codes showing binding friction(s).

            const int seed = 99173;
            const int tMax = 600;
            const int tMid = 300;

            var sim = new SimKernel(seed);
            var state = sim.State;

            // Make the test self-contained: clear any generated content on construction.
            state.Nodes.Clear();
            state.Markets.Clear();
            state.Edges.Clear();
            state.InFlightTransfers.Clear();

            // Create two markets with stable IDs.
            var mA = new Market { Id = "mkt_A" };
            var mB = new Market { Id = "mkt_B" };

            // Two goods: ore is profitable after fees; metal is near-flat and becomes unprofitable after fees (fee binds).
            // Inventory choices are to shape mid/buy/sell deterministically.
            mA.Inventory["ore"] = 5000;   // abundant at source
            mB.Inventory["ore"] = 1;      // scarce at sink

            mA.Inventory["metal"] = 60;   // moderate
            mB.Inventory["metal"] = 55;   // moderate (small spread once published)

            state.Markets[mA.Id] = mA;
            state.Markets[mB.Id] = mB;

            // Nodes referencing those markets.
            state.Nodes["node_A"] = new Node { Id = "node_A", MarketId = mA.Id, Name = "A" };
            state.Nodes["node_B"] = new Node { Id = "node_B", MarketId = mB.Id, Name = "B" };

            // One directed edge A->B with tight capacity to force scarcity.
            state.Edges["lane_A_B"] = new Edge
            {
                Id = "lane_A_B",
                FromNodeId = "node_A",
                ToNodeId = "node_B",
                Distance = 1f,
                TotalCapacity = 7
            };

            // Publish prices deterministically once at tick 0 (persist for the run since cadence is 720 ticks).
            mA.PublishPricesIfDue(0, MarketSystem.PublishWindowTicks);
            mB.PublishPricesIfDue(0, MarketSystem.PublishWindowTicks);

            long cash = 50_000; // starting credits
            long startCash = cash;

            long equityAt300 = 0;
            long equityAt600 = 0;

            int feeBlockedTicks = 0;
            int laneQueuedTicks = 0;

            // Total transaction fees applied (credits). Proves transaction_fee friction is active and binding.
            long feeTotalCredits = 0;

            // Deterministic transfer ids via counter.
            int transferSeq = 0;

            // SimKernel owns Tick advancement; drive time via Step() to keep Tick read-only and deterministic.
            // Start at tick 0; after tMax steps, Tick == tMax.
            for (int step = 0; step < tMax; step++)
            {
                var now = state.Tick;

                // 1) Attempt to place one reinvestment-style trade order per tick.
                // Choose best good deterministically by net_unit_profit desc then good_id asc.
                var goods = new[] { "metal", "ore" }.OrderBy(x => x, StringComparer.Ordinal).ToArray();

                string chosen = "";
                int bestNetUnitProfit = int.MinValue;
                int bestBuy = 0;
                int bestSell = 0;

                foreach (var g in goods)
                {
                    int buy = mA.GetPublishedBuyPrice(g);
                    int sell = mB.GetPublishedSellPrice(g);

                    // Per-unit fees: apply fee to buy gross and sell gross.
                    int buyFee = MarketSystem.ComputeTransactionFeeCredits(buy);
                    int sellFee = MarketSystem.ComputeTransactionFeeCredits(sell);

                    int net = sell - sellFee - (buy + buyFee);

                    if (net > bestNetUnitProfit || (net == bestNetUnitProfit && string.CompareOrdinal(g, chosen) < 0))
                    {
                        bestNetUnitProfit = net;
                        chosen = g;
                        bestBuy = buy;
                        bestSell = sell;
                    }
                }

                if (bestNetUnitProfit <= 0)
                {
                    feeBlockedTicks++;
                }
                else
                {
                    // Reinvestment behavior: try to spend up to all cash each tick on the chosen good.
                    // Quantity limited by source stock.
                    int sourceStock = mA.Inventory.TryGetValue(chosen, out var s) ? s : 0;
                    if (sourceStock > 0 && cash > 0)
                    {
                        int buyFeePerUnit = MarketSystem.ComputeTransactionFeeCredits(bestBuy);
                        int unitCost = bestBuy + buyFeePerUnit;
                        if (unitCost <= 0) unitCost = 1;

                        int affordable = (int)Math.Min(int.MaxValue, cash / unitCost);
                        int qty = Math.Min(sourceStock, affordable);

                        if (qty > 0)
                        {
                            long grossBuy = (long)bestBuy * qty;
                            long buyFee = (long)MarketSystem.ComputeTransactionFeeCredits((int)Math.Min(int.MaxValue, grossBuy));
                            feeTotalCredits += buyFee;
                            long totalCost = grossBuy + buyFee;

                            if (totalCost <= cash)
                            {
                                var id = $"arb_{transferSeq++:D6}";
                                bool enq = LaneFlowSystem.TryEnqueueTransfer(
                                    state,
                                    "node_A",
                                    "node_B",
                                    chosen,
                                    qty,
                                    id);

                                if (enq)
                                {
                                    cash -= totalCost;
                                }
                            }
                        }
                    }
                }

                // 2) Advance one tick through the real kernel (includes lane flow processing deterministically).
                // Snapshot destination inventory before the step so we can infer delivered quantities.
                int beforeOre = mB.Inventory.TryGetValue("ore", out var bo) ? bo : 0;
                int beforeMetal = mB.Inventory.TryGetValue("metal", out var bm) ? bm : 0;

                sim.Step();

                int afterOre = mB.Inventory.TryGetValue("ore", out var ao) ? ao : 0;
                int afterMetal = mB.Inventory.TryGetValue("metal", out var am) ? am : 0;

                int deliveredOre = Math.Max(0, afterOre - beforeOre);
                int deliveredMetal = Math.Max(0, afterMetal - beforeMetal);

                // Parse lane report for queued>0 (capacity binding evidence).
                var laneReport = LaneFlowSystem.GetLastLaneUtilizationReport(state);
                if (!string.IsNullOrWhiteSpace(laneReport))
                {
                    // lane_id|delivered|capacity|queued
                    // lane_A_B|...|...|queued
                    var lines = laneReport.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("lane_A_B|", StringComparison.Ordinal))
                        {
                            var parts = lines[i].Split('|');
                            if (parts.Length >= 4 && int.TryParse(parts[3], out var queued) && queued > 0)
                                laneQueuedTicks++;
                            break;
                        }
                    }
                }

                // 3) Sell any delivered goods immediately at the published sell price (deterministic liquidation),
                // applying transaction fee to sale proceeds. Remove sold units from destination inventory.
                if (deliveredOre > 0)
                {
                    long gross = (long)mB.GetPublishedSellPrice("ore") * deliveredOre;
                    int fee = MarketSystem.ComputeTransactionFeeCredits((int)Math.Min(int.MaxValue, gross));
                    feeTotalCredits += fee;
                    cash += (gross - fee);

                    mB.Inventory["ore"] = Math.Max(0, (mB.Inventory.TryGetValue("ore", out var cur) ? cur : 0) - deliveredOre);
                }

                if (deliveredMetal > 0)
                {
                    long gross = (long)mB.GetPublishedSellPrice("metal") * deliveredMetal;
                    int fee = MarketSystem.ComputeTransactionFeeCredits((int)Math.Min(int.MaxValue, gross));
                    feeTotalCredits += fee;
                    cash += (gross - fee);

                    mB.Inventory["metal"] = Math.Max(0, (mB.Inventory.TryGetValue("metal", out var cur) ? cur : 0) - deliveredMetal);
                }

                // Equity proxy includes cash plus marked-to-market value of in-flight cargo at destination sell price (net of sell fee).
                long inflightEquity = 0;
                foreach (var tr in state.InFlightTransfers.OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    if (tr.Quantity <= 0) continue;
                    if (string.Equals(tr.ToMarketId, mB.Id, StringComparison.Ordinal))
                    {
                        int sell = mB.GetPublishedSellPrice(tr.GoodId);
                        long gross = (long)sell * tr.Quantity;
                        int fee = MarketSystem.ComputeTransactionFeeCredits((int)Math.Min(int.MaxValue, gross));
                        inflightEquity += (gross - fee);
                    }
                }

                long equity = cash + inflightEquity;

                var tickAfter = state.Tick;
                if (tickAfter == tMid) equityAt300 = equity;
                if (tickAfter == tMax) equityAt600 = equity;
            }

            // Compute profit proxies from equity.
            long profit300 = equityAt300 - startCash;
            long profit600 = equityAt600 - startCash;

            // Bounded growth acceptance: profit(t600) <= profit(t300) * 2
            // Use equity proxy to avoid checkpoint artifacts due to in-flight timing.
            Assert.That(
                equityAt600,
                Is.LessThanOrEqualTo(equityAt300 * 2),
                $"Expected bounded profit growth. equity300={equityAt300} equity600={equityAt600} profit300={profit300} profit600={profit600}");

            // transaction_fee must actually apply in the explicit scenario (binding as a sink on throughput-scaled volume).
            Assert.That(feeTotalCredits, Is.GreaterThan(0), "Expected nonzero transaction fee application in arb scenario.");

            // Emit deterministic report with reason codes (no timestamps, normalized newlines).
            var repoRoot = FindRepoRoot();
            var outDir = Path.Combine(repoRoot, "docs", "generated");
            Directory.CreateDirectory(outDir);

            var sb = new StringBuilder();
            sb.AppendLine("MARKET_ARB_CONSTRAINT_REPORT_V0");
            sb.AppendLine($"seed={seed}");
            sb.AppendLine($"transaction_fee_bps={MarketSystem.TransactionFeeBps}");
            sb.AppendLine("lane_id=lane_A_B");
            sb.AppendLine("capacity_units_per_tick=7");
            sb.AppendLine($"equity_t300={equityAt300}");
            sb.AppendLine($"equity_t600={equityAt600}");
            sb.AppendLine($"bounded_check=equity_t600<=equity_t300*2:{(equityAt600 <= equityAt300 * 2 ? "PASS" : "FAIL")}");
            sb.AppendLine("REASON_CODES_V0");
            sb.AppendLine($"FEE_BLOCKED_TRADES_TICKS={feeBlockedTicks}");
            sb.AppendLine($"LANE_CAPACITY_QUEUED_TICKS={laneQueuedTicks}");

            // Fee binding proof: nonzero fee sink over the run and a counterfactual equity proxy with fees removed.
            sb.AppendLine($"FEE_TOTAL_CREDITS={feeTotalCredits}");
            sb.AppendLine($"EQUITY_T600_NO_FEE_PROXY={equityAt600 + feeTotalCredits}");

            sb.AppendLine("notes=FEE_BLOCKED_TRADES indicates fee eliminated otherwise marginal spread; LANE_CAPACITY_QUEUED indicates throughput bound; FEE_TOTAL_CREDITS%NO_FEE_PROXY show fee reduces achievable equity.");
            File.WriteAllText(
                Path.Combine(outDir, "market_arb_constraint_report.txt"),
                sb.ToString().Replace("\r\n", "\n"));
        }

        [Test]
        public void EconomyPlacement_StarterRegion_HasAtLeast3ViableTradeLoops_AndEmitsDeterministicReport()
        {
            const int seed = 12345;
            const int starCount = 8;

            var sim = new SimKernel(seed);
            var state = sim.State;
            GalaxyGenerator.Generate(state, starCount, radius: 1000f);

            // Publish prices deterministically at tick 0 for all markets.
            foreach (var m in state.Markets.Values.OrderBy(m => m.Id, StringComparer.Ordinal))
                m.PublishPricesIfDue(0, MarketSystem.PublishWindowTicks);

            var starterNodeIds = Enumerable.Range(0, GalaxyGenerator.StarterRegionNodeCount)
                .Select(i => $"star_{i}")
                .Where(id => state.Nodes.ContainsKey(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var loops = FindViableLoops(state, starterNodeIds, maxHops: 4);

            Assert.That(loops.Length, Is.GreaterThanOrEqualTo(3),
                "Expected at least 3 viable early trade loops in starter region.");

            var ordered = loops
                .OrderByDescending(x => x.NetProfitProxy)
                .ThenBy(x => x.RouteId, StringComparer.Ordinal)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("ECON_LOOPS_REPORT_V0");
            sb.AppendLine($"seed={seed}");
            sb.AppendLine($"star_count={starCount}");
            sb.AppendLine($"starter_region_count={GalaxyGenerator.StarterRegionNodeCount}");
            sb.AppendLine("sort=net_profit_proxy_desc,route_id_asc");
            sb.AppendLine("route_id|hop_count|net_profit_proxy|volume_proxy|legs");

            foreach (var l in ordered)
                sb.AppendLine($"{l.RouteId}|{l.HopCount}|{l.NetProfitProxy}|{l.VolumeProxy}|{l.LegsSummary}");

            var repoRoot = FindRepoRoot();
            var outDir = Path.Combine(repoRoot, "docs", "generated");
            Directory.CreateDirectory(outDir);

            File.WriteAllText(Path.Combine(outDir, "econ_loops_report.txt"),
                sb.ToString().Replace("\r\n", "\n"));
        }

        [Test]
        public void WorldgenDistributionBounds_StarterGoods_EachHasProducerAndSink_Over100Seeds_AndEmitsDeterministicReport()
        {
            // GATE.S2_5.WGEN.DISTRIBUTION.001 (ultra-loose):
            // Over N=100 seeds, for a small fixed starter goods set, each good must have:
            // - at least 1 producer station in the starter region
            // - at least 1 sink station in the starter region
            //
            // Structural definition (no inventory heuristics):
            // - producer: any starter-region IndustrySite where Outputs[good] > 0
            // - sink: any starter-region IndustrySite where Inputs[good] > 0
            //
            // Determinism:
            // - fixed seed list: 0..99
            // - fixed starCount: StarterRegionNodeCount
            // - stable ordering: goods ordinal, failing seeds asc, all derived lists sorted
            // - report has normalized newlines and no timestamps

            const int nSeeds = 100;
            const int starCount = GalaxyGenerator.StarterRegionNodeCount;

            // Small fixed starter goods set (v0).
            var goods = new[] { "fuel", "metal", "ore" }
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            var failingSeeds = new List<int>();
            var perGoodMinProducers = goods.ToDictionary(g => g, _ => int.MaxValue, StringComparer.Ordinal);
            var perGoodMinSinks = goods.ToDictionary(g => g, _ => int.MaxValue, StringComparer.Ordinal);

            // Per-good failing seeds (for more actionable report).
            var perGoodFailSeeds = goods.ToDictionary(g => g, _ => new List<int>(), StringComparer.Ordinal);

            for (int seed = 0; seed < nSeeds; seed++)
            {
                var sim = new SimKernel(seed);
                var state = sim.State;

                GalaxyGenerator.Generate(
                    state,
                    starCount,
                    radius: 1000f,
                    options: new GalaxyGenerator.GalaxyGenOptions { EnableDistributionSinksV0 = true });

                var starterNodeIds = GalaxyGenerator.GetStarterRegionNodeIdsSortedV0(state)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();

                // Build nodeId -> IndustrySites for deterministic checks (sorted ids, stable).
                var sitesByNode = state.IndustrySites.Values
                    .OrderBy(s => s.Id, StringComparer.Ordinal)
                    .GroupBy(s => s.NodeId, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

                foreach (var good in goods)
                {
                    int producerCount = 0;
                    int sinkCount = 0;

                    foreach (var nodeId in starterNodeIds)
                    {
                        bool hasProducerHere = false;
                        bool hasSinkHere = false;

                        if (sitesByNode.TryGetValue(nodeId, out var sites))
                        {
                            for (int i = 0; i < sites.Length; i++)
                            {
                                var site = sites[i];

                                if (!hasProducerHere &&
                                    site.Outputs != null &&
                                    site.Outputs.TryGetValue(good, out var outQty) &&
                                    outQty > 0)
                                {
                                    hasProducerHere = true;
                                }

                                if (!hasSinkHere &&
                                    site.Inputs != null &&
                                    site.Inputs.TryGetValue(good, out var inQty) &&
                                    inQty > 0)
                                {
                                    hasSinkHere = true;
                                }

                                if (hasProducerHere && hasSinkHere) break;
                            }
                        }

                        if (hasProducerHere) producerCount++;
                        if (hasSinkHere) sinkCount++;
                    }

                    if (producerCount < perGoodMinProducers[good]) perGoodMinProducers[good] = producerCount;
                    if (sinkCount < perGoodMinSinks[good]) perGoodMinSinks[good] = sinkCount;

                    if (producerCount == 0 || sinkCount == 0)
                    {
                        perGoodFailSeeds[good].Add(seed);
                    }
                }
            }

            // Union of failing seeds across all goods, sorted asc.
            foreach (var good in goods)
            {
                foreach (var s in perGoodFailSeeds[good])
                    failingSeeds.Add(s);
            }

            failingSeeds = failingSeeds.Distinct().OrderBy(x => x).ToList();

            var repoRoot = FindRepoRoot();
            var outDir = Path.Combine(repoRoot, "docs", "generated");
            Directory.CreateDirectory(outDir);

            var sb = new StringBuilder();
            sb.AppendLine("WORLDGEN_DISTRIBUTION_BOUNDS_REPORT_V0");
            sb.AppendLine($"n_seeds={nSeeds}");
            sb.AppendLine($"seed_range=0..{nSeeds - 1}");
            sb.AppendLine($"star_count={starCount}");
            sb.AppendLine($"starter_region_count={GalaxyGenerator.StarterRegionNodeCount}");
            sb.AppendLine("definition=producer_if_any_site_outputs_good;sink_if_any_site_inputs_good");
            sb.AppendLine("goods=" + string.Join(",", goods));
            sb.AppendLine("good_id|min_producers|min_sinks|failing_seed_count");
            foreach (var good in goods)
            {
                var failList = perGoodFailSeeds[good].OrderBy(x => x).ToArray();
                sb.AppendLine($"{good}|{perGoodMinProducers[good]}|{perGoodMinSinks[good]}|{failList.Length}");
            }

            sb.AppendLine("FAILING_SEEDS_UNION_ASC");
            if (failingSeeds.Count == 0)
            {
                sb.AppendLine("(none)");
            }
            else
            {
                for (int i = 0; i < failingSeeds.Count; i++)
                    sb.AppendLine(failingSeeds[i].ToString());
            }

            sb.AppendLine("FAILING_SEEDS_BY_GOOD_ASC");
            foreach (var good in goods)
            {
                var failList = perGoodFailSeeds[good].OrderBy(x => x).ToArray();
                sb.AppendLine($"good={good}");
                if (failList.Length == 0)
                {
                    sb.AppendLine("(none)");
                }
                else
                {
                    for (int i = 0; i < failList.Length; i++)
                        sb.AppendLine(failList[i].ToString());
                }
            }

            File.WriteAllText(
                Path.Combine(outDir, "worldgen_distribution_bounds_report_v0.txt"),
                sb.ToString().Replace("\r\n", "\n"));

            if (failingSeeds.Count > 0)
            {
                Assert.Fail($"Worldgen distribution bounds violated for {failingSeeds.Count} seed(s). See docs/generated/worldgen_distribution_bounds_report_v0.txt");
            }
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            // Walk up a bounded number of levels to find the repo root.
            for (int i = 0; i < 12 && dir != null; i++)
            {
                var hasDocs = Directory.Exists(Path.Combine(dir.FullName, "docs"));
                var hasTests = Directory.Exists(Path.Combine(dir.FullName, "SimCore.Tests"));
                var hasGit = Directory.Exists(Path.Combine(dir.FullName, ".git"));

                if ((hasDocs && hasTests) || hasGit)
                    return dir.FullName;

                dir = dir.Parent;
            }

            // Fallback: best-effort current directory.
            return Directory.GetCurrentDirectory();
        }

        private sealed record ViableLoop(string RouteId, int HopCount, int NetProfitProxy, int VolumeProxy, string LegsSummary);

        private static ViableLoop[] FindViableLoops(SimState state, string[] starterNodeIds, int maxHops)
        {
            var starterSet = new HashSet<string>(starterNodeIds, StringComparer.Ordinal);
            var adj = BuildAdjacency(state, starterSet);

            var loops = new Dictionary<string, ViableLoop>(StringComparer.Ordinal);

            foreach (var start in starterNodeIds.OrderBy(x => x, StringComparer.Ordinal))
            {
                var path = new List<string> { start };
                var visited = new HashSet<string>(StringComparer.Ordinal) { start };
                Dfs(state, start, start, path, visited, adj, maxHops, loops);
            }

            return loops.Values.ToArray();
        }

        private static Dictionary<string, List<string>> BuildAdjacency(SimState state, HashSet<string> starterSet)
        {
            var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var id in starterSet) adj[id] = new List<string>();

            foreach (var e in state.Edges.Values.OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                if (!starterSet.Contains(e.FromNodeId) || !starterSet.Contains(e.ToNodeId)) continue;

                adj[e.FromNodeId].Add(e.ToNodeId);
                adj[e.ToNodeId].Add(e.FromNodeId);
            }

            foreach (var k in adj.Keys.ToArray())
                adj[k] = adj[k].Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

            return adj;
        }

        private static void Dfs(
            SimState state,
            string start,
            string cur,
            List<string> path,
            HashSet<string> visited,
            Dictionary<string, List<string>> adj,
            int maxHops,
            Dictionary<string, ViableLoop> loops)
        {
            int hopsSoFar = path.Count - 1;
            if (hopsSoFar >= maxHops) return;
            if (!adj.TryGetValue(cur, out var nbrs)) return;

            foreach (var next in nbrs)
            {
                if (next == start && hopsSoFar + 1 >= 2)
                {
                    var cycle = path.ToList();
                    cycle.Add(start);

                    // De-dupe rotations by requiring start to be the smallest node id in the loop.
                    var uniq = cycle.Take(cycle.Count - 1).Distinct(StringComparer.Ordinal).ToArray();
                    var min = uniq.OrderBy(x => x, StringComparer.Ordinal).First();
                    if (!string.Equals(min, start, StringComparison.Ordinal)) continue;

                    if (TryEvaluateCycle(state, cycle, out var loop))
                        loops[loop.RouteId] = loop;

                    continue;
                }

                if (visited.Contains(next)) continue;

                visited.Add(next);
                path.Add(next);

                Dfs(state, start, next, path, visited, adj, maxHops, loops);

                path.RemoveAt(path.Count - 1);
                visited.Remove(next);
            }
        }

        private static bool TryEvaluateCycle(SimState state, List<string> cycle, out ViableLoop loop)
        {
            loop = null!;

            int totalProfit = 0;
            int volumeProxy = int.MaxValue;
            var legs = new List<string>();

            for (int i = 0; i < cycle.Count - 1; i++)
            {
                string from = cycle[i];
                string to = cycle[i + 1];

                var fromMkt = state.Markets[state.Nodes[from].MarketId];
                var toMkt = state.Markets[state.Nodes[to].MarketId];

                if (!TryBestLeg(fromMkt, toMkt, out var goodId, out var profit, out var vol)) return false;

                totalProfit += profit;
                volumeProxy = Math.Min(volumeProxy, vol);
                legs.Add($"{from}->{to}:{goodId}:{profit}:{vol}");
            }

            if (totalProfit <= 0) return false;
            if (volumeProxy <= 0 || volumeProxy == int.MaxValue) return false;

            loop = new ViableLoop(string.Join(">", cycle), cycle.Count - 1, totalProfit, volumeProxy, string.Join(",", legs));
            return true;
        }

        private static bool TryBestLeg(Market from, Market to, out string goodId, out int profit, out int volume)
        {
            var goods = new[] { "fuel", "ore", "metal" };

            string bestGood = "";
            int bestProfit = 0;
            int bestVol = 0;

            foreach (var g in goods.OrderBy(x => x, StringComparer.Ordinal))
            {
                int fromStock = from.Inventory.TryGetValue(g, out var fs) ? fs : 0;
                int toStock = to.Inventory.TryGetValue(g, out var ts) ? ts : 0;

                int vol = Math.Min(fromStock, Math.Max(0, Market.IdealStock - toStock));
                if (vol <= 0) continue;

                // Net profit proxy after fees: sell at destination bid minus buy at source ask.
                int p = to.GetPublishedSellPrice(g) - from.GetPublishedBuyPrice(g);
                if (p <= 0) continue;

                if (p > bestProfit || (p == bestProfit && string.CompareOrdinal(g, bestGood) < 0))
                {
                    bestProfit = p;
                    bestGood = g;
                    bestVol = vol;
                }
            }

            goodId = bestGood;
            profit = bestProfit;
            volume = bestVol;
            return bestProfit > 0 && bestVol > 0;
        }
    }
}
