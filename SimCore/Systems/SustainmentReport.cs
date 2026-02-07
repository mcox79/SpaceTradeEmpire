using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;

namespace SimCore.Systems;

public sealed class SustainmentInputStatus
{
    public string GoodId { get; set; } = "";
    public int HaveUnits { get; set; }
    public int PerTickRequired { get; set; }
    public int BufferTargetUnits { get; set; }
    public int CoverageTicks { get; set; }              // floor(Have / PerTickRequired), or int.MaxValue if PerTickRequired==0
    public float CoverageDays { get; set; }             // CoverageTicks / TicksPerDay
    public float BufferMargin { get; set; }             // (Have - Target) / max(1, Target)
}

public sealed class SustainmentSiteStatus
{
    public string SiteId { get; set; } = "";
    public string NodeId { get; set; } = "";

    public int HealthBps { get; set; }
    public int EffBpsNow { get; set; }                  // computed using the same logic as IndustrySystem
    public int DegradePerDayBps { get; set; }

    public float WorstBufferMargin { get; set; }        // min over inputs of BufferMargin
    public int TimeToStarveTicks { get; set; }          // min over inputs of CoverageTicks
    public float TimeToStarveDays { get; set; }

    public int TimeToFailureTicks { get; set; }         // starvation stage + degradation-to-zero stage
    public float TimeToFailureDays { get; set; }

    public List<SustainmentInputStatus> Inputs { get; set; } = new();
}

public static class SustainmentReport
{
    public static List<SustainmentSiteStatus> BuildForNode(SimState state, string nodeId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("nodeId must be non-empty.", nameof(nodeId));

        if (!state.Markets.TryGetValue(nodeId, out var market))
        {
            return new List<SustainmentSiteStatus>();
        }

        // Deterministic ordering
        var sites = state.IndustrySites.Values
            .Where(s => s.Active && string.Equals(s.NodeId, nodeId, StringComparison.Ordinal))
            .OrderBy(s => s.Id, StringComparer.Ordinal)
            .ToList();

        var result = new List<SustainmentSiteStatus>(sites.Count);

        foreach (var site in sites)
        {
            result.Add(BuildForSite(site, market));
        }

        return result;
    }

    private static SustainmentSiteStatus BuildForSite(IndustrySite site, Market market)
    {
        // Inputs, deterministic ordering
        var inputs = site.Inputs
            .Where(kv => kv.Value > 0)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        var inputStatuses = new List<SustainmentInputStatus>(inputs.Count);

        int timeToStarveTicks = int.MaxValue;
        float worstMargin = float.PositiveInfinity;

        foreach (var (goodId, perTick) in inputs)
        {
            int have = InventoryLedger.Get(market.Inventory, goodId);
            int target = IndustrySystem.ComputeBufferTargetUnits(site, goodId);

            int coverageTicks = (perTick <= 0) ? int.MaxValue : (have / perTick);
            float coverageDays = (coverageTicks == int.MaxValue) ? float.PositiveInfinity : (coverageTicks / (float)IndustrySystem.TicksPerDay);

            float denom = Math.Max(1, target);
            float margin = (have - target) / denom;

            if (coverageTicks < timeToStarveTicks) timeToStarveTicks = coverageTicks;
            if (margin < worstMargin) worstMargin = margin;

            inputStatuses.Add(new SustainmentInputStatus
            {
                GoodId = goodId,
                HaveUnits = have,
                PerTickRequired = perTick,
                BufferTargetUnits = target,
                CoverageTicks = coverageTicks,
                CoverageDays = coverageDays,
                BufferMargin = margin
            });
        }

        if (inputs.Count == 0)
        {
            timeToStarveTicks = int.MaxValue;
            worstMargin = 0f;
        }

        float timeToStarveDays = (timeToStarveTicks == int.MaxValue) ? float.PositiveInfinity : (timeToStarveTicks / (float)IndustrySystem.TicksPerDay);

        int effBpsNow = ComputeEffBpsNow(site, market);

        int timeToFailureTicks = ComputeTimeToFailureTicks(site, market, timeToStarveTicks, effBpsNow);
        float timeToFailureDays = (timeToFailureTicks == int.MaxValue) ? float.PositiveInfinity : (timeToFailureTicks / (float)IndustrySystem.TicksPerDay);

        return new SustainmentSiteStatus
        {
            SiteId = site.Id,
            NodeId = site.NodeId,

            HealthBps = site.HealthBps,
            EffBpsNow = effBpsNow,
            DegradePerDayBps = site.DegradePerDayBps,

            WorstBufferMargin = worstMargin,
            TimeToStarveTicks = timeToStarveTicks,
            TimeToStarveDays = timeToStarveDays,

            TimeToFailureTicks = timeToFailureTicks,
            TimeToFailureDays = timeToFailureDays,

            Inputs = inputStatuses
        };
    }

    // Matches IndustrySystem.Process efficiency logic (uses current inventory vs per-tick required)
    private static int ComputeEffBpsNow(IndustrySite site, Market market)
    {
        int effBps = IndustrySystem.Bps;

        foreach (var input in site.Inputs)
        {
            if (input.Value <= 0) continue;

            int available = InventoryLedger.Get(market.Inventory, input.Key);
            int required = input.Value;

            int ratioBps;
            if (available <= 0) ratioBps = 0;
            else ratioBps = (int)Math.Min((long)IndustrySystem.Bps, ((long)available * IndustrySystem.Bps) / required);

            if (ratioBps < effBps) effBps = ratioBps;
            if (effBps == 0) break;
        }

        if (effBps < 0) effBps = 0;
        if (effBps > IndustrySystem.Bps) effBps = IndustrySystem.Bps;
        return effBps;
    }

    // Deterministic closed-form estimate:
    // Stage 1: run at eff=100% until first starvation tick (min coverage). No degradation during this stage.
    // Stage 2: assume full deficit (eff=0) after starvation and compute exact ticks to health 0 with remainder math.
    // If already undersupplied now (effBpsNow < 10000), use current deficit for stage 2 (no starvation delay).
    private static int ComputeTimeToFailureTicks(IndustrySite site, Market market, int timeToStarveTicks, int effBpsNow)
    {
        if (site.HealthBps <= 0) return 0;

        // If no degradation configured, define "failure" as starvation only (or infinity if no inputs).
        if (site.DegradePerDayBps <= 0)
        {
            if (timeToStarveTicks == int.MaxValue) return int.MaxValue;
            return timeToStarveTicks;
        }

        // If no inputs, there is nothing to starve, and with eff=100% and no deficit, no degradation.
        if (timeToStarveTicks == int.MaxValue)
        {
            return int.MaxValue;
        }

        // Already undersupplied now: no starvation delay; use current deficit
        if (effBpsNow < IndustrySystem.Bps)
        {
            int deficitBps = IndustrySystem.Bps - effBpsNow;
            int ticksToZero = TicksToHealthZero(site.HealthBps, site.DegradePerDayBps, deficitBps, site.DegradeRemainder);
            return ticksToZero;
        }

        // Supplied now: degrade starts only once we cross into deficit regime.
        // Use starvation time as delay, then assume full deficit afterwards.
        int fullDeficitBps = IndustrySystem.Bps;

        int stage2Ticks = TicksToHealthZero(site.HealthBps, site.DegradePerDayBps, fullDeficitBps, site.DegradeRemainder);

        // Clamp overflow to "infinite"
        if (timeToStarveTicks > (int.MaxValue - stage2Ticks)) return int.MaxValue;

        return timeToStarveTicks + stage2Ticks;
    }

    // Exact ticks to reduce health to zero under constant deficitBps, using the same denom as IndustrySystem.ApplyDegradation:
    // denom = TicksPerDay * 10000
    // remainder accumulates numer = DegradePerDayBps * deficitBps each tick
    // health decreases by floor((N*numer + rem0)/denom)
    private static int TicksToHealthZero(int healthBps, int degradePerDayBps, int deficitBps, long rem0)
    {
        if (healthBps <= 0) return 0;
        if (degradePerDayBps <= 0) return int.MaxValue;
        if (deficitBps <= 0) return int.MaxValue;

        long numer = (long)degradePerDayBps * (long)deficitBps;
        long denom = (long)IndustrySystem.TicksPerDay * (long)IndustrySystem.Bps;

        if (numer <= 0) return int.MaxValue;

        // Need smallest N such that floor((N*numer + rem0)/denom) >= healthBps
        // Solve: N*numer + rem0 >= healthBps*denom
        long target = (long)healthBps * denom;

        long needed = target - rem0;
        if (needed <= 0) return 0;

        long n = (needed + numer - 1) / numer; // ceil

        if (n > int.MaxValue) return int.MaxValue;
        return (int)n;
    }
}
