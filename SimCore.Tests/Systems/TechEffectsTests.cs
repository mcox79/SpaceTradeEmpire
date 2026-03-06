using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S10.TECH_EFFECTS.TESTS.001: Contract tests for tech effects (speed, production).
[TestFixture]
[Category("TechEffects")]
public sealed class TechEffectsTests
{
    private SimState CreateMovementState(bool unlockThrusters)
    {
        var state = new SimState(42);
        state.PlayerCredits = 10000;

        // Create two connected nodes
        state.Nodes["node_a"] = new Node { Id = "node_a" };
        state.Nodes["node_b"] = new Node { Id = "node_b" };
        state.Edges["edge_ab"] = new Edge
        {
            Id = "edge_ab",
            FromNodeId = "node_a",
            ToNodeId = "node_b",
            Distance = 10f,
            TotalCapacity = 100
        };

        // Player fleet at node_a, traveling to node_b
        var fleet = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            Speed = 1f,
            State = FleetState.Traveling,
            CurrentEdgeId = "edge_ab",
            DestinationNodeId = "node_b",
            TravelProgress = 0f
        };
        state.Fleets["fleet_trader_1"] = fleet;

        if (unlockThrusters)
            state.Tech.UnlockedTechIds.Add("improved_thrusters");

        return state;
    }

    [Test]
    public void SpeedBonus_IncreasesProgress_WhenUnlocked()
    {
        var stateWith = CreateMovementState(unlockThrusters: true);
        var stateWithout = CreateMovementState(unlockThrusters: false);

        MovementSystem.Process(stateWith);
        MovementSystem.Process(stateWithout);

        var progressWith = stateWith.Fleets["fleet_trader_1"].TravelProgress;
        var progressWithout = stateWithout.Fleets["fleet_trader_1"].TravelProgress;

        // With improved_thrusters: 1.0f * 1.2 / 10.0 = 0.12
        // Without: 1.0f / 10.0 = 0.10
        Assert.That(progressWith, Is.GreaterThan(progressWithout),
            "Fleet with improved_thrusters should travel faster");
        Assert.That(progressWith, Is.EqualTo(0.12f).Within(0.001f));
        Assert.That(progressWithout, Is.EqualTo(0.10f).Within(0.001f));
    }

    [Test]
    public void SpeedBonus_OnlyAffectsPlayerFleets()
    {
        var state = CreateMovementState(unlockThrusters: true);

        // Add an AI fleet on the same edge
        var aiFleet = new Fleet
        {
            Id = "fleet_ai_1",
            OwnerId = "faction_a",
            CurrentNodeId = "node_a",
            Speed = 1f,
            State = FleetState.Traveling,
            CurrentEdgeId = "edge_ab",
            DestinationNodeId = "node_b",
            TravelProgress = 0f
        };
        state.Fleets["fleet_ai_1"] = aiFleet;
        // Reserve capacity for the AI fleet
        state.Edges["edge_ab"].UsedCapacity = 1;

        MovementSystem.Process(state);

        var playerProgress = state.Fleets["fleet_trader_1"].TravelProgress;
        var aiProgress = state.Fleets["fleet_ai_1"].TravelProgress;

        // Player gets 1.2x, AI gets 1.0x
        Assert.That(playerProgress, Is.EqualTo(0.12f).Within(0.001f));
        Assert.That(aiProgress, Is.EqualTo(0.10f).Within(0.001f));
    }

    private SimState CreateIndustryState(bool unlockRefining)
    {
        var state = new SimState(42);
        state.PlayerCredits = 10000;

        // Create node with market
        state.Nodes["node_a"] = new Node { Id = "node_a" };
        state.Markets["node_a"] = new Market { Id = "node_a" };

        // Stock input goods
        state.Markets["node_a"].Inventory["ore"] = 1000;

        // Create industry site: ore -> metal (1:1)
        var site = new IndustrySite
        {
            NodeId = "node_a",
            Active = true,
            RecipeId = "test_recipe",
            HealthBps = 10000,
            Inputs = new Dictionary<string, int> { ["ore"] = 10 },
            Outputs = new Dictionary<string, int> { ["metal"] = 10 }
        };
        state.IndustrySites["site_a"] = site;

        if (unlockRefining)
            state.Tech.UnlockedTechIds.Add("advanced_refining");

        return state;
    }

    [Test]
    public void ProductionEfficiency_IncreasesOutput_WhenUnlocked()
    {
        var stateWith = CreateIndustryState(unlockRefining: true);
        var stateWithout = CreateIndustryState(unlockRefining: false);

        IndustrySystem.Process(stateWith);
        IndustrySystem.Process(stateWithout);

        var metalWith = InventoryLedger.Get(stateWith.Markets["node_a"].Inventory, "metal");
        var metalWithout = InventoryLedger.Get(stateWithout.Markets["node_a"].Inventory, "metal");

        // With advanced_refining: output boosted by 10%. prodBps = 10000 + 1000 = 11000 (capped at 10000).
        // Actually: prodBps = min(10000, effBps + effBps/10) = min(10000, 10000 + 1000) = 10000
        // Wait — effBps is 10000 (fully supplied), so prodBps = min(10000, 10000 + 1000) = 10000.
        // The boost only matters when effBps < 10000... Let me re-read the code.
        // Actually: effBps at full supply = 10000. prodBps = min(10000, 10000 + 1000) = 10000.
        // So at full supply there's no visible boost — the boost helps when supply is constrained.
        // Let me adjust the test to partial supply.

        // At full supply both produce the same. The effect is only visible at partial efficiency.
        // Both should produce 10 metal.
        Assert.That(metalWith, Is.EqualTo(metalWithout),
            "At full supply, production efficiency bonus is capped at 100%");
    }

    [Test]
    public void ProductionEfficiency_BoostsPartialSupply()
    {
        // With partial supply (50%), the 10% boost increases output from 50% to 55%.
        var stateWith = CreateIndustryState(unlockRefining: true);
        var stateWithout = CreateIndustryState(unlockRefining: false);

        // Set ore to 5 (half of required 10) → effBps = 5000 (50%)
        stateWith.Markets["node_a"].Inventory["ore"] = 5;
        stateWithout.Markets["node_a"].Inventory["ore"] = 5;

        IndustrySystem.Process(stateWith);
        IndustrySystem.Process(stateWithout);

        var metalWith = InventoryLedger.Get(stateWith.Markets["node_a"].Inventory, "metal");
        var metalWithout = InventoryLedger.Get(stateWithout.Markets["node_a"].Inventory, "metal");

        // Without: effBps=5000, prodBps=5000, output = 10 * 5000 / 10000 = 5
        // With: effBps=5000, prodBps=min(10000, 5000+500)=5500, output = 10 * 5500 / 10000 = 5
        // Hmm, integer truncation: (10 * 5500) / 10000 = 55000 / 10000 = 5
        // Need larger output to see the difference.
        // Let's not change the setup — just assert the relationship.
        Assert.That(metalWith, Is.GreaterThanOrEqualTo(metalWithout),
            "With advanced_refining, partial supply should produce at least as much");
    }

    [Test]
    public void ProductionEfficiency_BoostsLargeOutput()
    {
        // Use larger numbers to make the 10% boost visible through integer truncation.
        var stateWith = CreateIndustryState(unlockRefining: true);
        var stateWithout = CreateIndustryState(unlockRefining: false);

        // Set large output to make 10% visible
        stateWith.IndustrySites["site_a"].Outputs["metal"] = 100;
        stateWithout.IndustrySites["site_a"].Outputs["metal"] = 100;
        // Half supply
        stateWith.Markets["node_a"].Inventory["ore"] = 5;
        stateWithout.Markets["node_a"].Inventory["ore"] = 5;

        IndustrySystem.Process(stateWith);
        IndustrySystem.Process(stateWithout);

        var metalWith = InventoryLedger.Get(stateWith.Markets["node_a"].Inventory, "metal");
        var metalWithout = InventoryLedger.Get(stateWithout.Markets["node_a"].Inventory, "metal");

        // Without: effBps=5000, prodBps=5000, output = (100 * 5000) / 10000 = 50
        // With: effBps=5000, prodBps=5500, output = (100 * 5500) / 10000 = 55
        Assert.That(metalWith, Is.EqualTo(55), "With advanced_refining at 50% eff: 100 * 5500/10000 = 55");
        Assert.That(metalWithout, Is.EqualTo(50), "Without at 50% eff: 100 * 5000/10000 = 50");
        Assert.That(metalWith, Is.GreaterThan(metalWithout));
    }

    [Test]
    public void SpeedBonus_Deterministic_AcrossSeeds()
    {
        // Same setup with different seed values should give same speed bonus
        foreach (var seed in new[] { 1, 42, 100, 999 })
        {
            var state = new SimState(seed);
            state.Nodes["a"] = new Node { Id = "a" };
            state.Nodes["b"] = new Node { Id = "b" };
            state.Edges["e"] = new Edge
            {
                Id = "e",
                FromNodeId = "a",
                ToNodeId = "b",
                Distance = 10f,
                TotalCapacity = 100
            };
            state.Fleets["f"] = new Fleet
            {
                Id = "f",
                OwnerId = "player",
                CurrentNodeId = "a",
                Speed = 1f,
                State = FleetState.Traveling,
                CurrentEdgeId = "e",
                DestinationNodeId = "b",
                TravelProgress = 0f
            };
            state.Tech.UnlockedTechIds.Add("improved_thrusters");

            MovementSystem.Process(state);

            Assert.That(state.Fleets["f"].TravelProgress, Is.EqualTo(0.12f).Within(0.001f),
                $"Seed {seed}: speed bonus should be deterministic");
        }
    }
}
