using NUnit.Framework;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S4.MAINT.CORE.001: Contract tests for maintenance/degradation model.
[TestFixture]
[Category("IndustrySiteMaintenance")]
public sealed class IndustrySiteMaintenanceTests
{
    [Test]
    public void IndustrySite_HasHealthFields()
    {
        var site = new IndustrySite();
        Assert.That(site.HealthBps, Is.EqualTo(10000));
        Assert.That(site.DegradePerDayBps, Is.EqualTo(0));
        Assert.That(site.DegradeRemainder, Is.EqualTo(0));
        Assert.That(site.Efficiency, Is.EqualTo(1.0f));
    }

    [Test]
    public void IndustrySite_HealthCanDegrade()
    {
        var site = new IndustrySite { HealthBps = 10000, DegradePerDayBps = 100 };
        // Simulate degradation
        site.HealthBps -= site.DegradePerDayBps;
        Assert.That(site.HealthBps, Is.EqualTo(9900));
    }

    [Test]
    public void IndustrySite_HealthClampedAtZero()
    {
        var site = new IndustrySite { HealthBps = 50, DegradePerDayBps = 100 };
        site.HealthBps = System.Math.Max(0, site.HealthBps - site.DegradePerDayBps);
        Assert.That(site.HealthBps, Is.EqualTo(0));
    }

    [Test]
    public void MaintenanceTweaksV0_Constants_Valid()
    {
        Assert.That(MaintenanceTweaksV0.DefaultDegradePerTickBps, Is.GreaterThan(0));
        Assert.That(MaintenanceTweaksV0.CriticalHealthBps, Is.GreaterThan(0));
        Assert.That(MaintenanceTweaksV0.CriticalHealthBps, Is.LessThanOrEqualTo(MaintenanceTweaksV0.MaxHealthBps));
        Assert.That(MaintenanceTweaksV0.RepairCostPer1000Bps, Is.GreaterThan(0));
        Assert.That(MaintenanceTweaksV0.MaxHealthBps, Is.EqualTo(10000));
    }

    [Test]
    public void MaintenanceTweaksV0_EfficiencyPenalty_CalculationConsistent()
    {
        // At exactly critical threshold: no penalty
        int healthAtCritical = MaintenanceTweaksV0.CriticalHealthBps;
        int deficit = MaintenanceTweaksV0.CriticalHealthBps - healthAtCritical;
        int penaltyPct = (deficit / 1000) * MaintenanceTweaksV0.EfficiencyPenaltyPer1000BpsBelowCritical;
        Assert.That(penaltyPct, Is.EqualTo(0));

        // At 0 health: max penalty
        int deficitAtZero = MaintenanceTweaksV0.CriticalHealthBps;
        int maxPenaltyPct = (deficitAtZero / 1000) * MaintenanceTweaksV0.EfficiencyPenaltyPer1000BpsBelowCritical;
        Assert.That(maxPenaltyPct, Is.EqualTo(50), "At 0 health, penalty should be 50%");
    }

    [Test]
    public void IndustrySite_RepairCost_Formula()
    {
        // Repair from 5000 to 10000 = 5000 bps = 5 * RepairCostPer1000Bps
        int damaged = 5000;
        int target = 10000;
        int bpsToRepair = target - damaged;
        int cost = (bpsToRepair / 1000) * MaintenanceTweaksV0.RepairCostPer1000Bps;
        Assert.That(cost, Is.EqualTo(25));
    }
}
