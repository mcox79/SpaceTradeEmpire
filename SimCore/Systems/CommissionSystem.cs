using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.FACTION_COMMISSION.ENTITY.001: Commission passive effects — rep drift + stipend.
// Per CommissionCycleTicks: +1 rep with employer, -1 rep with each rival, +stipend credits.
public static class CommissionSystem
{
    public static void Process(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard
        if (state.ActiveCommission is null) return;

        if (CommissionTweaksV0.CommissionCycleTicks <= 0) return; // STRUCTURAL: disabled guard
        if (state.Tick % CommissionTweaksV0.CommissionCycleTicks != 0) return; // STRUCTURAL: cycle check

        var comm = state.ActiveCommission;
        if (string.IsNullOrEmpty(comm.FactionId)) return;

        // +rep with employer
        ReputationSystem.AdjustReputation(state, comm.FactionId, CommissionTweaksV0.EmployerRepGainPerCycle);

        // -rep with rival factions (all other known factions)
        var factionIds = new List<string>(state.FactionReputation.Keys);
        factionIds.Sort(StringComparer.Ordinal);
        foreach (var fid in factionIds)
        {
            if (string.Equals(fid, comm.FactionId, StringComparison.Ordinal)) continue;
            ReputationSystem.AdjustReputation(state, fid, -CommissionTweaksV0.RivalRepLossPerCycle);
        }

        // Stipend payment
        int stipend = comm.StipendCreditsPerCycle > 0
            ? comm.StipendCreditsPerCycle
            : CommissionTweaksV0.DefaultStipendCredits;
        state.PlayerCredits += stipend;
    }
}
