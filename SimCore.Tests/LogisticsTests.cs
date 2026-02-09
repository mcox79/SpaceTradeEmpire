using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests;

public class LogisticsTests
{
    [Test]
    public void Logistics_AssignsJob_WhenShortageExists()
    {
        var state = new SimState(123);

        // 1. Setup Topology (Nodes + Edge)
        state.Nodes.Add("alpha", new Node { Id = "alpha", MarketId = "alpha" });
        state.Nodes.Add("beta", new Node { Id = "beta", MarketId = "beta" });
        state.Edges.Add("e1_ab", new Edge { Id = "e1_ab", FromNodeId = "alpha", ToNodeId = "beta", Distance = 10 });
        state.Edges.Add("e1_ba", new Edge { Id = "e1_ba", FromNodeId = "beta", ToNodeId = "alpha", Distance = 10 });


        // 2. Setup Markets
        var alpha = new Market { Id = "alpha" };
        alpha.Inventory["ore"] = 100;
        state.Markets.Add("alpha", alpha);

        var beta = new Market { Id = "beta" };
        beta.Inventory["ore"] = 0;
        state.Markets.Add("beta", beta);

        // 3. Setup Demand (Factory at Beta)
        var factory = new IndustrySite
        {
            Id = "fac_1",
            NodeId = "beta",
            Active = true,
            BufferDays = 1,
            Inputs = new Dictionary<string, int> { { "ore", 10 } }
        };

        state.IndustrySites.Add("fac_1", factory);

        // 4. Setup Fleet (Idle at Beta)
        var fleet = new Fleet { Id = "f1", OwnerId = "ai", CurrentNodeId = "beta", State = FleetState.Idle };
        state.Fleets.Add("f1", fleet);

        // Act
        LogisticsSystem.Process(state);

        // Assert
        Assert.IsNotNull(fleet.CurrentJob, "Fleet should have a job assigned.");
        Assert.That(fleet.CurrentJob.GoodId, Is.EqualTo("ore"));
        Assert.That(fleet.CurrentJob.SourceNodeId, Is.EqualTo("alpha"));

        // New contract: LogisticsSystem assigns a job and loads the planned leg.
        // MovementSystem consumes RouteEdgeIds; DestinationNodeId is intentionally empty to avoid replanning.
        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo("alpha"));
        Assert.That(fleet.DestinationNodeId, Is.EqualTo(""));
        Assert.That(fleet.RouteEdgeIds, Is.EqualTo(new[] { "e1_ba" }));
        Assert.That(fleet.RouteEdgeIndex, Is.EqualTo(0));
        Assert.That(fleet.State, Is.EqualTo(FleetState.Idle));


    }
}
