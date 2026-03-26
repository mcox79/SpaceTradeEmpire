using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// GATE.T58.FO.DOCK_RECAP.001: "While You Were Away" dock arrival recap.
// Per fo_trade_manager_v0.md §Dock Arrival Recap: when the player docks after 100+ ticks
// since last dock, the FO delivers a batch summary (max 3 lines).
//
// Structure: positive lead → one issue → one opportunity → "Details in Empire tab."
// Fires BEFORE dock menu opens (3-second dock window).
public static class DockRecapSystem
{
    public static void Process(SimState state)
    {
        if (state.FirstOfficer is null) return;

        var recap = state.FirstOfficer.DockRecap;

        // Detect docking: player fleet is Docked.
        bool playerDocked = false;
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.OwnerId == "player" && fleet.State == FleetState.Docked)
            {
                playerDocked = true;
                break;
            }
        }

        if (!playerDocked)
        {
            // Not docked — accumulate metrics from transaction log.
            AccumulateMetrics(state, recap);
            return;
        }

        // Player just docked — check if recap should fire.
        int ticksSinceLastDock = state.Tick - recap.LastDockTick;

        if (ticksSinceLastDock >= FOManagerTweaksV0.RecapMinTicksSinceLastDock && !recap.PendingRecap
            && recap.LastDockTick > 0) // STRUCTURAL: skip first dock (no history)
        {
            GenerateRecap(state, recap);
        }

        // Update last dock tick (resets every time we see docked state).
        if (ticksSinceLastDock > 1) // STRUCTURAL: avoid resetting every tick while docked
        {
            recap.LastDockTick = state.Tick;
            recap.TradesCompletedSinceLastDock = 0;
            recap.CreditsEarnedSinceLastDock = 0;
            recap.MostSevereIssue = "";
            recap.BestOpportunity = "";
        }
    }

    private static void AccumulateMetrics(SimState state, DockRecapState recap)
    {
        // Count completed trades and credits earned from recent transactions.
        if (state.TransactionLog is null) return;

        // Scan last few transactions for ones after LastDockTick.
        for (int i = state.TransactionLog.Count - 1; i >= 0; i--) // STRUCTURAL: reverse scan
        {
            var tx = state.TransactionLog[i];
            if (tx.Tick <= recap.LastDockTick) break;
            if (tx.Source == "Sell")
            {
                recap.TradesCompletedSinceLastDock++;
                recap.CreditsEarnedSinceLastDock += tx.ProfitDelta;
            }
        }

        // Track most severe issue from empire health.
        if (state.FirstOfficer?.EmpireHealth is not null)
        {
            var health = state.FirstOfficer.EmpireHealth;
            if (health.DeadRoutes > 0)
                recap.MostSevereIssue = $"{health.DeadRoutes} route(s) dead";
            else if (health.DegradedRoutes > 0)
                recap.MostSevereIssue = $"{health.DegradedRoutes} route(s) degraded";
        }
    }

    private static void GenerateRecap(SimState state, DockRecapState recap)
    {
        recap.RecapLines.Clear();
        recap.PendingRecap = true;

        // Line 1: Positive lead (credits earned, trades completed).
        if (recap.TradesCompletedSinceLastDock > 0)
        {
            recap.RecapLines.Add(
                $"{recap.TradesCompletedSinceLastDock} trades completed, net {(recap.CreditsEarnedSinceLastDock >= 0 ? "+" : "")}{recap.CreditsEarnedSinceLastDock}cr.");
        }
        else
        {
            recap.RecapLines.Add("No trades completed while you were out.");
        }

        // Line 2: One issue (most severe).
        if (!string.IsNullOrEmpty(recap.MostSevereIssue))
        {
            recap.RecapLines.Add($"Issue: {recap.MostSevereIssue}.");
        }

        // Line 3: One opportunity (recent discovery or new route).
        if (!string.IsNullOrEmpty(recap.BestOpportunity))
        {
            recap.RecapLines.Add($"Opportunity: {recap.BestOpportunity}.");
        }

        // Cap at max lines.
        while (recap.RecapLines.Count > FOManagerTweaksV0.RecapMaxLines)
            recap.RecapLines.RemoveAt(recap.RecapLines.Count - 1); // STRUCTURAL: trim to max

        // Fire FO trigger for recap delivery.
        FirstOfficerSystem.TryFireTrigger(state, "DOCK_RECAP");
    }
}
