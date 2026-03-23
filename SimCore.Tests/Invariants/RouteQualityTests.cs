using System;
using System.Collections.Generic;
using SimCore;
using SimCore.Gen;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

// GATE.T50.ECON.ROUTE_QUALITY_TEST.001: Monte Carlo route quality test.
// Ensures every seed has at least one profitable trade route within 2 hops of player start.
public class RouteQualityTests
{
    private const int SeedCount = 10;          // STRUCTURAL: number of seeds
    private const int MinProfitableMargin = 10; // STRUCTURAL: minimum margin in cr/unit

    [Test]
    public void EverySeeed_HasProfitableRoute_Within2Hops()
    {
        var failures = new List<string>();

        for (int seedIdx = 0; seedIdx < SeedCount; seedIdx++)
        {
            int seed = 42 + seedIdx * 37; // STRUCTURAL: spread seeds for variety
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 20, 100f);

            var startId = kernel.State.PlayerLocationNodeId;
            if (string.IsNullOrEmpty(startId))
            {
                failures.Add($"Seed {seed}: no PlayerLocationNodeId");
                continue;
            }

            if (!kernel.State.Markets.TryGetValue(startId, out var startMarket))
            {
                failures.Add($"Seed {seed}: no market at start node {startId}");
                continue;
            }

            // Collect nodes within 2 hops.
            var hop1 = new HashSet<string>(StringComparer.Ordinal);
            var within2 = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in kernel.State.Edges.Values)
            {
                if (string.Equals(edge.FromNodeId, startId, StringComparison.Ordinal))
                { hop1.Add(edge.ToNodeId); within2.Add(edge.ToNodeId); }
                else if (string.Equals(edge.ToNodeId, startId, StringComparison.Ordinal))
                { hop1.Add(edge.FromNodeId); within2.Add(edge.FromNodeId); }
            }
            foreach (var h1 in hop1)
            {
                foreach (var edge in kernel.State.Edges.Values)
                {
                    string? candidate = null;
                    if (string.Equals(edge.FromNodeId, h1, StringComparison.Ordinal))
                        candidate = edge.ToNodeId;
                    else if (string.Equals(edge.ToNodeId, h1, StringComparison.Ordinal))
                        candidate = edge.FromNodeId;
                    if (candidate != null && !string.Equals(candidate, startId, StringComparison.Ordinal))
                        within2.Add(candidate);
                }
            }

            // Check if any good at start has profitable margin at any neighbor within 2 hops.
            int bestMargin = int.MinValue;
            string bestGood = "";
            string bestDest = "";
            foreach (var goodId in startMarket.Inventory.Keys)
            {
                int buyPrice = startMarket.GetBuyPrice(goodId);
                foreach (var nid in within2)
                {
                    if (!kernel.State.Markets.TryGetValue(nid, out var nm)) continue;
                    if (!nm.Inventory.ContainsKey(goodId)) continue;
                    int sellPrice = nm.GetSellPrice(goodId);
                    int margin = sellPrice - buyPrice;
                    if (margin > bestMargin)
                    {
                        bestMargin = margin;
                        bestGood = goodId;
                        bestDest = nid;
                    }
                }
            }

            if (bestMargin < MinProfitableMargin)
            {
                failures.Add($"Seed {seed}: best margin={bestMargin} (good={bestGood}, dest={bestDest}), need {MinProfitableMargin}");
            }
        }

        Assert.That(failures, Is.Empty,
            $"Route quality failures:\n{string.Join("\n", failures)}");
    }
}
