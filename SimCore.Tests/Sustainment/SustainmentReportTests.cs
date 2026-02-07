using System.Collections.Generic;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Sustainment;

public class SustainmentReportTests
{
    [Test]
    public void SustainmentReport_Computes_Margin_And_Times_Deterministically()
    {
        var state = new SimState(1);

        var mkt = new Market { Id = "n0" };
        mkt.Inventory["ore"] = 100;
        mkt.Inventory["fuel"] = 10;
        state.Markets["n0"] = mkt;

        var site = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int> { { "ore", 2 }, { "fuel", 1 } },
            BufferDays = 1,
            HealthBps = 10000,
            DegradePerDayBps = 1000,
            DegradeRemainder = 0
        };
        state.IndustrySites[site.Id] = site;

        var report = SustainmentReport.BuildForNode(state, "n0");
        Assert.That(report.Count, Is.EqualTo(1));

        var r = report[0];

        Assert.That(r.EffBpsNow, Is.EqualTo(10000));

        // fuel coverage ticks = 10/1 = 10
        Assert.That(r.TimeToStarveTicks, Is.EqualTo(10));
        Assert.That(r.TimeToFailureTicks, Is.GreaterThanOrEqualTo(r.TimeToStarveTicks));

        Assert.That(r.Inputs.Count, Is.EqualTo(2));
        Assert.That(r.Inputs[0].GoodId, Is.EqualTo("fuel")); // sorted by key: fuel then ore
        Assert.That(r.Inputs[1].GoodId, Is.EqualTo("ore"));
    }
}
