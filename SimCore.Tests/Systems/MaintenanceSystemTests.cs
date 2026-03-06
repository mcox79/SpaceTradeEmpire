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
        // Ensure supply doesn't deplete mid-day (would double decay rate)
        state.IndustrySites["site_a"].SupplyLevel = 9999;
        // Run for a full game day — daily rate should apply once
        for (int i = 0; i < IndustrySystem.TicksPerDay; i++)
        {
            MaintenanceSystem.ProcessDecay(state);
            state.AdvanceTick();
        }
        Assert.That(state.IndustrySites["site_a"].HealthBps,
            Is.EqualTo(10000 - MaintenanceTweaksV0.DefaultDegradePerTickBps));
    }

    [Test]
    public void ProcessDecay_ClampsAtZero()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5;
        state.IndustrySites["site_a"].DegradePerDayBps = 100;
        // Run for a full day — 100 bps daily loss exceeds 5 bps health
        for (int i = 0; i < IndustrySystem.TicksPerDay; i++)
        {
            MaintenanceSystem.ProcessDecay(state);
            state.AdvanceTick();
        }
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

    // --- GATE.S4.MAINT_SUSTAIN.SUPPLY_REPAIR.001: Supply-based repair tests ---

    [Test]
    public void RepairWithSupply_RestoresCondition()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5000;
        state.IndustrySites["site_a"].SupplyLevel = 50;
        var result = MaintenanceSystem.RepairWithSupply(state, "site_a", 4);
        Assert.That(result.Success, Is.True);
        // 4 units * 500 BPS/unit = 2000 BPS restored
        Assert.That(result.BpsRestored, Is.EqualTo(2000));
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(7000));
        Assert.That(result.CreditsCost, Is.EqualTo(0));
    }

    [Test]
    public void RepairWithSupply_InsufficientSupply_Fails()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5000;
        state.IndustrySites["site_a"].SupplyLevel = 2;
        var result = MaintenanceSystem.RepairWithSupply(state, "site_a", 5);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("insufficient_supply"));
        // Supply should be unchanged on failure
        Assert.That(state.IndustrySites["site_a"].SupplyLevel, Is.EqualTo(2));
    }

    [Test]
    public void RepairWithSupply_DeductsSupply()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 5000;
        state.IndustrySites["site_a"].SupplyLevel = 30;
        var result = MaintenanceSystem.RepairWithSupply(state, "site_a", 6);
        Assert.That(result.Success, Is.True);
        Assert.That(state.IndustrySites["site_a"].SupplyLevel, Is.EqualTo(24));
    }

    [Test]
    public void RepairWithSupply_CapsAtMaxHealth()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].HealthBps = 9500;
        state.IndustrySites["site_a"].SupplyLevel = 50;
        // Request 10 units * 500 = 5000 BPS, but only 500 BPS needed to cap
        var result = MaintenanceSystem.RepairWithSupply(state, "site_a", 10);
        Assert.That(result.Success, Is.True);
        Assert.That(result.BpsRestored, Is.EqualTo(500));
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(10000));
    }

    [Test]
    public void ProcessDecay_ConsumesSupply()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].SupplyLevel = 50;
        // Tick starts at 0; Tick % 10 == 0 → supply consumed on first call
        MaintenanceSystem.ProcessDecay(state);
        Assert.That(state.IndustrySites["site_a"].SupplyLevel, Is.EqualTo(49));

        // Advance tick to a non-consumption tick (tick 1)
        state.AdvanceTick();
        MaintenanceSystem.ProcessDecay(state);
        // Supply unchanged — tick 1 % 10 != 0
        Assert.That(state.IndustrySites["site_a"].SupplyLevel, Is.EqualTo(49));
    }

    [Test]
    public void ProcessDecay_NoSupply_FasterDecay()
    {
        var state = CreateState();
        state.IndustrySites["site_a"].SupplyLevel = 0;
        state.IndustrySites["site_a"].HealthBps = 10000;
        int degradeRate = state.IndustrySites["site_a"].DegradePerDayBps;

        // Run for a full day to see daily rate applied
        for (int i = 0; i < IndustrySystem.TicksPerDay; i++)
        {
            MaintenanceSystem.ProcessDecay(state);
            state.AdvanceTick();
        }

        // With 0 supply, decay rate is doubled (per day)
        int expected = 10000 - (degradeRate * MaintenanceTweaksV0.NoSupplyDecayMultiplier);
        Assert.That(state.IndustrySites["site_a"].HealthBps, Is.EqualTo(expected));
    }
}
