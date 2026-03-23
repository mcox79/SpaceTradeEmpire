using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T45.DEEP_DREAD.EXPOSURE_TRACK.001: Fracture exposure tracking.
// Increments DeepExposure each tick the player is at a Phase 2+ node.
// Higher phases accumulate faster. Milestones at 20/50/100 trigger
// FO observations (handled by FirstOfficerSystem triggers).
// Instrument disagreement narrows by exposure/1000 factor (adaptation).
public static class ExposureTrackSystem
{
    public static void Process(SimState state)
    {
        var playerNodeId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(playerNodeId)) return;
        if (!state.Nodes.TryGetValue(playerNodeId, out var node)) return;

        int phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
        if (phase < 2) return; // STRUCTURAL: Phase 2+ only

        // Higher phases accumulate faster: Phase 2 = +1, Phase 3 = +2, Phase 4 = +3
        int increment = phase - 1; // STRUCTURAL: phase-based scaling
        state.DeepExposure += increment;
    }

    /// <summary>
    /// Returns the instrument disagreement narrowing factor (0-1000 bps range).
    /// Higher exposure = less disagreement. Capped at DisagreementNarrowPerKExposure.
    /// </summary>
    public static int GetDisagreementNarrowBps(SimState state)
    {
        if (state.DeepExposure <= 0) return 0; // STRUCTURAL: no exposure = no narrowing
        // exposure / 1000 * DisagreementNarrowPerKExposure (integer math)
        int narrowing = (state.DeepExposure * DeepDreadTweaksV0.DisagreementNarrowPerKExposure) / 1000; // STRUCTURAL: per-K scaling
        return narrowing;
    }

    /// <summary>
    /// Returns true if the player has reached the adaptation threshold (deep exposure mastery).
    /// </summary>
    public static bool IsAdapted(SimState state)
    {
        return state.DeepExposure >= DeepDreadTweaksV0.ExposureAdaptedThreshold;
    }
}
