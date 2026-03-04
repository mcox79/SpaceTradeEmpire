using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S4.MAINT.SYSTEM.001: Maintenance system — tick decay, repair, efficiency coupling.
public static class MaintenanceSystem
{
    public sealed class RepairResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
        public int CreditsCost { get; set; }
        public int BpsRestored { get; set; }
    }

    /// <summary>
    /// Processes degradation for all active industry sites. Called once per sim tick.
    /// </summary>
    public static void ProcessDecay(SimState state)
    {
        foreach (var kv in state.IndustrySites)
        {
            var site = kv.Value;
            if (!site.Active) continue;
            if (site.DegradePerDayBps <= MaintenanceTweaksV0.MinDegradeThresholdBps) continue;

            site.HealthBps = Math.Max(MaintenanceTweaksV0.MinHealthBps, site.HealthBps - site.DegradePerDayBps);
            UpdateEfficiency(site);
        }
    }

    /// <summary>
    /// Repairs a specific industry site to full health. Deducts credits from player.
    /// </summary>
    public static RepairResult RepairSite(SimState state, string siteId)
    {
        if (string.IsNullOrEmpty(siteId))
            return new RepairResult { Success = false, Reason = "empty_site_id" };

        if (!state.IndustrySites.TryGetValue(siteId, out var site))
            return new RepairResult { Success = false, Reason = "site_not_found" };

        if (site.HealthBps >= MaintenanceTweaksV0.MaxHealthBps)
            return new RepairResult { Success = false, Reason = "already_full_health" };

        int bpsToRepair = MaintenanceTweaksV0.MaxHealthBps - site.HealthBps;
        int cost = (bpsToRepair / MaintenanceTweaksV0.BpsBucketSize) * MaintenanceTweaksV0.RepairCostPer1000Bps;
        if (cost <= MaintenanceTweaksV0.MinHealthBps && bpsToRepair > MaintenanceTweaksV0.MinHealthBps)
            cost = MaintenanceTweaksV0.MinRepairCost;

        if (state.PlayerCredits < cost)
            return new RepairResult { Success = false, Reason = "insufficient_credits" };

        state.PlayerCredits -= cost;
        site.HealthBps = MaintenanceTweaksV0.MaxHealthBps;
        UpdateEfficiency(site);

        return new RepairResult
        {
            Success = true,
            CreditsCost = cost,
            BpsRestored = bpsToRepair
        };
    }

    /// <summary>
    /// Updates efficiency based on health. Above critical: 100%. Below critical: linear penalty.
    /// </summary>
    public static void UpdateEfficiency(IndustrySite site)
    {
        if (site.HealthBps >= MaintenanceTweaksV0.CriticalHealthBps)
        {
            site.Efficiency = 1.0f;
            return;
        }

        int deficit = MaintenanceTweaksV0.CriticalHealthBps - site.HealthBps;
        int penaltyPct = (deficit / MaintenanceTweaksV0.BpsBucketSize) * MaintenanceTweaksV0.EfficiencyPenaltyPer1000BpsBelowCritical;
        int effPct = Math.Max(MaintenanceTweaksV0.MinHealthBps, MaintenanceTweaksV0.FullEfficiencyPct - penaltyPct);
        site.Efficiency = effPct / (float)MaintenanceTweaksV0.FullEfficiencyPct;
    }

    /// <summary>
    /// Returns the repair cost for a site (read-only query).
    /// </summary>
    public static int GetRepairCost(IndustrySite site)
    {
        if (site.HealthBps >= MaintenanceTweaksV0.MaxHealthBps) return MaintenanceTweaksV0.MinHealthBps;
        int bpsToRepair = MaintenanceTweaksV0.MaxHealthBps - site.HealthBps;
        int cost = (bpsToRepair / MaintenanceTweaksV0.BpsBucketSize) * MaintenanceTweaksV0.RepairCostPer1000Bps;
        if (cost <= MaintenanceTweaksV0.MinHealthBps && bpsToRepair > MaintenanceTweaksV0.MinHealthBps)
            cost = MaintenanceTweaksV0.MinRepairCost;
        return cost;
    }
}
