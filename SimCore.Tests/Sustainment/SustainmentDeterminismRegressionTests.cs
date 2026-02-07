using System.Collections.Generic;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Sustainment;

public class SustainmentDeterminismRegressionTests
{
    [Test]
    public void Sustainment_RunTwice_SameSetup_Produces_Identical_State()
    {
        const int seed = 123;
        const int ticks = 3 * IndustrySystem.TicksPerDay + 7;

        var a = BuildState(seed);
        var b = BuildState(seed);

        RunTicks(a, ticks);
        RunTicks(b, ticks);

        AssertStatesEqual(a, b);
    }

    private static SimState BuildState(int seed)
    {
        var state = new SimState(seed);

        var mkt = new Market { Id = "n0" };
        // Intentional mix: some supply for ore, none for fuel to force partial undersupply and remainder accumulation.
        mkt.Inventory["ore"] = 5000;
        mkt.Inventory["fuel"] = 0;
        state.Markets["n0"] = mkt;

        var site = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int>
            {
                { "ore", 2 },
                { "fuel", 1 }
            },
            Outputs = new Dictionary<string, int>(),
            BufferDays = 2,
            HealthBps = 10000,
            DegradePerDayBps = 1234 // chosen to ensure remainder behavior across ticks
        };

        state.IndustrySites[site.Id] = site;
        return state;
    }

    private static void RunTicks(SimState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            IndustrySystem.Process(state);
            state.AdvanceTick();
        }
    }

    private static void AssertStatesEqual(SimState a, SimState b)
    {
        Assert.That(a.Tick, Is.EqualTo(b.Tick), "Tick differs.");

        Assert.That(a.Markets.ContainsKey("n0"), Is.True, "Market n0 missing in A.");
        Assert.That(b.Markets.ContainsKey("n0"), Is.True, "Market n0 missing in B.");

        var am = a.Markets["n0"];
        var bm = b.Markets["n0"];

        Assert.That(am.Inventory["ore"], Is.EqualTo(bm.Inventory["ore"]), "Market ore differs.");
        Assert.That(am.Inventory["fuel"], Is.EqualTo(bm.Inventory["fuel"]), "Market fuel differs.");

        Assert.That(a.IndustrySites.ContainsKey("tech0"), Is.True, "Site tech0 missing in A.");
        Assert.That(b.IndustrySites.ContainsKey("tech0"), Is.True, "Site tech0 missing in B.");

        var asite = a.IndustrySites["tech0"];
        var bsite = b.IndustrySites["tech0"];

        Assert.That(asite.HealthBps, Is.EqualTo(bsite.HealthBps), "HealthBps differs.");
        Assert.That(asite.DegradeRemainder, Is.EqualTo(bsite.DegradeRemainder), "DegradeRemainder differs.");
        Assert.That(asite.Efficiency, Is.EqualTo(bsite.Efficiency).Within(0.0001f), "Efficiency differs.");
    }
}
