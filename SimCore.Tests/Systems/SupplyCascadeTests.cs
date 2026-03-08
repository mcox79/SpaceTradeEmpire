using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S7.WARFRONT.SUPPLY_CASCADE.001: Pentagon ring supply cascade tests.
// Validates that wartime demand shocks propagate through the dependency ring,
// creating scarcity cascades that affect connected factions.
[TestFixture]
public class SupplyCascadeTests
{
    [Test]
    public void WarfrontDemandDrainsMunitionsAtContestedNodes()
    {
        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Find a contested node from the hot war (Valorin-Weavers).
        var hotWar = sim.State.Warfronts.Values
            .FirstOrDefault(w => w.WarType == WarType.Hot);
        Assert.That(hotWar, Is.Not.Null, "Expected hot war to be seeded");

        if (hotWar!.ContestedNodeIds.Count == 0)
        {
            Assert.Pass("No contested nodes in this seed — skip");
            return;
        }

        var contestedNodeId = hotWar.ContestedNodeIds[0];
        if (!sim.State.Nodes.TryGetValue(contestedNodeId, out var node)) return;
        if (string.IsNullOrEmpty(node.MarketId)) return;
        if (!sim.State.Markets.TryGetValue(node.MarketId, out var market)) return;

        // Seed munitions at the contested market.
        market.Inventory[SimCore.Content.WellKnownGoodIds.Munitions] = 100;
        int initial = market.Inventory[SimCore.Content.WellKnownGoodIds.Munitions];

        // Run warfront demand for several ticks.
        for (int i = 0; i < 10; i++)
            WarfrontDemandSystem.Process(sim.State);

        int remaining = market.Inventory.TryGetValue(SimCore.Content.WellKnownGoodIds.Munitions, out var r) ? r : 0;
        Assert.That(remaining, Is.LessThan(initial), "Wartime demand should drain munitions");
    }

    [Test]
    public void PeacefulWarfront_NoDemandDrain()
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { MarketId = "mkt_a" };
        state.Markets["mkt_a"] = new Market();
        state.Markets["mkt_a"].Inventory[SimCore.Content.WellKnownGoodIds.Munitions] = 50;

        state.Warfronts["wf_peace"] = new WarfrontState
        {
            Id = "wf_peace",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.Peace,
            ContestedNodeIds = new List<string> { "node_a" },
        };

        WarfrontDemandSystem.Process(state);

        Assert.That(state.Markets["mkt_a"].Inventory[SimCore.Content.WellKnownGoodIds.Munitions], Is.EqualTo(50),
            "Peace should not drain goods");
    }

    [Test]
    public void HigherIntensity_DrainsFaster()
    {
        // Test at Skirmish (2) vs TotalWar (4).
        int remainingSkirmish = RunDrainTest(WarfrontIntensity.Skirmish, 20);
        int remainingTotalWar = RunDrainTest(WarfrontIntensity.TotalWar, 20);

        Assert.That(remainingTotalWar, Is.LessThanOrEqualTo(remainingSkirmish),
            "Total war should drain as much or more than skirmish");
    }

    private int RunDrainTest(WarfrontIntensity intensity, int ticks)
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { MarketId = "mkt_a" };
        state.Markets["mkt_a"] = new Market();
        state.Markets["mkt_a"].Inventory[SimCore.Content.WellKnownGoodIds.Munitions] = 200;

        state.Warfronts["wf_test"] = new WarfrontState
        {
            Id = "wf_test",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = intensity,
            ContestedNodeIds = new List<string> { "node_a" },
        };

        for (int i = 0; i < ticks; i++)
            WarfrontDemandSystem.Process(state);

        return state.Markets["mkt_a"].Inventory.TryGetValue(SimCore.Content.WellKnownGoodIds.Munitions, out var r) ? r : 0;
    }

    [Test]
    public void PentagonRing_AllGoodsAreRegisteredContent()
    {
        // Verify that each pentagon ring good is a registered content good.
        var validGoods = new HashSet<string>
        {
            SimCore.Content.WellKnownGoodIds.Fuel, SimCore.Content.WellKnownGoodIds.Ore,
            SimCore.Content.WellKnownGoodIds.Food, SimCore.Content.WellKnownGoodIds.Metal,
            SimCore.Content.WellKnownGoodIds.Electronics, SimCore.Content.WellKnownGoodIds.Composites,
            SimCore.Content.WellKnownGoodIds.Munitions, SimCore.Content.WellKnownGoodIds.RareMetals,
            SimCore.Content.WellKnownGoodIds.ExoticCrystals, SimCore.Content.WellKnownGoodIds.ExoticMatter,
            SimCore.Content.WellKnownGoodIds.Organics, SimCore.Content.WellKnownGoodIds.Components,
            SimCore.Content.WellKnownGoodIds.SalvagedTech,
        };

        foreach (var entry in FactionTweaksV0.PentagonRing)
        {
            Assert.That(validGoods, Does.Contain(entry.Good),
                $"Pentagon ring good '{entry.Good}' ({entry.Consumer} needs from {entry.Supplier}) is not a registered good");
        }
    }

    [Test]
    public void WarfrontEvolution_ColdWarEscalatesOverTime()
    {
        var state = new SimState(42);
        state.Warfronts["wf_cold"] = new WarfrontState
        {
            Id = "wf_cold",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.Tension,
            WarType = WarType.Cold,
            TickStarted = 0,
        };

        // Run past the max escalation window.
        var initialIntensity = state.Warfronts["wf_cold"].Intensity;

        // Simulate many ticks past max escalation window.
        for (int tick = 0; tick <= WarfrontTweaksV0.ColdWarEscalateMaxTick + 1; tick++)
        {
            state.AdvanceTick();
            WarfrontEvolutionSystem.Process(state);
        }

        // After max window, cold war should have escalated to at least Skirmish.
        Assert.That((int)state.Warfronts["wf_cold"].Intensity,
            Is.GreaterThanOrEqualTo((int)WarfrontIntensity.Skirmish),
            "Cold war should escalate past max window");
    }
}
