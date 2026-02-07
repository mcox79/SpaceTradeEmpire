using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests.Sustainment;

public class TechUpkeepConsumesGoodsTests
{
    [Test]
    public void TechSite_With_TwoInputs_Consumes_Both_EachTick_When_Supplied()
    {
        var state = new SimState(1);

        var mkt = new Market { Id = "n0" };
        mkt.Inventory["ore"] = 100;
        mkt.Inventory["fuel"] = 100;
        state.Markets["n0"] = mkt;

        var site = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int>
            {
                { "ore", 2 },
                { "fuel", 3 }
            },
            Outputs = new Dictionary<string, int>(),
            BufferDays = 1,
            DegradePerDayBps = 0
        };
        state.IndustrySites[site.Id] = site;

        IndustrySystem.Process(state);

        Assert.That(mkt.Inventory["ore"], Is.EqualTo(98));
        Assert.That(mkt.Inventory["fuel"], Is.EqualTo(97));
        Assert.That(site.Efficiency, Is.EqualTo(1.0f).Within(0.0001f));
    }
}
