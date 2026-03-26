using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T58.FO.EMPIRE_HEALTH.001: Aggregate fleet + route + economy metrics into
// Healthy/Degraded/Critical diamond per fo_trade_manager_v0.md §Empire Health Indicator.
//
// Evaluation cadence: every HealthEvalCadenceTicks ticks.
// Status transitions: None→Healthy on first managed route. Healthy⇄Degraded⇄Critical.
// Recovery is silent (no audio sting). Degradation fires ALERT sting via FO trigger.
public static class EmpireHealthSystem
{
    public static void Process(SimState state)
    {
        if (state.FirstOfficer is null) return;
        if (state.Tick % FOManagerTweaksV0.HealthEvalCadenceTicks != 0) return; // STRUCTURAL: cadence

        var health = state.FirstOfficer.EmpireHealth;

        // Count managed routes by health status.
        int healthy = 0, degraded = 0, dead = 0, total = 0; // STRUCTURAL: counters

        foreach (var route in state.Intel.TradeRoutes.Values)
        {
            if (route.Status == TradeRouteStatus.Discovered) continue; // Not yet managed
            total++;

            switch (route.Status)
            {
                case TradeRouteStatus.Active:
                    if (route.ConfidenceScore < FOManagerTweaksV0.DegradedMarginPct)
                        degraded++;
                    else
                        healthy++;
                    break;
                case TradeRouteStatus.Stale:
                case TradeRouteStatus.Flagged:
                case TradeRouteStatus.Paused:
                    degraded++;
                    break;
                case TradeRouteStatus.Unprofitable:
                    dead++;
                    break;
            }
        }

        health.HealthyRoutes = healthy;
        health.DegradedRoutes = degraded;
        health.DeadRoutes = dead;
        health.TotalManagedRoutes = total;

        // Check sustain state from fleet program metrics (consecutive failures = sustain issues).
        bool sustainLow = false;
        bool sustainCritical = false;
        CheckSustainHealth(state, ref sustainLow, ref sustainCritical);
        health.SustainLow = sustainLow;
        health.SustainCritical = sustainCritical;

        // Check for ship loss (any player fleet destroyed).
        bool shipLost = CheckShipLoss(state);
        health.ShipLost = shipLost;

        // Determine new status.
        var previousStatus = health.Status;

        if (total == 0) // STRUCTURAL: no managed routes → None
        {
            health.Status = EmpireHealthStatus.None;
        }
        else if (dead > 0 || sustainCritical || shipLost) // STRUCTURAL: critical triggers
        {
            health.Status = EmpireHealthStatus.Critical;
        }
        else if (degraded > 0 || sustainLow) // STRUCTURAL: degraded triggers
        {
            health.Status = EmpireHealthStatus.Degraded;
        }
        else
        {
            health.Status = EmpireHealthStatus.Healthy;
        }

        // Track transitions.
        if (health.Status != previousStatus)
        {
            health.PreviousStatus = previousStatus;
            health.LastTransitionTick = state.Tick;

            // Fire FO alert on degradation (Healthy→Degraded or Degraded→Critical).
            if (health.Status > previousStatus && previousStatus != EmpireHealthStatus.None)
            {
                FirstOfficerSystem.TryFireTrigger(state, "EMPIRE_HEALTH_DEGRADED");
            }
        }
    }

    private static void CheckSustainHealth(SimState state, ref bool sustainLow, ref bool sustainCritical)
    {
        // Scan fleet program metrics for consecutive failures indicating sustain issues.
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.OwnerId != "player") continue;
            var metrics = fleet.Metrics;
            if (metrics is null) continue;
            if (metrics.ConsecutiveFailures >= FOManagerTweaksV0.CriticalSustainCycles)
            {
                sustainCritical = true;
                return; // STRUCTURAL: early exit, critical is worst case
            }
            if (metrics.ConsecutiveFailures >= FOManagerTweaksV0.DegradedSustainCycles)
            {
                sustainLow = true;
            }
        }
    }

    private static bool CheckShipLoss(SimState state)
    {
        // Check if any player-owned fleet has been destroyed (hull <= 0).
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.OwnerId != "player") continue;
            if (fleet.HullHp <= 0 && fleet.HullHpMax > 0) return true; // STRUCTURAL: destroyed ship
        }
        return false;
    }
}
