using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("CommissionSystem")]
public sealed class CommissionSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.FactionReputation["concord"] = 50;
        state.FactionReputation["chitin"] = 50;
        state.FactionReputation["weaver"] = 50;
        return state;
    }

    [Test]
    public void NoActiveCommission_IsNoOp()
    {
        var state = CreateState();
        state.ActiveCommission = null;
        long creditsBefore = state.PlayerCredits;

        // Advance to cycle tick
        while (state.Tick % CommissionTweaksV0.CommissionCycleTicks != 0)
            state.AdvanceTick();

        CommissionSystem.Process(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore));
        Assert.That(state.FactionReputation["concord"], Is.EqualTo(50));
    }

    [Test]
    public void CycleTick_PaysStipendAndDriftsRep()
    {
        var state = CreateState();
        state.ActiveCommission = new Commission
        {
            FactionId = "concord",
            StipendCreditsPerCycle = 100,
            StartTick = 0
        };
        long creditsBefore = state.PlayerCredits;

        // Advance to cycle tick
        while (state.Tick % CommissionTweaksV0.CommissionCycleTicks != 0)
            state.AdvanceTick();

        CommissionSystem.Process(state);

        // Stipend paid
        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore + 100));
        // Employer rep gained
        Assert.That(state.FactionReputation["concord"], Is.GreaterThan(50));
        // Rival rep lost
        Assert.That(state.FactionReputation["chitin"], Is.LessThan(50));
        Assert.That(state.FactionReputation["weaver"], Is.LessThan(50));
    }

    [Test]
    public void OffCycleTick_DoesNothing()
    {
        var state = CreateState();
        state.ActiveCommission = new Commission
        {
            FactionId = "concord",
            StipendCreditsPerCycle = 100,
            StartTick = 0
        };
        long creditsBefore = state.PlayerCredits;

        // Advance to one past cycle tick so we're off-cycle
        while (state.Tick % CommissionTweaksV0.CommissionCycleTicks != 0)
            state.AdvanceTick();
        state.AdvanceTick(); // now off-cycle

        CommissionSystem.Process(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore));
    }

    [Test]
    public void ZeroStipend_FallsBackToDefault()
    {
        var state = CreateState();
        state.ActiveCommission = new Commission
        {
            FactionId = "concord",
            StipendCreditsPerCycle = 0, // should use DefaultStipendCredits
            StartTick = 0
        };
        long creditsBefore = state.PlayerCredits;

        while (state.Tick % CommissionTweaksV0.CommissionCycleTicks != 0)
            state.AdvanceTick();

        CommissionSystem.Process(state);

        Assert.That(state.PlayerCredits,
            Is.EqualTo(creditsBefore + CommissionTweaksV0.DefaultStipendCredits));
    }
}
