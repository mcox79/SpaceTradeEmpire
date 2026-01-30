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
        
        // Setup: Alpha (Source of Ore), Beta (Factory Needs Ore)
        var alpha = new Market { Id = "alpha" };
        alpha.Inventory["ore"] = 100;
        state.Markets.Add("alpha", alpha);

        var beta = new Market { Id = "beta" };
        beta.Inventory["ore"] = 0;
        state.Markets.Add("beta", beta);

        // Factory at Beta
        var factory = new IndustrySite 
        { 
            Id = "fac_1", 
            NodeId = "beta",
            Inputs = new Dictionary<string, int> { { "ore", 10 } }
        };
        state.IndustrySites.Add("fac_1", factory);

        // Idle Fleet at Beta
        var fleet = new Fleet { Id = "f1", OwnerId = "ai", CurrentNodeId = "beta", State = FleetState.Idle };
        state.Fleets.Add("f1", fleet);

        // Act: Run Logistics
        LogisticsSystem.Process(state);

        // Assert: Fleet should now be targetting Alpha to get Ore
        Assert.IsNotNull(fleet.CurrentJob);
        Assert.That(fleet.CurrentJob.GoodId, Is.EqualTo("ore"));
        Assert.That(fleet.CurrentJob.SourceNodeId, Is.EqualTo("alpha"));
        Assert.That(fleet.CurrentTask, Does.Contain("Fetching ore"));
    }
}