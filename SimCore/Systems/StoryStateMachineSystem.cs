using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// GATE.S8.STORY_STATE.TRIGGERS.001: Evaluates 5 revelation trigger conditions per tick.
public static class StoryStateMachineSystem
{
    public static void Process(SimState state)
    {
        var ss = state.StoryState;
        if (ss == null) return;

        // Check each revelation trigger (only fire once per revelation).
        if (!ss.HasRevelation(RevelationFlags.R1_Module))
            TryFireR1(state, ss);

        if (!ss.HasRevelation(RevelationFlags.R2_Concord))
            TryFireR2(state, ss);

        if (!ss.HasRevelation(RevelationFlags.R3_Pentagon))
            TryFireR3(state, ss);

        if (!ss.HasRevelation(RevelationFlags.R4_Communion))
            TryFireR4(state, ss);

        if (!ss.HasRevelation(RevelationFlags.R5_Instability))
            TryFireR5(state, ss);

        // Update act based on revelation count.
        UpdateAct(ss);
    }

    // R1: Module Origin — fracture exposure >= threshold AND lattice visits >= minimum.
    private static void TryFireR1(SimState state, StoryState ss)
    {
        if (ss.FractureExposureCount >= StoryStateTweaksV0.R1FractureExposureThreshold
            && ss.LatticeVisitCount >= StoryStateTweaksV0.R1LatticeVisitMinimum)
        {
            ss.RevealedFlags |= RevelationFlags.R1_Module;
            ss.R1Tick = state.Tick;
            // GATE.S8.STORY.FO_REVELATION.001: FO reacts to module origin revelation.
            FirstOfficerSystem.TryFireTrigger(state, "REVELATION_MODULE_ORIGIN");
        }
    }

    // R2: Concord Suppression — Concord reputation >= Allied threshold.
    private static void TryFireR2(SimState state, StoryState ss)
    {
        if (state.FactionReputation.TryGetValue("concord", out int rep)
            && rep >= StoryStateTweaksV0.R2ConcordRepThreshold)
        {
            ss.RevealedFlags |= RevelationFlags.R2_Concord;
            ss.R2Tick = state.Tick;
            // GATE.S8.STORY.FO_REVELATION.001: FO reacts to Concord suppression revelation.
            FirstOfficerSystem.TryFireTrigger(state, "REVELATION_CONCORD_SUPPRESSION");
        }
    }

    // R3: Pentagon Break — all 5 faction types traded with.
    private static void TryFireR3(SimState state, StoryState ss)
    {
        if (ss.AllPentagonFactionsTraded)
        {
            ss.RevealedFlags |= RevelationFlags.R3_Pentagon;
            ss.R3Tick = state.Tick;
            // GATE.S8.STORY.FO_REVELATION.001: FO reacts to pentagon break (existing trigger).
            FirstOfficerSystem.TryFireTrigger(state, "PENTAGON_BREAK");
        }
    }

    // R4: Communion Truth — high fracture exposure + has read a Communion data log.
    private static void TryFireR4(SimState state, StoryState ss)
    {
        if (ss.FractureExposureCount >= StoryStateTweaksV0.R4FractureExposureThreshold
            && ss.HasReadCommunionLog)
        {
            ss.RevealedFlags |= RevelationFlags.R4_Communion;
            ss.R4Tick = state.Tick;
            // GATE.S8.STORY.FO_REVELATION.001: FO reacts to Communion truth revelation.
            FirstOfficerSystem.TryFireTrigger(state, "REVELATION_COMMUNION_TRUTH");
        }
    }

    // R5: Living Geometry — endgame tick reached + enough fragments collected.
    private static void TryFireR5(SimState state, StoryState ss)
    {
        if (state.Tick >= StoryStateTweaksV0.R5MinimumTick
            && ss.CollectedFragmentCount >= StoryStateTweaksV0.R5MinimumFragments)
        {
            ss.RevealedFlags |= RevelationFlags.R5_Instability;
            ss.R5Tick = state.Tick;
            // GATE.S8.STORY.FO_REVELATION.001: FO reacts to living geometry revelation.
            FirstOfficerSystem.TryFireTrigger(state, "REVELATION_LIVING_GEOMETRY");
        }
    }

    private static void UpdateAct(StoryState ss)
    {
        int count = ss.RevelationCount;
        if (count >= StoryStateTweaksV0.Act3Threshold)
            ss.CurrentAct = StoryAct.Act3_Revealed;
        else if (count >= StoryStateTweaksV0.Act2Threshold)
            ss.CurrentAct = StoryAct.Act2_Questioning;
        else
            ss.CurrentAct = StoryAct.Act1_Innocent;
    }
}
