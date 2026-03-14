using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.WIN.LOSS_DETECT.001: Detect player death (hull 0) and bankruptcy (credits below threshold).
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
                state.GameResultValue = GameResult.Death;
                return;
            }
        }

        // --- Bankruptcy: credits below threshold with insufficient cargo to recover ---
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
                state.GameResultValue = GameResult.Bankruptcy;
            }
        }
    }
}
