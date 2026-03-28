using System;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.WIN.LOSS_DETECT.001: Detect player death (hull 0) and bankruptcy (credits below threshold).
// GATE.T62.LOSS.REPLACEMENT_FLOW.001: Haven respawn on death if Haven discovered.
public static class LossDetectionSystem
{
    public static void Process(SimState state)
    {
        // Only check while game is in progress.
        if (state.GameResultValue != GameResult.InProgress) return;

        // --- Death: player fleet hull reaches 0 ---
        if (state.Fleets.TryGetValue("fleet_trader_1", out var playerFleet))
        {
            // HullHp == -1 means uninitialized (no combat yet); only trigger on explicit 0.
            if (playerFleet.HullHp == 0)
            {
                // GATE.T62.LOSS.REPLACEMENT_FLOW.001: Attempt Haven respawn before game over.
                if (TryHavenRespawn(state, playerFleet))
                {
                    TelemetrySystem.LogEvent(state, "respawn", playerFleet.CurrentNodeId ?? "", "haven_insurance");
                    return;
                }

                // No Haven — permanent death.
                TelemetrySystem.LogEvent(state, "death", playerFleet.CurrentNodeId ?? "", "hull_zero");
                state.GameResultValue = GameResult.Death;
                return;
            }
        }

        // --- Bankruptcy: credits below threshold with insufficient cargo to recover ---
        // GATE.T64.ECON.BANKRUPTCY_FIX.001: Bankruptcy detection runs independently of death.
        // Death check above may return early, but if player survived (Haven respawn), bankruptcy
        // should still be checked. The InProgress guard at top prevents overwrite of terminal state.
        if (state.PlayerCredits < WinRequirementsTweaksV0.BankruptcyCreditsThreshold)
        {
            // Check total cargo value across player fleet.
            int cargoValue = 0;
            if (playerFleet != null)
            {
                foreach (var kvp in playerFleet.Cargo)
                {
                    cargoValue += kvp.Value; // Count units, not credits — simple threshold.
                }
            }

            if (cargoValue < WinRequirementsTweaksV0.BankruptcyMinCargoValueToSurvive)
            {
                // GATE.T51.TELEMETRY.QUIT_TRACK.001: Log bankruptcy event.
                TelemetrySystem.LogEvent(state, "death", playerFleet?.CurrentNodeId ?? "", "bankruptcy");
                state.GameResultValue = GameResult.Bankruptcy;
            }
        }
    }

    // GATE.T62.LOSS.REPLACEMENT_FLOW.001: Haven respawn — insurance payout, teleport to Haven, flag for ship selection.
    // Returns true if respawn succeeded (Haven discovered). False = permanent death.
    private static bool TryHavenRespawn(SimState state, Fleet playerFleet)
    {
        // Require Haven discovered.
        if (state.Haven == null || !state.Haven.Discovered) return false;
        if (string.IsNullOrEmpty(state.Haven.NodeId)) return false;

        // Calculate and apply insurance payout.
        int payout = InsuranceSystem.CalculatePayout(playerFleet.ShipClassId);
        if (payout > 0)
        {
            state.PlayerCredits += payout;
            state.AppendTransaction(new TransactionRecord
            {
                CashDelta = payout,
                GoodId = "",
                Quantity = 0,
                Source = "InsurancePayout",
                NodeId = state.Haven.NodeId,
            });
        }

        // Clear cargo (lost with the ship).
        playerFleet.Cargo.Clear();
        playerFleet.CargoOriginPhase.Clear();

        // Teleport to Haven.
        playerFleet.CurrentNodeId = state.Haven.NodeId;
        playerFleet.DestinationNodeId = "";
        playerFleet.FinalDestinationNodeId = "";
        playerFleet.CurrentEdgeId = "";
        playerFleet.RouteEdgeIds.Clear();
        playerFleet.RouteEdgeIndex = 0;
        playerFleet.State = FleetState.Docked;
        playerFleet.ManualOverrideNodeId = "";

        // Restore hull to minimum survival state (shuttle-class baseline).
        var classDef = Content.ShipClassContentV0.GetById(playerFleet.ShipClassId);
        int baseHull = classDef?.CoreHull ?? InsuranceTweaksV0.RespawnMinHull;
        playerFleet.HullHp = Math.Max(InsuranceTweaksV0.RespawnMinHull, baseHull / InsuranceTweaksV0.RespawnHullDivisor); // STRUCTURAL: fraction of max
        playerFleet.HullHpMax = classDef?.CoreHull ?? playerFleet.HullHpMax;

        // Restore shield to zero (must recharge).
        playerFleet.ShieldHp = 0;

        // Set respawn pending flag for UI to show ship selection.
        state.PlayerRespawnPending = true;
        state.PlayerRespawnCount++;

        return true;
    }
}
