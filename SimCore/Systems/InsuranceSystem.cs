using System;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T62.LOSS.INSURANCE_MODEL.001: Fleet insurance — premium deduction + payout on destruction.
// Runs once per tick. Premium deducted every PremiumIntervalTicks.
// On fleet destruction, payout = (ship value * PayoutRateBps / BpsDivisor) - DeductibleCredits.
public static class InsuranceSystem
{
    public static void Process(SimState state)
    {
        if (state == null) return;

        ProcessPremiums(state);
    }

    private static void ProcessPremiums(SimState state)
    {
        // Only deduct premiums at the interval boundary.
        if (state.Tick % InsuranceTweaksV0.PremiumIntervalTicks != 0) return; // STRUCTURAL: modulo tick check

        // Skip if player can't afford minimum.
        if (state.PlayerCredits < InsuranceTweaksV0.MinCreditsForPremium) return;

        int totalPremium = 0; // STRUCTURAL: accumulator init

        foreach (var fleet in state.Fleets.Values)
        {
            if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (fleet.IsStored) continue; // Stored ships don't need insurance.

            int shipValue = ShipyardSystem.GetPurchasePrice(fleet.ShipClassId);
            if (shipValue <= 0) continue; // STRUCTURAL: skip unpriceable ships

            int premium = shipValue * InsuranceTweaksV0.PremiumRateBps / InsuranceTweaksV0.BpsDivisor;
            if (premium <= 0) premium = 1; // STRUCTURAL: minimum 1 credit premium
            totalPremium += premium;
        }

        if (totalPremium > 0 && state.PlayerCredits >= totalPremium)
        {
            state.PlayerCredits -= totalPremium;
            state.AppendTransaction(new TransactionRecord
            {
                CashDelta = -totalPremium,
                GoodId = "",
                Quantity = 0,
                Source = "InsurancePremium",
                NodeId = "",
            });
        }
    }

    /// <summary>
    /// Calculate insurance payout for a destroyed ship. Called by LossDetectionSystem or respawn logic.
    /// Returns the net payout (after deductible). Minimum 0.
    /// </summary>
    public static int CalculatePayout(string shipClassId)
    {
        int shipValue = ShipyardSystem.GetPurchasePrice(shipClassId);
        if (shipValue <= 0) return 0; // STRUCTURAL: no payout for unknown ships

        int grossPayout = shipValue * InsuranceTweaksV0.PayoutRateBps / InsuranceTweaksV0.BpsDivisor;
        int netPayout = grossPayout - InsuranceTweaksV0.DeductibleCredits;
        return Math.Max(0, netPayout); // STRUCTURAL: floor at 0
    }

    /// <summary>
    /// Get the current premium amount for a ship class (per cycle).
    /// </summary>
    public static int GetPremiumAmount(string shipClassId)
    {
        int shipValue = ShipyardSystem.GetPurchasePrice(shipClassId);
        if (shipValue <= 0) return 0;
        int premium = shipValue * InsuranceTweaksV0.PremiumRateBps / InsuranceTweaksV0.BpsDivisor;
        return Math.Max(1, premium); // STRUCTURAL: minimum 1
    }
}
