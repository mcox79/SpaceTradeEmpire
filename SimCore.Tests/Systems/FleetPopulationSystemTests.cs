using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("FleetPopulationSystem")]
public sealed class FleetPopulationSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        // Set up a faction node with resources for spawning
        state.Nodes["nodeA"] = new Node { Id = "nodeA", MarketId = "nodeA" };
        state.NodeFactionId["nodeA"] = "concord";
        state.Markets["nodeA"] = new Market
        {
            Id = "nodeA",
            Inventory = new()
            {
                [FleetPopulationTweaksV0.ReplacementGood1] = FleetPopulationTweaksV0.ReplacementMetalCost * 5,
                [FleetPopulationTweaksV0.ReplacementGood2] = FleetPopulationTweaksV0.ReplacementComponentsCost * 5,
                ["food"] = 100, ["fuel"] = 100
            }
        };
        return state;
    }

    private const int EVAL_INTERVAL = 500; // STRUCT_POPULATION_EVAL_INTERVAL

    [Test]
    public void Tick0_DoesNotEvaluate()
    {
        var state = CreateState();

        FleetPopulationSystem.Process(state);

        // No fleets should be spawned at tick 0
        Assert.That(state.Fleets.Count, Is.EqualTo(0));
    }

    [Test]
    public void OffCadenceTick_DoesNotEvaluate()
    {
        var state = CreateState();
        // Advance to 1 past evaluation interval
        for (int i = 0; i < EVAL_INTERVAL + 1; i++)
            state.AdvanceTick();

        FleetPopulationSystem.Process(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(0));
    }

    [Test]
    public void EvalTick_BelowThreshold_SpawnsFleet()
    {
        var state = CreateState();
        // Advance to evaluation interval
        for (int i = 0; i < EVAL_INTERVAL; i++)
            state.AdvanceTick();

        int fleetsBefore = state.Fleets.Count;
        FleetPopulationSystem.Process(state);

        // Should have spawned at least 1 fleet for the faction with 0 fleets
        Assert.That(state.Fleets.Count, Is.GreaterThan(fleetsBefore));
    }

    [Test]
    public void InsufficientResources_DoesNotSpawn()
    {
        var state = CreateState();
        // Empty out resources
        state.Markets["nodeA"].Inventory[FleetPopulationTweaksV0.ReplacementGood1] = 0;
        state.Markets["nodeA"].Inventory[FleetPopulationTweaksV0.ReplacementGood2] = 0;

        for (int i = 0; i < EVAL_INTERVAL; i++)
            state.AdvanceTick();

        FleetPopulationSystem.Process(state);

        Assert.That(state.Fleets.Count, Is.EqualTo(0));
    }

    [Test]
    public void SpawnedFleet_DeductsResources()
    {
        var state = CreateState();
        int metalBefore = state.Markets["nodeA"].Inventory[FleetPopulationTweaksV0.ReplacementGood1];
        int compBefore = state.Markets["nodeA"].Inventory[FleetPopulationTweaksV0.ReplacementGood2];

        for (int i = 0; i < EVAL_INTERVAL; i++)
            state.AdvanceTick();

        FleetPopulationSystem.Process(state);

        if (state.Fleets.Count > 0)
        {
            Assert.That(state.Markets["nodeA"].Inventory[FleetPopulationTweaksV0.ReplacementGood1],
                Is.LessThan(metalBefore));
            Assert.That(state.Markets["nodeA"].Inventory[FleetPopulationTweaksV0.ReplacementGood2],
                Is.LessThan(compBefore));
        }
    }
}
