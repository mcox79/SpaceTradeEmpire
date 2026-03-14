using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path effects per tick.
// GATE.S8.HAVEN.ACCOMMODATION.001: Accommodation thread advancement.
// GATE.S8.HAVEN.COMMUNION_REP.001: Communion Representative spawn/presence.
public static class HavenEndgameSystem
{
    public static void Process(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;

        ProcessEndgamePath(state, haven);
        ProcessAccommodation(state, haven);
        ProcessCommunionRep(state, haven);
    }

    // GATE.S8.HAVEN.ENDGAME_PATHS.001: Path-specific rep drift.
    private static void ProcessEndgamePath(SimState state, HavenStarbase haven)
    {
        if (haven.ChosenEndgamePath == EndgamePath.None) return;
        if (state.Tick % EndgameTweaksV0.PathDriftIntervalTicks != 0) return;

        switch (haven.ChosenEndgamePath)
        {
            case EndgamePath.Reinforce:
                AdjustRep(state, FactionTweaksV0.ConcordId, EndgameTweaksV0.ReinforceRepDriftPerInterval);
                break;
            case EndgamePath.Naturalize:
                AdjustRep(state, FactionTweaksV0.CommunionId, EndgameTweaksV0.NaturalizeRepDriftPerInterval);
                break;
            case EndgamePath.Renegotiate:
                // Costs trust with all factions.
                foreach (var fid in FactionTweaksV0.AllFactionIds)
                    AdjustRep(state, fid, EndgameTweaksV0.RenegotiateRepDriftPerInterval);
                break;
        }
    }

    // GATE.S8.HAVEN.ACCOMMODATION.001: Advance threads based on player state.
    private static void ProcessAccommodation(SimState state, HavenStarbase haven)
    {
        if (haven.Tier < HavenTier.Operational) return; // Tier 3+ required

        // Initialize threads if not present.
        foreach (var threadId in AccommodationThreadIds.All)
        {
            if (!haven.AccommodationProgress.ContainsKey(threadId))
                haven.AccommodationProgress[threadId] = 0;
        }

        // Discovery thread: advances when player has collected fragments.
        if (state.StoryState?.CollectedFragmentCount > 0)
            AdvanceThread(haven, AccommodationThreadIds.Discovery, state.StoryState.CollectedFragmentCount);

        // Commerce thread: advances based on player stats (goods traded).
        if (state.PlayerStats?.GoodsTraded > 0)
            AdvanceThread(haven, AccommodationThreadIds.Commerce, state.PlayerStats.GoodsTraded / 10);

        // Conflict thread: advances based on combat logs.
        if (state.CombatLogs?.Count > 0)
            AdvanceThread(haven, AccommodationThreadIds.Conflict, state.CombatLogs.Count);

        // Harmony thread: advances based on faction reputation average.
        int totalRep = 0;
        int factionCount = 0;
        foreach (var fid in FactionTweaksV0.AllFactionIds)
        {
            if (state.FactionReputation.TryGetValue(fid, out int rep))
            {
                totalRep += rep;
                factionCount++;
            }
        }
        if (factionCount > 0)
            AdvanceThread(haven, AccommodationThreadIds.Harmony, totalRep / factionCount);
    }

    private static void AdvanceThread(HavenStarbase haven, string threadId, int level)
    {
        if (level <= 0) return;
        int current = haven.AccommodationProgress.GetValueOrDefault(threadId);
        int target = System.Math.Min(level * EndgameTweaksV0.AccommodationProgressPerAction, EndgameTweaksV0.AccommodationMaxProgress);
        if (target > current)
            haven.AccommodationProgress[threadId] = target;
    }

    // GATE.S8.HAVEN.COMMUNION_REP.001: Communion Representative spawn check.
    private static void ProcessCommunionRep(SimState state, HavenStarbase haven)
    {
        var rep = haven.CommunionRep;
        if (rep == null) return;

        // Spawn conditions: Tier 3+, Communion faction rep >= Neutral.
        bool qualifies = haven.Tier >= EndgameTweaksV0.CommunionRepMinTier
            && state.FactionReputation.TryGetValue(FactionTweaksV0.CommunionId, out int factionRep)
            && factionRep >= EndgameTweaksV0.CommunionRepMinFactionRep;

        rep.Present = qualifies;
    }

    // Choose an endgame path (command-driven, called from ChooseEndgamePathCommand).
    public static bool ChooseEndgamePath(SimState state, EndgamePath path)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return false;
        if (haven.Tier < EndgameTweaksV0.MinTierForChoice) return false;
        if (haven.ChosenEndgamePath != EndgamePath.None) return false; // Already chosen
        if (path == EndgamePath.None) return false;

        haven.ChosenEndgamePath = path;
        haven.EndgamePathChosenTick = state.Tick;
        return true;
    }

    // GATE.S8.HAVEN.ACCOMMODATION_FX.001: Get accommodation bonus percentage for a thread.
    // Returns 0 if no bonus, or the appropriate tier pct value.
    public static int GetAccommodationBonusPct(SimState state, string threadId)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return 0;
        if (haven.AccommodationProgress == null) return 0;
        if (!haven.AccommodationProgress.TryGetValue(threadId, out int progress)) return 0;

        // Determine tier.
        int tier = 0;
        if (progress >= HavenTweaksV0.AccBonusTier3Threshold) tier = 3;
        else if (progress >= HavenTweaksV0.AccBonusTier2Threshold) tier = 2;
        else if (progress >= HavenTweaksV0.AccBonusTier1Threshold) tier = 1;

        if (tier == 0) return 0;

        return threadId switch
        {
            AccommodationThreadIds.Discovery => tier switch
            {
                1 => HavenTweaksV0.AccDiscoveryScanTier1Pct,
                2 => HavenTweaksV0.AccDiscoveryScanTier2Pct,
                _ => HavenTweaksV0.AccDiscoveryScanTier3Pct,
            },
            AccommodationThreadIds.Commerce => tier switch
            {
                1 => HavenTweaksV0.AccCommercePriceTier1Pct,
                2 => HavenTweaksV0.AccCommercePriceTier2Pct,
                _ => HavenTweaksV0.AccCommercePriceTier3Pct,
            },
            AccommodationThreadIds.Conflict => tier switch
            {
                1 => HavenTweaksV0.AccConflictDamageTier1Pct,
                2 => HavenTweaksV0.AccConflictDamageTier2Pct,
                _ => HavenTweaksV0.AccConflictDamageTier3Pct,
            },
            AccommodationThreadIds.Harmony => tier switch
            {
                1 => HavenTweaksV0.AccHarmonyRepTier1Pct,
                2 => HavenTweaksV0.AccHarmonyRepTier2Pct,
                _ => HavenTweaksV0.AccHarmonyRepTier3Pct,
            },
            _ => 0,
        };
    }

    private static void AdjustRep(SimState state, string factionId, int delta)
    {
        if (!state.FactionReputation.ContainsKey(factionId))
            state.FactionReputation[factionId] = 0;
        int newRep = state.FactionReputation[factionId] + delta;
        state.FactionReputation[factionId] = System.Math.Clamp(newRep, FactionTweaksV0.ReputationMin, FactionTweaksV0.ReputationMax);
    }
}
