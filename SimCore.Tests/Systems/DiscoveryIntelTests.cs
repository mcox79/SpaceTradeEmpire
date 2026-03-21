using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.T41.DISCOVERY_INTEL.SYSTEM.001 + GATE.T41.INTEL_DECAY.SYSTEM.001
[TestFixture]
public sealed class DiscoveryIntelTests
{
    /// <summary>
    /// Build a small graph with markets for intel testing:
    ///   A ── B ── C ── D ── E
    /// Player starts at A. Inventory-driven pricing: low stock = high price.
    /// </summary>
    private SimState MakeIntelGraph()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "A";

        string[] ids = { "A", "B", "C", "D", "E" };
        foreach (var id in ids)
        {
            state.Nodes[id] = new Node { Id = id };
            state.Markets[id] = new Market { Id = id };
        }

        // Inventory-driven pricing: low stock = expensive, high stock = cheap
        // Market.BasePrice=100, IdealStock=50. Stock > ideal = price < base.
        state.Markets["A"].Inventory["ore"] = 80; // cheap (high stock)
        state.Markets["B"].Inventory["ore"] = 60; // somewhat cheap
        state.Markets["C"].Inventory["ore"] = 5;  // expensive (low stock)
        state.Markets["D"].Inventory["ore"] = 70; // cheap
        state.Markets["E"].Inventory["ore"] = 2;  // very expensive

        void AddEdge(string from, string to)
        {
            string eid = $"e_{from}_{to}";
            state.Edges[eid] = new Edge { Id = eid, FromNodeId = from, ToNodeId = to, Distance = 5f };
        }

        AddEdge("A", "B");
        AddEdge("B", "C");
        AddEdge("C", "D");
        AddEdge("D", "E");

        state.Fleets["fleet_player"] = new Fleet { Id = "fleet_player", OwnerId = "player", Speed = 1.0f };

        return state;
    }

    [Test]
    public void AnalyzedDiscovery_GeneratesTradeRoute_WithSourceDiscoveryId()
    {
        var state = MakeIntelGraph();

        // Seed a RESOURCE_POOL_MARKER discovery at node B
        var discId = "disc_v0|RESOURCE_POOL_MARKER|B|ore|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Analyzed
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        // Process outcome system to generate intel
        DiscoveryOutcomeSystem.Process(state);

        // Should have created a discovery-derived trade route
        var discoveryRoutes = state.Intel.TradeRoutes.Values
            .Where(r => !string.IsNullOrEmpty(r.SourceDiscoveryId))
            .ToList();

        Assert.That(discoveryRoutes.Count, Is.GreaterThanOrEqualTo(1),
            "Analyzed discovery should generate at least one trade route");

        var route = discoveryRoutes[0];
        Assert.That(route.SourceDiscoveryId, Is.Not.Empty);
        Assert.That(route.GoodId, Is.EqualTo("ore"));
        Assert.That(route.EstimatedProfitPerUnit, Is.GreaterThan(0));
    }

    [Test]
    public void DiscoveryRoute_BelowMinProfit_NotCreated()
    {
        var state = MakeIntelGraph();

        // Set all markets to same stock = same price = 0 profit
        state.Markets["A"].Inventory["ore"] = 50;
        state.Markets["B"].Inventory["ore"] = 50;
        state.Markets["C"].Inventory["ore"] = 50;

        var discId = "disc_v0|RESOURCE_POOL_MARKER|B|ore|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Analyzed
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        DiscoveryOutcomeSystem.Process(state);

        var discoveryRoutes = state.Intel.TradeRoutes.Values
            .Where(r => !string.IsNullOrEmpty(r.SourceDiscoveryId))
            .ToList();

        Assert.That(discoveryRoutes.Count, Is.EqualTo(0),
            "Same-price discoveries should not generate routes (zero profit)");
    }

    [Test]
    public void DiscoveryRouteDecay_NearBand_DecaysAtConfiguredRate()
    {
        var state = MakeIntelGraph();

        // Manually create a discovery-derived route at a near node (B is 1 hop from player at A)
        var route = new TradeRouteIntel
        {
            RouteId = "route_test_1",
            SourceNodeId = "B",
            DestNodeId = "C",
            GoodId = "ore",
            EstimatedProfitPerUnit = 10,
            DiscoveredTick = 0,
            LastValidatedTick = 0,
            Status = TradeRouteStatus.Discovered,
            SourceDiscoveryId = "disc_v0|RESOURCE_POOL_MARKER|B|ore|src1"
        };
        state.Intel.TradeRoutes["route_test_1"] = route;

        // Advance past Near decay threshold (50 ticks)
        for (int i = 0; i < DiscoveryIntelTweaksV0.NearDecayTicks + 1; i++)
            state.AdvanceTick();

        // Run intel system to apply decay
        IntelSystem.EvaluateTradeRoutes(state);

        Assert.That(route.Status, Is.EqualTo(TradeRouteStatus.Stale),
            $"Near-band route should be Stale after {DiscoveryIntelTweaksV0.NearDecayTicks} ticks");
    }

    [Test]
    public void DiscoveryRouteDecay_DeepBand_TakesLongerToDecay()
    {
        var state = MakeIntelGraph();

        // Route at E (4 hops from A → Mid band 3-5 hops, decay at 150 ticks)
        // SourceNodeId is used for BFS distance — set it to E.
        var route = new TradeRouteIntel
        {
            RouteId = "route_deep_1",
            SourceNodeId = "E",
            DestNodeId = "D",
            GoodId = "ore",
            EstimatedProfitPerUnit = 10,
            DiscoveredTick = 0,
            LastValidatedTick = 0,
            Status = TradeRouteStatus.Active,
            SourceDiscoveryId = "disc_v0|RESOURCE_POOL_MARKER|E|ore|src2"
        };
        state.Intel.TradeRoutes["route_deep_1"] = route;

        // Advance past Near threshold (50) but not Mid threshold (150)
        for (int i = 0; i < DiscoveryIntelTweaksV0.NearDecayTicks + 5; i++)
            state.AdvanceTick();

        IntelSystem.EvaluateTradeRoutes(state);

        // E is 4 hops from A → Mid band (3-5 hops) → decay at 150 ticks
        // At tick 55, should NOT be stale yet
        Assert.That(route.Status, Is.Not.EqualTo(TradeRouteStatus.Unprofitable),
            "Mid-band route should not be Unprofitable at Near threshold");
    }

    [Test]
    public void InstabilityGated_Discovery_SkipsOutcome_WhenInstabilityLow()
    {
        var state = MakeIntelGraph();

        var discId = "disc_v0|SIGNAL|B|ref1|src1";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Analyzed,
            InstabilityGate = 2,
            FlavorText = "Some signal"
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);
        state.Nodes["B"].InstabilityLevel = 1; // Below gate of 2

        DiscoveryOutcomeSystem.Process(state);

        // Should NOT generate trade intel because instability is below gate
        var discoveryRoutes = state.Intel.TradeRoutes.Values
            .Where(r => !string.IsNullOrEmpty(r.SourceDiscoveryId))
            .ToList();

        Assert.That(discoveryRoutes.Count, Is.EqualTo(0),
            "Gated discovery should not generate intel when instability is below gate");
    }

    [Test]
    public void GetManualScanCountByFamily_CountsScannedDiscoveries()
    {
        var state = MakeIntelGraph();

        // MapKindToFamily: RESOURCE_POOL_MARKER→RUIN, CORRIDOR_TRACE→SIGNAL, other→OUTCOME
        // So to count family "RUIN" we need kind RESOURCE_POOL_MARKER
        state.Intel.Discoveries["disc_v0|RESOURCE_POOL_MARKER|A|ref1|s1"] = new DiscoveryStateV0
        { DiscoveryId = "disc_v0|RESOURCE_POOL_MARKER|A|ref1|s1", Phase = DiscoveryPhase.Seen };
        state.Intel.Discoveries["disc_v0|RESOURCE_POOL_MARKER|B|ref2|s2"] = new DiscoveryStateV0
        { DiscoveryId = "disc_v0|RESOURCE_POOL_MARKER|B|ref2|s2", Phase = DiscoveryPhase.Scanned };
        state.Intel.Discoveries["disc_v0|RESOURCE_POOL_MARKER|C|ref3|s3"] = new DiscoveryStateV0
        { DiscoveryId = "disc_v0|RESOURCE_POOL_MARKER|C|ref3|s3", Phase = DiscoveryPhase.Analyzed };
        state.Intel.Discoveries["disc_v0|CORRIDOR_TRACE|D|ref4|s4"] = new DiscoveryStateV0
        { DiscoveryId = "disc_v0|CORRIDOR_TRACE|D|ref4|s4", Phase = DiscoveryPhase.Scanned };

        int ruinCount = DiscoveryOutcomeSystem.GetManualScanCountByFamily(state, "RUIN");
        int signalCount = DiscoveryOutcomeSystem.GetManualScanCountByFamily(state, "SIGNAL");

        Assert.That(ruinCount, Is.EqualTo(2), "Should count Scanned + Analyzed RESOURCE_POOL_MARKER as RUIN family");
        Assert.That(signalCount, Is.EqualTo(1), "Should count Scanned CORRIDOR_TRACE as SIGNAL family");
    }
}
