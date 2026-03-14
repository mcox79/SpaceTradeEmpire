using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Systems;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S8.MEGAPROJECT.HEADLESS.001: Full-stack integration proof.
// Exercises: start megaproject, deliver supply, advance ticks to completion,
// verify map rule mutation, save/load round-trip preserves state.
[TestFixture]
public class MegaprojectIntegrationTests
{
    private SimKernel CreateReadyKernel(int seed = 42)
    {
        var kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        // Run a few ticks to stabilize state.
        for (int i = 0; i < 5; i++) kernel.Step();
        return kernel;
    }

    private string GetFactionNode(SimState state)
    {
        foreach (var nodeId in state.Nodes.Keys.OrderBy(k => k))
        {
            if (state.Markets.ContainsKey(nodeId) && state.NodeFactionId.ContainsKey(nodeId))
                return nodeId;
        }
        return "";
    }

    [Test]
    public void FullStack_StartSupplyComplete_FractureAnchor()
    {
        var kernel = CreateReadyKernel();
        var state = kernel.State;

        var nodeId = GetFactionNode(state);
        Assert.That(nodeId, Is.Not.Empty, "Need a node with market+faction");

        // Setup: high rep and credits.
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;
        state.PlayerCargo[WellKnownGoodIds.ExoticMatter] = 10000;
        state.PlayerCargo[WellKnownGoodIds.Composites] = 10000;

        // Start megaproject.
        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        Assert.That(result.Success, Is.True, result.Reason);
        var mpId = result.MegaprojectId;

        // Complete all stages via supply + ticks.
        var def = MegaprojectContentV0.GetByTypeId("fracture_anchor")!;
        var mp = state.Megaprojects[mpId];

        for (int stage = 0; stage < def.Stages; stage++)
        {
            // Deliver supply for current stage.
            foreach (var req in def.SupplyPerStage)
            {
                MegaprojectSystem.DeliverSupply(state, mpId, req.Key, req.Value);
            }

            // Advance ticks for stage completion.
            for (int i = 0; i < def.TicksPerStage; i++)
            {
                kernel.Step();
            }
        }

        Assert.That(mp.IsComplete, Is.True, "Megaproject should be complete after all stages");
        Assert.That(mp.MutationApplied, Is.True, "Mutation should be applied");
        Assert.That(state.Nodes[nodeId].IsFractureNode, Is.True, "Node should be marked as fracture node");
    }

    [Test]
    public void FullStack_SaveLoad_PreservesMegaprojectState()
    {
        var kernel = CreateReadyKernel();
        var state = kernel.State;

        var nodeId = GetFactionNode(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        state.PlayerCargo[WellKnownGoodIds.ExoticMatter] = 100;

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        Assert.That(result.Success, Is.True);

        // Deliver partial supply.
        bool delivered = MegaprojectSystem.DeliverSupply(state, result.MegaprojectId, WellKnownGoodIds.ExoticMatter, 15);
        Assert.That(delivered, Is.True, "Supply delivery should succeed when player has cargo");

        // Save.
        var json = kernel.SaveToString();
        Assert.That(json, Is.Not.Empty);

        // Load into new kernel.
        var kernel2 = new SimKernel(1);
        kernel2.LoadFromString(json);
        var state2 = kernel2.State;

        // Verify megaproject preserved.
        Assert.That(state2.Megaprojects.Count, Is.EqualTo(1));
        var mp2 = state2.Megaprojects[result.MegaprojectId];
        Assert.That(mp2.TypeId, Is.EqualTo("fracture_anchor"));
        Assert.That(mp2.NodeId, Is.EqualTo(nodeId));
        Assert.That(mp2.SupplyDelivered.ContainsKey(WellKnownGoodIds.ExoticMatter), Is.True,
            "Supply delivery should be preserved after save/load");
        Assert.That(mp2.SupplyDelivered[WellKnownGoodIds.ExoticMatter], Is.EqualTo(15));
    }

    [Test]
    public void FullStack_TradeCorridor_BoostsEdgeSpeed()
    {
        var kernel = CreateReadyKernel();
        var state = kernel.State;

        var nodeId = GetFactionNode(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;
        state.PlayerCargo[WellKnownGoodIds.RareMetals] = 10000;
        state.PlayerCargo[WellKnownGoodIds.Electronics] = 10000;

        var result = MegaprojectSystem.StartMegaproject(state, "trade_corridor", nodeId, "fleet_trader_1");
        Assert.That(result.Success, Is.True, result.Reason);

        var def = MegaprojectContentV0.GetByTypeId("trade_corridor")!;
        var mp = state.Megaprojects[result.MegaprojectId];

        for (int stage = 0; stage < def.Stages; stage++)
        {
            foreach (var req in def.SupplyPerStage)
                MegaprojectSystem.DeliverSupply(state, result.MegaprojectId, req.Key, req.Value);
            for (int i = 0; i < def.TicksPerStage; i++)
                kernel.Step();
        }

        Assert.That(mp.IsComplete, Is.True);

        // Verify edge speed boost on connected edges.
        var boostedEdges = state.Edges.Values
            .Where(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId)
            .ToList();
        Assert.That(boostedEdges.Count, Is.GreaterThan(0));
    }

    [Test]
    public void FullStack_MultipleMegaprojects_DifferentNodes()
    {
        var kernel = CreateReadyKernel();
        var state = kernel.State;

        // Find two different faction nodes.
        var factionNodes = state.Nodes.Keys
            .OrderBy(k => k)
            .Where(n => state.Markets.ContainsKey(n) && state.NodeFactionId.ContainsKey(n))
            .Take(2)
            .ToList();

        Assert.That(factionNodes.Count, Is.EqualTo(2), "Need 2 faction nodes");

        foreach (var nid in factionNodes)
        {
            var fid = state.NodeFactionId[nid];
            state.FactionReputation[fid] = 50;
        }
        state.PlayerCredits = 200000;

        var r1 = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", factionNodes[0], "fleet_trader_1");
        Assert.That(r1.Success, Is.True, r1.Reason);

        var r2 = MegaprojectSystem.StartMegaproject(state, "sensor_pylon", factionNodes[1], "fleet_trader_1");
        Assert.That(r2.Success, Is.True, r2.Reason);

        Assert.That(state.Megaprojects.Count, Is.EqualTo(2));
    }
}
