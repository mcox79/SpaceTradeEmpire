using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests.Sustainment;

public class DeterministicDegradationTests
{
    [Test]
    public void Health_Decreases_Deterministically_When_Undersupplied()
    {
        var state = new SimState(1);

        var mkt = new Market { Id = "n0" };
        mkt.Inventory["ore"] = 0;
        mkt.Inventory["fuel"] = 0;
        state.Markets["n0"] = mkt;

        var site = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int>
            {
                { "ore", 10 },
                { "fuel", 1 }
            },
            BufferDays = 1,
            HealthBps = 10000,
            DegradePerDayBps = 1000 // 10% per day at full deficit
        };
        state.IndustrySites[site.Id] = site;

        // Run exactly 1440 ticks: expect total degradation ~= 1000 bps (10%) at full deficit.
        for (int i = 0; i < IndustrySystem.TicksPerDay; i++)
        {
            IndustrySystem.Process(state);
            state.AdvanceTick();
        }

        Assert.That(site.HealthBps, Is.EqualTo(9000));
        Assert.That(site.DegradeRemainder, Is.EqualTo(0));
        Assert.That(site.Efficiency, Is.EqualTo(0.0f).Within(0.0001f));
    }
}
