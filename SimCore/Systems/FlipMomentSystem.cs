using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T58.FO.FLIP_MOMENT.001: Detect net-negative → net-positive revenue transition.
// Per fo_trade_manager_v0.md §The Flip Moment: when the empire crosses from net-negative
// to sustained net-positive (FlipSustainedTicks consecutive positive ticks with
// FlipMinRoutes managed routes), fire a one-time multi-sensory celebration.
//
// The flip fires ONCE per playthrough (HasFlipped is permanent).
public static class FlipMomentSystem
{
    public static void Process(SimState state)
    {
        if (state.FirstOfficer is null) return;

        var flip = state.FirstOfficer.FlipMoment;
        if (flip.HasFlipped) return; // STRUCTURAL: one-time event

        // Need minimum managed routes before flip can trigger.
        var health = state.FirstOfficer.EmpireHealth;
        if (health.TotalManagedRoutes < FOManagerTweaksV0.FlipMinRoutes) return; // STRUCTURAL: min routes

        // Calculate net revenue from recent transactions.
        long netRevenue = CalculateTickNetRevenue(state);
        flip.LastTickNetRevenue = netRevenue;

        if (netRevenue > 0) // STRUCTURAL: positive threshold
        {
            flip.ConsecutivePositiveTicks++;
        }
        else
        {
            flip.ConsecutivePositiveTicks = 0;
        }

        // Sustained positive → FLIP!
        if (flip.ConsecutivePositiveTicks >= FOManagerTweaksV0.FlipSustainedTicks)
        {
            flip.HasFlipped = true;
            flip.FlipTick = state.Tick;
            flip.FlipEventPending = true;

            // Fire FO character beat.
            FirstOfficerSystem.TryFireTrigger(state, "FLIP_MOMENT");
        }
    }

    /// <summary>
    /// Calculate net revenue for the current evaluation window.
    /// Uses transaction log to sum revenue minus costs from program-driven trades.
    /// </summary>
    private static long CalculateTickNetRevenue(SimState state)
    {
        if (state.TransactionLog is null || state.TransactionLog.Count == 0) return 0;

        long net = 0;
        // Sum ProfitDelta from recent transactions (last HealthEvalCadenceTicks ticks).
        int windowStart = state.Tick - FOManagerTweaksV0.HealthEvalCadenceTicks;

        for (int i = state.TransactionLog.Count - 1; i >= 0; i--) // STRUCTURAL: reverse scan
        {
            var tx = state.TransactionLog[i];
            if (tx.Tick < windowStart) break;
            net += tx.ProfitDelta;
        }

        return net;
    }
}
