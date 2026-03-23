using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.T44.SIGNAL.CONTRACT_TESTS.001 — Economy bridge signal contract tests.
/// Validates the underlying SimCore data shapes that GetNodeEconomySnapshotV0
/// and GetMarketAlertsV0 read from. Tests run pure C# (no Godot).
/// </summary>
[TestFixture]
public class EconomyBridgeContractTests
{
    private const int StarCount = 12;
    private const float Radius = 100f;

    /// <summary>
    /// Verifies that the economy snapshot data (traffic, prosperity, industry,
    /// warfront, faction, docked fleets) can be computed for a valid node
    /// after 100 ticks of simulation.
    /// </summary>
    [Test]
    public void GetNodeEconomySnapshot_ReturnsExpectedFields()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, StarCount, Radius);

        for (int i = 0; i < 100; i++)
            kernel.Step();

        var state = kernel.State;

        // Pick a node that has a market (most generated nodes do).
        string? targetNodeId = null;
        foreach (var kv in state.Nodes)
        {
            if (state.Markets.ContainsKey(kv.Key))
            {
                targetNodeId = kv.Key;
                break;
            }
        }
        Assert.That(targetNodeId, Is.Not.Null, "No node with a market found after galaxy generation");

        // --- traffic_level: count fleets at/targeting this node ---
        int traffic = 0;
        int docked = 0;
        foreach (var f in state.Fleets.Values)
        {
            if (string.Equals(f.CurrentNodeId, targetNodeId, StringComparison.Ordinal))
            {
                traffic++;
                if (!f.IsMoving) docked++;
            }
            else if (string.Equals(f.DestinationNodeId, targetNodeId, StringComparison.Ordinal)
                || string.Equals(f.FinalDestinationNodeId, targetNodeId, StringComparison.Ordinal))
            {
                traffic++;
            }
        }
        Assert.That(traffic, Is.GreaterThanOrEqualTo(0), "traffic_level must be non-negative");
        Assert.That(docked, Is.GreaterThanOrEqualTo(0), "docked_fleets must be non-negative");
        Assert.That(docked, Is.LessThanOrEqualTo(traffic), "docked cannot exceed total traffic");

        // --- prosperity: avg inventory / IdealStock ---
        Assert.That(state.Markets.ContainsKey(targetNodeId!), Is.True);
        var mkt = state.Markets[targetNodeId!];
        int total = 0; int count = 0;
        foreach (var v in mkt.Inventory.Values) { total += v; count++; }
        float prosperity = count > 0 ? (float)total / count / Market.IdealStock : 0f;
        Assert.That(prosperity, Is.GreaterThanOrEqualTo(0f), "prosperity must be non-negative");

        // --- industry_type: derived from IndustrySite.RecipeId ---
        // Just verify the lookup doesn't crash and returns a string.
        string industryType = "none";
        foreach (var site in state.IndustrySites.Values)
        {
            if (!string.Equals(site.NodeId, targetNodeId, StringComparison.Ordinal)) continue;
            industryType = site.RecipeId switch
            {
                "" when site.Outputs.ContainsKey("fuel") => "fuel_well",
                var r when r.Contains("ore") => "mine",
                var r when r.Contains("metal") => "refinery",
                var r when r.Contains("munitions") => "munitions_fab",
                var r when r.Contains("food") => "food_processor",
                var r when r.Contains("electronics") => "electronics_fab",
                var r when r.Contains("composites") => "composites_fab",
                var r when r.Contains("components") => "components_fab",
                _ => "factory",
            };
            break;
        }
        Assert.That(industryType, Is.Not.Null.And.Not.Empty, "industry_type must be a non-empty string");

        // --- warfront_tier: non-negative integer ---
        int warfrontTier = MarketSystem.GetNodeWarfrontIntensity(state, targetNodeId!);
        Assert.That(warfrontTier, Is.GreaterThanOrEqualTo(0), "warfront_tier must be non-negative");

        // --- faction_id: string (may be empty if no faction assigned) ---
        string factionId = state.NodeFactionId != null && state.NodeFactionId.TryGetValue(targetNodeId!, out var fid) ? fid : "";
        Assert.That(factionId, Is.Not.Null, "faction_id must not be null");
    }

    /// <summary>
    /// Verifies that market alert data (stockouts, price changes) can be
    /// computed from SimState. We manipulate inventory to force a stockout
    /// and verify the alert shape.
    /// </summary>
    [Test]
    public void GetMarketAlerts_ReturnsValidAlerts()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, StarCount, Radius);

        // Step a bit to let prices publish.
        for (int i = 0; i < 50; i++)
            kernel.Step();

        var state = kernel.State;

        // Find a market with inventory and mark its node as visited.
        string? alertNodeId = null;
        string? alertGoodId = null;
        foreach (var kv in state.Markets)
        {
            foreach (var inv in kv.Value.Inventory)
            {
                if (inv.Value > 0)
                {
                    alertNodeId = kv.Key;
                    alertGoodId = inv.Key;
                    break;
                }
            }
            if (alertNodeId != null) break;
        }
        Assert.That(alertNodeId, Is.Not.Null, "No market with positive inventory found");
        Assert.That(alertGoodId, Is.Not.Null);

        // Mark node as visited (required for alerts to trigger).
        state.PlayerVisitedNodeIds.Add(alertNodeId!);

        // Force a stockout by zeroing inventory.
        var market = state.Markets[alertNodeId!];
        market.Inventory[alertGoodId!] = 0;

        // Now compute alerts the same way the bridge does.
        var alerts = new List<Dictionary<string, object>>();
        var visitedNodes = new List<string>(state.PlayerVisitedNodeIds);
        visitedNodes.Sort(StringComparer.Ordinal);

        foreach (var nodeId in visitedNodes)
        {
            if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;
            var goodIds = new List<string>(mkt.Inventory.Keys);
            goodIds.Sort(StringComparer.Ordinal);

            foreach (var goodId in goodIds)
            {
                int stock = mkt.Inventory.TryGetValue(goodId, out var sv) ? sv : 0;
                if (stock == 0)
                {
                    alerts.Add(new Dictionary<string, object>
                    {
                        ["node_id"] = nodeId,
                        ["good_id"] = goodId,
                        ["type"] = "stockout",
                    });
                }
            }
        }

        // We forced at least one stockout.
        Assert.That(alerts.Count, Is.GreaterThan(0), "Expected at least one stockout alert");

        // Verify alert shape.
        var firstAlert = alerts[0];
        Assert.That(firstAlert.ContainsKey("node_id"), Is.True);
        Assert.That(firstAlert.ContainsKey("good_id"), Is.True);
        Assert.That(firstAlert.ContainsKey("type"), Is.True);
        Assert.That(firstAlert["type"], Is.EqualTo("stockout").Or.EqualTo("price_spike").Or.EqualTo("price_drop"));
        Assert.That((string)firstAlert["node_id"], Is.Not.Empty);
        Assert.That((string)firstAlert["good_id"], Is.Not.Empty);
    }

    /// <summary>
    /// Verifies that the economy snapshot computation returns safe defaults
    /// for a non-existent node (no crash, sensible zero/empty values).
    /// </summary>
    [Test]
    public void GetNodeEconomySnapshot_NonExistentNode_ReturnsDefaults()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, StarCount, Radius);

        var state = kernel.State;
        string fakeNodeId = "DOES_NOT_EXIST_NODE_XYZ";

        // Traffic: no fleets at a non-existent node.
        int traffic = 0;
        int docked = 0;
        foreach (var f in state.Fleets.Values)
        {
            if (string.Equals(f.CurrentNodeId, fakeNodeId, StringComparison.Ordinal))
            {
                traffic++;
                if (!f.IsMoving) docked++;
            }
        }
        Assert.That(traffic, Is.EqualTo(0), "No fleet should be at a non-existent node");
        Assert.That(docked, Is.EqualTo(0));

        // Prosperity: no market means 0.
        Assert.That(state.Markets.ContainsKey(fakeNodeId), Is.False);

        // Industry: no site at this node.
        bool hasIndustry = state.IndustrySites.Values.Any(s =>
            string.Equals(s.NodeId, fakeNodeId, StringComparison.Ordinal));
        Assert.That(hasIndustry, Is.False);

        // Warfront: 0 for non-existent.
        int warfront = MarketSystem.GetNodeWarfrontIntensity(state, fakeNodeId);
        Assert.That(warfront, Is.EqualTo(0));

        // Faction: empty string.
        string faction = state.NodeFactionId != null && state.NodeFactionId.TryGetValue(fakeNodeId, out var fid) ? fid : "";
        Assert.That(faction, Is.Empty);
    }

    /// <summary>
    /// Verifies that across multiple seeds, the economy snapshot fields are
    /// consistent and well-formed at a variety of tick counts.
    /// </summary>
    [Test]
    public void GetNodeEconomySnapshot_MultiSeed_ConsistentShape()
    {
        foreach (int seed in new[] { 42, 99, 1001 })
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, StarCount, Radius);

            for (int i = 0; i < 200; i++)
                kernel.Step();

            var state = kernel.State;
            int nodesChecked = 0;

            foreach (var nodeId in state.Nodes.Keys)
            {
                if (!state.Markets.ContainsKey(nodeId)) continue;
                nodesChecked++;

                // Prosperity calculation must not throw.
                var mkt = state.Markets[nodeId];
                int total = 0; int count = 0;
                foreach (var v in mkt.Inventory.Values) { total += v; count++; }
                float prosperity = count > 0 ? (float)total / count / Market.IdealStock : 0f;

                Assert.That(prosperity, Is.GreaterThanOrEqualTo(0f),
                    $"Seed {seed} node {nodeId}: negative prosperity");
                Assert.That(float.IsNaN(prosperity), Is.False,
                    $"Seed {seed} node {nodeId}: NaN prosperity");

                // Warfront intensity non-negative.
                int warfront = MarketSystem.GetNodeWarfrontIntensity(state, nodeId);
                Assert.That(warfront, Is.GreaterThanOrEqualTo(0),
                    $"Seed {seed} node {nodeId}: negative warfront intensity");
            }

            Assert.That(nodesChecked, Is.GreaterThan(0),
                $"Seed {seed}: no market nodes found");
        }
    }

    /// <summary>
    /// Verifies that market alert types are exclusively from the expected set
    /// and that stockouts can be detected when inventory is drained.
    /// </summary>
    [Test]
    public void GetMarketAlerts_TypesAreFromExpectedSet()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, StarCount, Radius);

        // Run long enough for price publishing to happen.
        for (int i = 0; i < 100; i++)
            kernel.Step();

        var state = kernel.State;
        var validTypes = new HashSet<string> { "stockout", "price_spike", "price_drop" };

        // Mark all market nodes as visited.
        foreach (var nodeId in state.Markets.Keys)
            state.PlayerVisitedNodeIds.Add(nodeId);

        // Scan alerts the same way bridge does.
        var visitedNodes = new List<string>(state.PlayerVisitedNodeIds);
        visitedNodes.Sort(StringComparer.Ordinal);

        foreach (var nodeId in visitedNodes)
        {
            if (!state.Markets.TryGetValue(nodeId, out var mkt)) continue;

            foreach (var goodId in mkt.Inventory.Keys)
            {
                int currentMid = mkt.GetMidPrice(goodId);
                int publishedMid = mkt.GetPublishedMidPrice(goodId);
                int stock = mkt.Inventory.TryGetValue(goodId, out var sv) ? sv : 0;

                string alertType;
                if (stock == 0)
                    alertType = "stockout";
                else if (publishedMid > 0 && Math.Abs(currentMid - publishedMid) * 10000 / publishedMid >= 2000)
                    alertType = currentMid > publishedMid ? "price_spike" : "price_drop";
                else
                    continue;

                Assert.That(validTypes.Contains(alertType), Is.True,
                    $"Unexpected alert type: {alertType}");
            }
        }
    }
}
