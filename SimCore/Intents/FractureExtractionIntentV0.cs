using SimCore.Content;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Intents;

/// <summary>
/// GATE.EXTRACT.FRACTURE_PROGRAM.001: Fracture extraction program intent.
/// Requires FractureUnlocked + fleet assignment.
/// Per-cycle cost: 3x fuel + 3 exotic_crystals.
/// Per-cycle yield: 5-8 exotic_matter to player cargo.
/// Per-cycle side effect: +1 DeepExposure.
/// </summary>
public sealed class FractureExtractionIntentV0 : IIntent
{
    public string Kind => "FRACTURE_EXTRACTION_V0";

    public readonly string FleetId;
    public readonly string ProgramId;

    public FractureExtractionIntentV0(string fleetId, string programId)
    {
        FleetId = fleetId ?? "";
        ProgramId = programId ?? "";
    }

    public void Apply(SimState state)
    {
        if (state is null) return;

        // Require fracture unlocked.
        if (!state.FractureUnlocked)
        {
            state.AppendExploitationEvent($"tick={state.Tick} prog={ProgramId} FractureExtraction BLOCKED fracture_locked");
            return;
        }

        // Require fleet assignment.
        if (string.IsNullOrWhiteSpace(FleetId) || !state.Fleets.ContainsKey(FleetId))
        {
            state.AppendExploitationEvent($"tick={state.Tick} prog={ProgramId} FractureExtraction BLOCKED no_fleet");
            return;
        }

        // Check fuel cost.
        int fuelCost = ExtractionTweaksV0.FractureExtractionFuelCost;
        int fuelHave = InventoryLedger.Get(state.PlayerCargo, WellKnownGoodIds.Fuel);
        if (fuelHave < fuelCost)
        {
            state.AppendExploitationEvent($"tick={state.Tick} prog={ProgramId} FractureExtraction BLOCKED insufficient_fuel need={fuelCost} have={fuelHave}");
            return;
        }

        // Check exotic_crystals cost.
        int crystalCost = ExtractionTweaksV0.FractureExtractionCrystalCost;
        int crystalHave = InventoryLedger.Get(state.PlayerCargo, WellKnownGoodIds.ExoticCrystals);
        if (crystalHave < crystalCost)
        {
            state.AppendExploitationEvent($"tick={state.Tick} prog={ProgramId} FractureExtraction BLOCKED insufficient_crystals need={crystalCost} have={crystalHave}");
            return;
        }

        // Deduct costs.
        InventoryLedger.TryRemoveCargo(state.PlayerCargo, WellKnownGoodIds.Fuel, fuelCost);
        InventoryLedger.TryRemoveCargo(state.PlayerCargo, WellKnownGoodIds.ExoticCrystals, crystalCost);

        // Deterministic yield: use tick modulo to get a value in [YieldMin, YieldMax].
        int range = ExtractionTweaksV0.FractureExtractionYieldMax - ExtractionTweaksV0.FractureExtractionYieldMin + 1;
        int yield = ExtractionTweaksV0.FractureExtractionYieldMin + (state.Tick % range);

        InventoryLedger.AddCargo(state.PlayerCargo, WellKnownGoodIds.ExoticMatter, yield);

        // Side effect: +1 DeepExposure.
        state.DeepExposure += ExtractionTweaksV0.FractureExtractionExposurePerCycle;

        state.AppendExploitationEvent($"tick={state.Tick} prog={ProgramId} FractureExtraction yield={yield} fuel_spent={fuelCost} crystals_spent={crystalCost} exposure={state.DeepExposure}");
    }
}
