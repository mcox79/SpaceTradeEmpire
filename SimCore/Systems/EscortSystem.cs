using System.Linq;
using SimCore.Programs;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S5.ESCORT_PROG.MODEL.001: Escort and Patrol program processing.
// EscortV0: fleet advances toward a destination node (MarketId) from an origin (SourceMarketId).
//   Progress is tracked via ExpeditionTicksRemaining. On leg completion, LastRunTick is updated
//   and the counter resets (simplified model — no entity fleet movement yet).
// PatrolV0: fleet ping-pongs between node A (MarketId) and node B (SourceMarketId).
//   Each leg completion updates LastRunTick; the patrol continues indefinitely.
public static class EscortSystem
{
    /// <summary>
    /// Process all Running EscortV0 and PatrolV0 programs each tick.
    /// Called once per tick from SimKernel.Step().
    /// </summary>
    public static void Process(SimState state)
    {
        if (state?.Programs?.Instances is null) return;

        // Sorted by Id (Ordinal) for determinism.
        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, System.StringComparer.Ordinal))
        {
            var prog = kv.Value;

            if (prog.Status != ProgramStatus.Running) continue;

            if (prog.Kind == ProgramKind.EscortV0)
                ProcessEscort(state, prog);
            else if (prog.Kind == ProgramKind.PatrolV0)
                ProcessPatrol(state, prog);
        }
    }

    private static void ProcessEscort(SimState state, ProgramInstance prog)
    {
        prog.ExpeditionTicksRemaining++;

        if (prog.ExpeditionTicksRemaining >= EscortTweaksV0.PatrolCycleBaseTicks)
        {
            // Escort leg complete — record completion and reset counter.
            prog.LastRunTick = state.Tick;
            prog.ExpeditionTicksRemaining = 0;
        }
    }

    private static void ProcessPatrol(SimState state, ProgramInstance prog)
    {
        prog.ExpeditionTicksRemaining++;

        if (prog.ExpeditionTicksRemaining >= EscortTweaksV0.PatrolCycleBaseTicks)
        {
            // Patrol leg complete — record and reset for continuous cycling.
            prog.LastRunTick = state.Tick;
            prog.ExpeditionTicksRemaining = 0;
        }
    }
}
