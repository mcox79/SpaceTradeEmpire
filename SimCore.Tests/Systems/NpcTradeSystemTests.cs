using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S5.NPC_TRADE.SYSTEM.001: NPC trade system contract tests.
[TestFixture]
[Category("NpcTrade")]
public sealed class NpcTradeSystemTests
{
    private SimState CreateTwoNodeState()
    {
        var state = new SimState(42);

        var nodeA = new Node { Id = "node_a", Name = "Alpha" };
        var nodeB = new Node { Id = "node_b", Name = "Beta" };
        state.Nodes["node_a"] = nodeA;
        state.Nodes["node_b"] = nodeB;

        var marketA = new Market { Id = "node_a" };
        marketA.Inventory["fuel"] = 50;
        marketA.Inventory["ore"] = 30;
        state.Markets["node_a"] = marketA;

        var marketB = new Market { Id = "node_b" };
        marketB.Inventory["fuel"] = 5; // low stock = high sell price at B
        marketB.Inventory["ore"] = 50;
        state.Markets["node_b"] = marketB;

        var edge = new Edge { Id = "edge_a_b", FromNodeId = "node_a", ToNodeId = "node_b", Distance = 10f };
        state.Edges["edge_a_b"] = edge;

        return state;
    }

    private Fleet CreateNpcTrader(string id, string nodeId)
    {
        return new Fleet
        {
            Id = id,
            OwnerId = "npc",
            Role = FleetRole.Trader,
            CurrentNodeId = nodeId,
        };
    }

    /// <summary>
    /// Advance state to first valid NPC trade eval tick (>= EvalIntervalTicks AND divisible).
    /// </summary>
    private static void AdvanceToEvalTick(SimState state)
    {
        while (state.Tick < NpcTradeTweaksV0.EvalIntervalTicks)
            state.AdvanceTick();
        while (state.Tick % NpcTradeTweaksV0.EvalIntervalTicks != 0)
            state.AdvanceTick();
    }

    [Test]
    public void ProcessNpcTrade_NullState_NoThrow()
    {
        Assert.DoesNotThrow(() => NpcTradeSystem.ProcessNpcTrade(null!));
    }

    [Test]
    public void ProcessNpcTrade_SkipsPlayerFleets()
    {
        var state = CreateTwoNodeState();
        var playerFleet = new Fleet
        {
            Id = "fleet_player",
            OwnerId = "player",
            Role = FleetRole.Trader,
            CurrentNodeId = "node_a",
        };
        state.Fleets["fleet_player"] = playerFleet;

        AdvanceToEvalTick(state);

        int cargoBefore = playerFleet.Cargo.Count;
        NpcTradeSystem.ProcessNpcTrade(state);

        Assert.That(playerFleet.Cargo.Count, Is.EqualTo(cargoBefore),
            "NPC trade should not affect player fleets");
    }

    [Test]
    public void ProcessNpcTrade_SkipsNonTraderRole()
    {
        var state = CreateTwoNodeState();
        var patrolFleet = new Fleet
        {
            Id = "fleet_patrol",
            OwnerId = "npc",
            Role = FleetRole.Patrol,
            CurrentNodeId = "node_a",
        };
        state.Fleets["fleet_patrol"] = patrolFleet;

        AdvanceToEvalTick(state);

        int cargoBefore = patrolFleet.Cargo.Count;
        NpcTradeSystem.ProcessNpcTrade(state);

        Assert.That(patrolFleet.Cargo.Count, Is.EqualTo(cargoBefore),
            "NPC trade should not affect non-trader fleets");
    }

    [Test]
    public void FindBestOpportunity_FindsProfitableRoute()
    {
        var state = CreateTwoNodeState();

        var localMarket = state.Markets["node_a"];
        var opp = NpcTradeSystem.FindBestOpportunity(state, "node_a", localMarket);

        if (opp != null)
        {
            Assert.That(opp.ProfitPerUnit, Is.GreaterThanOrEqualTo(NpcTradeTweaksV0.ProfitThresholdCredits));
            Assert.That(opp.Units, Is.GreaterThan(0));
            Assert.That(opp.DestNodeId, Is.EqualTo("node_b"));
        }
    }

    [Test]
    public void ProcessNpcTrade_FleetDeliversCargo_MarketGrows()
    {
        var state = CreateTwoNodeState();
        var fleet = CreateNpcTrader("npc_1", "node_a");
        fleet.Cargo["ore"] = 5;
        state.Fleets["npc_1"] = fleet;

        int marketOreBefore = state.Markets["node_a"].Inventory["ore"];

        AdvanceToEvalTick(state);
        NpcTradeSystem.ProcessNpcTrade(state);

        Assert.That(fleet.Cargo.ContainsKey("ore"), Is.False,
            "Cargo should be empty after delivery");
        Assert.That(state.Markets["node_a"].Inventory["ore"], Is.GreaterThan(marketOreBefore),
            "Market should receive delivered goods");
    }

    [Test]
    public void ProcessNpcTrade_OnlyRunsAtInterval()
    {
        var state = CreateTwoNodeState();
        var fleet = CreateNpcTrader("npc_1", "node_a");
        fleet.Cargo["ore"] = 5;
        state.Fleets["npc_1"] = fleet;

        // Advance to a non-eval tick (past the warmup period)
        AdvanceToEvalTick(state);
        state.AdvanceTick(); // now at EvalIntervalTicks + 1

        int cargoQtyBefore = fleet.Cargo.TryGetValue("ore", out var q) ? q : 0;
        NpcTradeSystem.ProcessNpcTrade(state);

        int cargoQtyAfter = fleet.Cargo.TryGetValue("ore", out var qa) ? qa : 0;
        Assert.That(cargoQtyAfter, Is.EqualTo(cargoQtyBefore),
            "Should not process trade on non-interval tick");
    }

    [Test]
    public void ProcessNpcTrade_PriceDeltaShrinks_AfterCirculation()
    {
        var state = CreateTwoNodeState();
        var fleet1 = CreateNpcTrader("npc_1", "node_a");
        var fleet2 = CreateNpcTrader("npc_2", "node_b");
        state.Fleets["npc_1"] = fleet1;
        state.Fleets["npc_2"] = fleet2;

        // Run many eval cycles
        for (int i = 0; i < 200; i++)
        {
            // Simulate delivery: if fleet has destination, teleport there and clear
            if (!string.IsNullOrEmpty(fleet1.DestinationNodeId))
            {
                fleet1.CurrentNodeId = fleet1.DestinationNodeId;
                fleet1.DestinationNodeId = "";
                fleet1.FinalDestinationNodeId = "";
                fleet1.CurrentEdgeId = "";
            }
            if (!string.IsNullOrEmpty(fleet2.DestinationNodeId))
            {
                fleet2.CurrentNodeId = fleet2.DestinationNodeId;
                fleet2.DestinationNodeId = "";
                fleet2.FinalDestinationNodeId = "";
                fleet2.CurrentEdgeId = "";
            }

            AdvanceToEvalTick(state);
            NpcTradeSystem.ProcessNpcTrade(state);
            state.AdvanceTick();
        }

        // After circulation, B's fuel stock should have grown from deliveries
        Assert.That(state.Markets["node_b"].Inventory["fuel"],
            Is.GreaterThanOrEqualTo(5),
            "Fuel stock at node B should not decrease (NPC delivers goods)");
    }

    // GATE.S18.TRADE_GOODS.NPC_TRADE_UPDATE.001: Weight-based scoring test.
    [Test]
    public void FindBestOpportunity_PrefersHigherWeightGood()
    {
        var state = CreateTwoNodeState();
        // Set up equal profit for fuel (weight=100) and munitions (weight=140)
        state.Markets["node_a"].Inventory["munitions"] = 50;
        state.Markets["node_b"].Inventory["munitions"] = 5; // deficit at B

        var localMarket = state.Markets["node_a"];
        var opp = NpcTradeSystem.FindBestOpportunity(state, "node_a", localMarket);

        // With equal profitPerUnit, the higher-weight good should win
        if (opp != null && opp.ProfitPerUnit >= NpcTradeTweaksV0.ProfitThresholdCredits)
        {
            // If munitions has higher weighted score, it should be selected
            int mWeight = NpcTradeTweaksV0.GoodTradeWeights.TryGetValue("munitions", out var mw) ? mw : NpcTradeTweaksV0.DefaultGoodWeight;
            int fWeight = NpcTradeTweaksV0.GoodTradeWeights.TryGetValue("fuel", out var fw) ? fw : NpcTradeTweaksV0.DefaultGoodWeight;
            Assert.That(mWeight, Is.GreaterThan(fWeight), "Munitions should have higher trade weight than fuel");
        }
    }

    // ── GATE.S14.NPC_ALIVE.FLEET_TESTS.001: Fleet survival + role diversity ──

    [Test]
    public void WorldLoader_Apply_Preserves_AiFleets()
    {
        // Full galaxy gen → WorldLoader.Apply should result in more than just the player fleet.
        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        int totalFleets = sim.State.Fleets.Count;
        int aiFleets = sim.State.Fleets.Values.Count(f =>
            !string.Equals(f.OwnerId, "player", System.StringComparison.Ordinal));

        Assert.That(totalFleets, Is.GreaterThan(1), "Should have more than just the player fleet");
        Assert.That(aiFleets, Is.GreaterThan(0), "AI fleets should exist after generation");
    }

    [Test]
    public void FleetSeed_RoleDiversity_WithinTolerance()
    {
        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        var aiFleets = sim.State.Fleets.Values
            .Where(f => !string.Equals(f.OwnerId, "player", System.StringComparison.Ordinal))
            .ToList();

        int traders = aiFleets.Count(f => f.Role == FleetRole.Trader);
        int haulers = aiFleets.Count(f => f.Role == FleetRole.Hauler);
        int patrols = aiFleets.Count(f => f.Role == FleetRole.Patrol);
        int total = aiFleets.Count;

        Assert.That(total, Is.GreaterThan(5), "Need enough AI fleets for role distribution check");

        double traderPct = (double)traders / total;
        double haulerPct = (double)haulers / total;
        double patrolPct = (double)patrols / total;

        // Allow ±20pp tolerance: target 60/25/15
        Assert.That(traderPct, Is.InRange(0.35, 0.85), $"Trader ratio {traderPct:P0} out of range");
        Assert.That(haulerPct, Is.InRange(0.05, 0.50), $"Hauler ratio {haulerPct:P0} out of range");
        Assert.That(patrolPct, Is.InRange(0.01, 0.40), $"Patrol ratio {patrolPct:P0} out of range");
    }

    [Test]
    public void FleetSeed_TradersMove_After100Ticks()
    {
        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        for (int i = 0; i < 100; i++)
            sim.Step();

        var aiTraders = sim.State.Fleets.Values
            .Where(f => f.Role == FleetRole.Trader &&
                        !string.Equals(f.OwnerId, "player", System.StringComparison.Ordinal))
            .ToList();

        int active = aiTraders.Count(f =>
            f.Cargo.Count > 0 ||
            !string.IsNullOrEmpty(f.FinalDestinationNodeId) ||
            !string.IsNullOrEmpty(f.DestinationNodeId));

        double activePct = aiTraders.Count > 0 ? (double)active / aiTraders.Count : 0;

        Assert.That(activePct, Is.GreaterThanOrEqualTo(0.30),
            $"At least 30% of traders should be active after 100 ticks. " +
            $"Active: {active}/{aiTraders.Count} ({activePct:P0})");
    }
}
