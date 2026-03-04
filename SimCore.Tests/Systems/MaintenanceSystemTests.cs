using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S4.MAINT.SYSTEM.001: Contract tests for MaintenanceSystem.
[TestFixture]
[Category("MaintenanceSystem")]
public sealed class MaintenanceSystemTests
{
    private SimState CreateState()
    {
        var state = new SimState(42);
        state.PlayerCredits = 1000;
        state.IndustrySites["site_a"] = new IndustrySite
        {
            Id = "site_a",
            NodeId = "node_1",
            HealthBps = 10000,
            DegradePerDayBps = MaintenanceTweaksV0.DefaultDegradePerTickBps,
            Active = true,
            Efficiency = 1.0f
        };
        return state;
    }

    [Test]
    public void ProcessDecay_ReducesHealth()
    {
        var state = CreateState();
        MaintenanceSystem.ProcessDecay(state);
        Assert.That(state.IndustrySites["site_a"].HealthBps,
            Is.EqualTo(10000 - MaintenanceTweaksV0.DefaultDegradePerTickBps));
    }

    [Test]
    public void ProcessDecay_ClampsAtZero()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5;
        state.IndustrySites["site_a"].DegradePerDayBps = 100;
        MaintenanceSystem.ProcessDecay(state);
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(0));
    }

    [Test]
    public void ProcessDecay_SkipsInactive()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].Active = false;
        MaintenanceSystem.ProcessDecay(state);
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(10000));
    }

    [Test]
    public void ProcessDecay_SkipsZeroDegradeRate()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].DegradePerDayBps = 0;
        MaintenanceSystem.ProcessDecay(state);
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(10000));
    }

    [Test]
    public void UpdateEfficiency_AboveCritical_FullEfficiency()
    {
        var site = new IndustrySite { HealthBps = 8000 };
        MaintenanceSystem.UpdateEfficiency(site);
        Assert.That(site.Efficiency, Is.EqualTo(1.0f));
    }

    [Test]
    public void UpdateEfficiency_BelowCritical_Penalized()
    {
        var site = new IndustrySite { HealthBps = 3000 };
        MaintenanceSystem.UpdateEfficiency(site);
        // Deficit = 5000 - 3000 = 2000, penalty = (2000/1000) * 10 = 20%
        Assert.That(site.Efficiency, Is.EqualTo(0.8f).Within(0.01f));
    }

    [Test]
    public void UpdateEfficiency_AtZeroHealth_MaxPenalty()
    {
        var site = new IndustrySite { HealthBps = 0 };
        MaintenanceSystem.UpdateEfficiency(site);
        // Deficit = 5000, penalty = 5 * 10 = 50%
        Assert.That(site.Efficiency, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void RepairSite_RestoresToFull()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5000;
        var result = MaintenanceSystem.RepairSite(state, "site_a");
        Assert.That(result.Success, Is.True);
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(10000));
        Assert.That(result.BpsRestored, Is.EqualTo(5000));
    }

    [Test]
    public void RepairSite_DeductsCredits()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5000;
        long before = state.PlayerCredits;
        MaintenanceSystem.RepairSite(state, "site_a");
        Assert.That(state.PlayerCredits, Is.LessThan(before));
    }

    [Test]
    public void RepairSite_Fails_AlreadyFull()
    {
        var state = CreateState();
        var result = MaintenanceSystem.RepairSite(state, "site_a");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("already_full_health"));
    }

    [Test]
    public void RepairSite_Fails_InsufficientCredits()
    {
        var state = CreateState();
        state.PlayerCredits = 0;
        state.IndustrySites["site_a"].HealthBps = 1000;
        var result = MaintenanceSystem.RepairSite(state, "site_a");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("insufficient_credits"));
    }

    [Test]
    public void RepairSite_Fails_UnknownSite()
    {
        var state = CreateState();
        var result = MaintenanceSystem.RepairSite(state, "nonexistent");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("site_not_found"));
    }

    [Test]
    public void GetRepairCost_Correct()
    {
        var site = new IndustrySite { HealthBps = 5000 };
        int cost = MaintenanceSystem.GetRepairCost(site);
        // 5000 bps to repair / 1000 * 5 = 25
        Assert.That(cost, Is.EqualTo(25));
    }

    [Test]
    public void GetRepairCost_Zero_WhenFull()
    {
        var site = new IndustrySite { HealthBps = 10000 };
        Assert.That(MaintenanceSystem.GetRepairCost(site), Is.EqualTo(0));
    }

    [Test]
    public void FullDecayCycle_Deterministic()
    {
        var s1 = CreateState();
        var s2 = CreateState();
        for (int i = 0; i < 100; i++)
        {
            MaintenanceSystem.ProcessDecay(s1);
            MaintenanceSystem.ProcessDecay(s2);
        }
        Assert.That(s1.IndustrySites["site_a"].HealthBps, Is.EqualTo(s2.IndustrySites["site_a"].HealthBps));
        Assert.That(s1.IndustrySites["site_a"].Efficiency, Is.EqualTo(s2.IndustrySites["site_a"].Efficiency));
    }
}
