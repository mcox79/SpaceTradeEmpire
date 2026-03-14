using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S8.MEGAPROJECT.CONTRACT.001: Contract tests for megaproject system.
[TestFixture]
public class MegaprojectTests
{
    private SimKernel CreateSeededKernel(int seed = 42)
    {
        var kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel;
    }

    private string GetFirstNodeWithMarketAndFaction(SimState state)
    {
        foreach (var nodeId in state.Nodes.Keys.OrderBy(k => k))
        {
            if (state.Markets.ContainsKey(nodeId) && state.NodeFactionId.ContainsKey(nodeId))
                return nodeId;
        }
        return "";
    }

    [Test]
    public void StartMegaproject_WithSufficientResources_Succeeds()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        Assert.That(nodeId, Is.Not.Empty, "Need a node with market+faction");

        // Set high rep and credits.
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        Assert.That(result.Success, Is.True, result.Reason);
        Assert.That(result.MegaprojectId, Is.Not.Empty);
        Assert.That(state.Megaprojects.Count, Is.EqualTo(1));
        Assert.That(state.PlayerCredits, Is.LessThan(100000)); // Credits deducted
    }

    [Test]
    public void StartMegaproject_InsufficientReputation_Rejected()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        Assert.That(nodeId, Is.Not.Empty);

        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 0; // Below MinFactionRepToStart
        state.PlayerCredits = 100000;

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("insufficient_reputation"));
    }

    [Test]
    public void DeliverSupply_TransfersCargo()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        Assert.That(result.Success, Is.True);

        // Give player cargo.
        state.PlayerCargo[WellKnownGoodIds.ExoticMatter] = 100;
        state.PlayerCargo[WellKnownGoodIds.Composites] = 100;

        // Deliver exotic matter.
        bool delivered = MegaprojectSystem.DeliverSupply(state, result.MegaprojectId, WellKnownGoodIds.ExoticMatter, 30);
        Assert.That(delivered, Is.True);
        Assert.That(state.PlayerCargo[WellKnownGoodIds.ExoticMatter], Is.EqualTo(70));

        var mp = state.Megaprojects[result.MegaprojectId];
        Assert.That(mp.SupplyDelivered[WellKnownGoodIds.ExoticMatter], Is.EqualTo(30));
    }

    [Test]
    public void Process_AdvancesStagesWhenSupplied()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        var mp = state.Megaprojects[result.MegaprojectId];

        // Supply first stage fully.
        mp.SupplyDelivered[WellKnownGoodIds.ExoticMatter] = MegaprojectTweaksV0.AnchorExoticMatterPerStage;
        mp.SupplyDelivered[WellKnownGoodIds.Composites] = MegaprojectTweaksV0.AnchorCompositesPerStage;

        // Tick until stage advances.
        for (int i = 0; i < MegaprojectTweaksV0.AnchorTicksPerStage; i++)
        {
            MegaprojectSystem.Process(state);
            state.AdvanceTick();
        }

        Assert.That(mp.Stage, Is.EqualTo(1));
        Assert.That(mp.ProgressTicks, Is.EqualTo(0)); // Reset after stage complete
        Assert.That(mp.SupplyDelivered.Count, Is.EqualTo(0)); // Cleared
    }

    [Test]
    public void Process_CompletesAfterAllStages()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        var mp = state.Megaprojects[result.MegaprojectId];
        var def = MegaprojectContentV0.GetByTypeId("fracture_anchor")!;

        // Complete all stages.
        for (int stage = 0; stage < def.Stages; stage++)
        {
            mp.SupplyDelivered[WellKnownGoodIds.ExoticMatter] = MegaprojectTweaksV0.AnchorExoticMatterPerStage;
            mp.SupplyDelivered[WellKnownGoodIds.Composites] = MegaprojectTweaksV0.AnchorCompositesPerStage;

            for (int i = 0; i < def.TicksPerStage; i++)
            {
                MegaprojectSystem.Process(state);
                state.AdvanceTick();
            }
        }

        Assert.That(mp.IsComplete, Is.True);
        Assert.That(mp.CompletedTick, Is.GreaterThanOrEqualTo(0));
        Assert.That(mp.MutationApplied, Is.True);
    }

    [Test]
    public void FractureAnchor_MutationMarksFractureNode()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        // Ensure node is NOT fracture before.
        Assert.That(state.Nodes[nodeId].IsFractureNode, Is.False);

        var result = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        var mp = state.Megaprojects[result.MegaprojectId];
        var def = MegaprojectContentV0.GetByTypeId("fracture_anchor")!;

        // Complete all stages.
        for (int stage = 0; stage < def.Stages; stage++)
        {
            mp.SupplyDelivered[WellKnownGoodIds.ExoticMatter] = MegaprojectTweaksV0.AnchorExoticMatterPerStage;
            mp.SupplyDelivered[WellKnownGoodIds.Composites] = MegaprojectTweaksV0.AnchorCompositesPerStage;
            for (int i = 0; i < def.TicksPerStage; i++)
            {
                MegaprojectSystem.Process(state);
                state.AdvanceTick();
            }
        }

        Assert.That(state.Nodes[nodeId].IsFractureNode, Is.True);
    }

    [Test]
    public void TradeCorridor_MutationBoostsEdgeSpeed()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        state.PlayerCargo[WellKnownGoodIds.RareMetals] = 1000;
        state.PlayerCargo[WellKnownGoodIds.Electronics] = 1000;

        var result = MegaprojectSystem.StartMegaproject(state, "trade_corridor", nodeId, "fleet_trader_1");
        var mp = state.Megaprojects[result.MegaprojectId];
        var def = MegaprojectContentV0.GetByTypeId("trade_corridor")!;

        for (int stage = 0; stage < def.Stages; stage++)
        {
            mp.SupplyDelivered[WellKnownGoodIds.RareMetals] = MegaprojectTweaksV0.CorridorRareMetalsPerStage;
            mp.SupplyDelivered[WellKnownGoodIds.Electronics] = MegaprojectTweaksV0.CorridorElectronicsPerStage;
            for (int i = 0; i < def.TicksPerStage; i++)
            {
                MegaprojectSystem.Process(state);
                state.AdvanceTick();
            }
        }

        // Check edges connected to the node have boosted speed.
        var boostedEdges = state.Edges.Values
            .Where(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId)
            .ToList();
        Assert.That(boostedEdges.Count, Is.GreaterThan(0));
        foreach (var edge in boostedEdges)
        {
            Assert.That(edge.SpeedMultiplierPct, Is.EqualTo(100 + MegaprojectTweaksV0.CorridorTransitSpeedBoostPct));
        }
    }

    [Test]
    public void SensorPylon_MutationRegistersPylonNode()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        var result = MegaprojectSystem.StartMegaproject(state, "sensor_pylon", nodeId, "fleet_trader_1");
        var mp = state.Megaprojects[result.MegaprojectId];
        var def = MegaprojectContentV0.GetByTypeId("sensor_pylon")!;

        for (int stage = 0; stage < def.Stages; stage++)
        {
            mp.SupplyDelivered[WellKnownGoodIds.Electronics] = MegaprojectTweaksV0.PylonElectronicsPerStage;
            mp.SupplyDelivered[WellKnownGoodIds.ExoticCrystals] = MegaprojectTweaksV0.PylonExoticCrystalsPerStage;
            for (int i = 0; i < def.TicksPerStage; i++)
            {
                MegaprojectSystem.Process(state);
                state.AdvanceTick();
            }
        }

        Assert.That(state.SensorPylonNodes.Contains(nodeId), Is.True);
    }

    [Test]
    public void StartMegaproject_NodeOccupied_Rejected()
    {
        var kernel = CreateSeededKernel();
        var state = kernel.State;

        var nodeId = GetFirstNodeWithMarketAndFaction(state);
        var factionId = state.NodeFactionId[nodeId];
        state.FactionReputation[factionId] = 50;
        state.PlayerCredits = 100000;

        var r1 = MegaprojectSystem.StartMegaproject(state, "fracture_anchor", nodeId, "fleet_trader_1");
        Assert.That(r1.Success, Is.True);

        var r2 = MegaprojectSystem.StartMegaproject(state, "trade_corridor", nodeId, "fleet_trader_1");
        Assert.That(r2.Success, Is.False);
        Assert.That(r2.Reason, Is.EqualTo("node_occupied"));
    }
}
