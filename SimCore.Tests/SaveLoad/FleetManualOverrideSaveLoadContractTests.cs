using System;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Gen;

namespace SimCore.Tests.SaveLoad;

public class FleetManualOverrideSaveLoadContractTests
{
    [Test]
    public void SaveLoad_RoundTrip_Preserves_ManualOverrideNodeId_Exactly()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Deterministic target: highest Node.Id (stable, avoids "same node" edge case)
        var targetNodeId = sim.State.Nodes.Values
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Last().Id;

        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, targetNodeId, "test_override"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(targetNodeId), "Override should be set before save.");

        var beforeHash = sim.State.GetSignature();
        var json = sim.SaveToString();

        var sim2 = new SimKernel(seed);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();

        var fleet2 = sim2.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        Assert.That(afterHash, Is.EqualTo(beforeHash), "Save/load roundtrip changed world hash.");
        Assert.That(fleet2.ManualOverrideNodeId, Is.EqualTo(targetNodeId), "ManualOverrideNodeId must round-trip exactly.");
    }

    [Test]
    public void SaveLoad_RoundTrip_Preserves_OverrideDrivenRouting_Intention()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Choose a different node than current to ensure MovementSystem can plan.
        var targetNodeId = sim.State.Nodes.Values
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Select(n => n.Id)
            .First(id => !string.Equals(id, fleet.CurrentNodeId, StringComparison.Ordinal));

        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, targetNodeId, "test_override"));
        sim.Step();

        // Force MovementSystem to observe and plan on next tick.
        sim.Step();

        var beforeHash = sim.State.GetSignature();
        var json = sim.SaveToString();

        var sim2 = new SimKernel(seed);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();

        Assert.That(afterHash, Is.EqualTo(beforeHash), "Save/load roundtrip changed world hash.");

        var fleet2 = sim2.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        Assert.That(fleet2.ManualOverrideNodeId, Is.EqualTo(targetNodeId));

        // Core contract: while override is set, the fleet's final destination request aligns to override.
        // (RouteEdgeIds may be empty if no route exists; we assert the request field, not successful travel.)
        Assert.That(fleet2.FinalDestinationNodeId, Is.EqualTo(targetNodeId));
    }
}
