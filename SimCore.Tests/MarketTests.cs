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
        public void EconomyPlacement_StarterRegion_HasAtLeast3ViableTradeLoops_AndEmitsDeterministicReport()
        {
            const int seed = 12345;
            const int starCount = 8;

            var state = new SimState(seed);
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
