using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests;

public class IndustryTests
{
    [Test]
    public void Industry_ConsumesInputs_AndProducesOutputs()
    {
        var state = new SimState(123);
        
        // Setup Market
        var mkt = new Market { Id = "mkt_1" };
        mkt.Inventory["ore"] = 10;
        state.Markets.Add("mkt_1", mkt);
        
        // Setup Site (Refinery)
        var site = new IndustrySite 
        { 
            Id = "site_1", 
            NodeId = "mkt_1",
            Inputs = new Dictionary<string, int> { { "ore", 2 } },
            Outputs = new Dictionary<string, int> { { "metal", 1 } }
        };
        state.IndustrySites.Add("site_1", site);

        // TICK 1
        IndustrySystem.Process(state);

        // Assert: 10 - 2 = 8 Ore. 0 + 1 = 1 Metal.
        Assert.That(mkt.Inventory["ore"], Is.EqualTo(8));
        Assert.That(mkt.Inventory["metal"], Is.EqualTo(1));
    }

    [Test]
    public void Industry_PartialProduction_ScalesDown()
    {
        var state = new SimState(123);
        var mkt = new Market { Id = "mkt_1" };
        mkt.Inventory["ore"] = 5; // Needs 10 for full batch
        state.Markets.Add("mkt_1", mkt);

        var site = new IndustrySite 
        { 
            Id = "site_1", 
            NodeId = "mkt_1",
            Inputs = new Dictionary<string, int> { { "ore", 10 } },
            Outputs = new Dictionary<string, int> { { "metal", 4 } }
        };
        state.IndustrySites.Add("site_1", site);

        // TICK 1
        IndustrySystem.Process(state);

        // Ratio = 5 / 10 = 0.5
        // Consumed = floor(10 * 0.5) = 5
        // Produced = floor(4 * 0.5) = 2
        Assert.That(mkt.Inventory["ore"], Is.EqualTo(0));
        Assert.That(mkt.Inventory["metal"], Is.EqualTo(2));
    }
}