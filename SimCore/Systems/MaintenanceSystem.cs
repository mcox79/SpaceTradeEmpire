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
    /// GATE.S4.MAINT_SUSTAIN.SUPPLY_REPAIR.001: Also consumes supply and doubles decay when supply is 0.
    /// </summary>
    public static void ProcessDecay(SimState state)
    {
        foreach (var kv in state.IndustrySites)
        {
            var site = kv.Value;
            if (!site.Active) continue;

            // Supply consumption: 1 unit per SupplyConsumptionIntervalTicks
            if (MaintenanceTweaksV0.SupplyConsumptionIntervalTicks > 0
                && state.Tick % MaintenanceTweaksV0.SupplyConsumptionIntervalTicks == 0
                && site.SupplyLevel > 0)
            {
                site.SupplyLevel = Math.Max(0, site.SupplyLevel - 1);
            }

            if (site.DegradePerDayBps <= MaintenanceTweaksV0.MinDegradeThresholdBps) continue;

            int effectiveDegrade = site.DegradePerDayBps;
            if (site.SupplyLevel <= 0)
                effectiveDegrade *= MaintenanceTweaksV0.NoSupplyDecayMultiplier;

            // Accumulator pattern matching IndustrySystem: spread daily rate across TicksPerDay.
            // Uses same denominator (TicksPerDay * Bps) so DegradeRemainder is shared correctly.
            long maintNumer = (long)effectiveDegrade * IndustrySystem.Bps;
            long denom = (long)IndustrySystem.TicksPerDay * IndustrySystem.Bps;
            site.DegradeRemainder = checked(site.DegradeRemainder + maintNumer);
            int bpsLoss = (int)(site.DegradeRemainder / denom);
            site.DegradeRemainder = site.DegradeRemainder % denom;
            if (bpsLoss > 0)
                site.HealthBps = Math.Max(MaintenanceTweaksV0.MinHealthBps, site.HealthBps - bpsLoss);
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
    /// GATE.S4.MAINT_SUSTAIN.SUPPLY_REPAIR.001: Repairs a site by consuming supply goods instead of credits.
    /// Each supply unit restores BpsPerSupplyUnit basis points of health.
    /// </summary>
    public static RepairResult RepairWithSupply(SimState state, string siteId, int supplyUnitsToUse)
    {
        if (string.IsNullOrEmpty(siteId))
            return new RepairResult { Success = false, Reason = "empty_site_id" };

        if (!state.IndustrySites.TryGetValue(siteId, out var site))
            return new RepairResult { Success = false, Reason = "site_not_found" };

        if (supplyUnitsToUse <= 0)
            return new RepairResult { Success = false, Reason = "invalid_amount" };

        if (site.SupplyLevel < supplyUnitsToUse)
            return new RepairResult { Success = false, Reason = "insufficient_supply" };

        if (site.HealthBps >= MaintenanceTweaksV0.MaxHealthBps)
            return new RepairResult { Success = false, Reason = "already_full_health" };

        int maxBpsRestorable = MaintenanceTweaksV0.MaxHealthBps - site.HealthBps;
        int bpsFromSupply = supplyUnitsToUse * MaintenanceTweaksV0.BpsPerSupplyUnit;
        int bpsRestored = Math.Min(bpsFromSupply, maxBpsRestorable);

        site.SupplyLevel -= supplyUnitsToUse;
        site.HealthBps += bpsRestored;
        UpdateEfficiency(site);

        return new RepairResult
        {
            Success = true,
            CreditsCost = 0,
            BpsRestored = bpsRestored
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
