using System;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.REPUTATION.ACCESS_TIERS.001: Reputation tier enum.
public enum RepTier { Allied, Friendly, Neutral, Hostile, Enemy }

// GATE.S7.TERRITORY.REGIME_MODEL.001: Territory regime enum.
// Computed from TradePolicy + RepTier.
public enum TerritoryRegime { Open, Guarded, Restricted, Hostile }

// GATE.S7.FACTION.REPUTATION_SYS.001: Player faction reputation system.
// Standing per faction clamped to [-100, 100]. Modified by trade and combat.
public static class ReputationSystem
{
    public static void Process(SimState state)
    {
        // Reputation changes are applied by commands/events (trade, combat),
        // not per-tick. This method is reserved for decay or drift mechanics later.
    }

    /// <summary>
    /// Get player standing with a faction. Returns 0 if no record exists.
    /// </summary>
    public static int GetReputation(SimState state, string factionId)
    {
        if (state.FactionReputation.TryGetValue(factionId, out var rep))
            return rep;
        return FactionTweaksV0.ReputationDefault;
    }

    /// <summary>
    /// Adjust player reputation with a faction by delta, clamped to [-100, 100].
    /// </summary>
    public static void AdjustReputation(SimState state, string factionId, int delta)
    {
        if (string.IsNullOrEmpty(factionId)) return;

        var current = GetReputation(state, factionId);
        var next = Math.Clamp(current + delta, FactionTweaksV0.ReputationMin, FactionTweaksV0.ReputationMax);
        state.FactionReputation[factionId] = next;
    }

    /// <summary>
    /// Apply trade reputation gain: player traded at a station controlled by factionId.
    /// </summary>
    public static void OnTradeAtFactionStation(SimState state, string factionId)
    {
        AdjustReputation(state, factionId, FactionTweaksV0.TradeRepGain);
    }

    /// <summary>
    /// Apply combat reputation loss: player attacked a ship belonging to factionId.
    /// </summary>
    public static void OnAttackFactionShip(SimState state, string factionId)
    {
        AdjustReputation(state, factionId, FactionTweaksV0.AttackRepLoss);
    }

    // GATE.S7.REPUTATION.ACCESS_TIERS.001: Classify reputation into tier.
    public static RepTier GetRepTier(int reputation)
    {
        if (reputation >= FactionTweaksV0.AlliedThreshold) return RepTier.Allied;
        if (reputation >= FactionTweaksV0.FriendlyThreshold) return RepTier.Friendly;
        if (reputation >= FactionTweaksV0.NeutralThreshold) return RepTier.Neutral;
        if (reputation >= FactionTweaksV0.HostileThreshold) return RepTier.Hostile;
        return RepTier.Enemy;
    }

    public static RepTier GetRepTier(SimState state, string factionId)
        => GetRepTier(GetReputation(state, factionId));

    // GATE.S7.REPUTATION.ACCESS_TIERS.001: Access checks by tier.
    public static bool CanDock(SimState state, string factionId)
    {
        int rep = GetReputation(state, factionId);
        return rep >= FactionTweaksV0.DockBlockedBelowTier;
    }

    public static bool CanTrade(SimState state, string factionId)
    {
        int rep = GetReputation(state, factionId);
        return rep >= FactionTweaksV0.TradeBlockedBelowTier;
    }

    public static bool CanBuyTech(SimState state, string factionId)
    {
        int rep = GetReputation(state, factionId);
        return rep >= FactionTweaksV0.TechRequiresMinTier;
    }

    // GATE.S7.TERRITORY.REGIME_MODEL.001: Compute territory regime from TradePolicy + RepTier.
    // Matrix: Open+Friendly+=Open, Open+Neutral=Guarded, Guarded+Friendly+=Guarded,
    //         Guarded+Neutral=Restricted, any+Hostile=Restricted, Closed|Enemy=Hostile.
    public static TerritoryRegime ComputeTerritoryRegime(int tradePolicy, RepTier repTier)
    {
        if (repTier == RepTier.Enemy) return TerritoryRegime.Hostile;
        if (tradePolicy == (int)Schemas.TradePolicy.Closed) return TerritoryRegime.Hostile;
        if (repTier == RepTier.Hostile) return TerritoryRegime.Restricted;

        if (tradePolicy == (int)Schemas.TradePolicy.Open)
        {
            return (repTier == RepTier.Allied || repTier == RepTier.Friendly)
                ? TerritoryRegime.Open
                : TerritoryRegime.Guarded; // Neutral
        }

        // Guarded policy
        return (repTier == RepTier.Allied || repTier == RepTier.Friendly)
            ? TerritoryRegime.Guarded
            : TerritoryRegime.Restricted; // Neutral
    }

    // Convenience: compute regime for a node from state data.
    public static TerritoryRegime ComputeTerritoryRegime(SimState state, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return TerritoryRegime.Open;
        if (!state.NodeFactionId.TryGetValue(nodeId, out var factionId) || string.IsNullOrEmpty(factionId))
            return TerritoryRegime.Open;

        var repTier = GetRepTier(state, factionId);
        int policy = state.FactionTradePolicy.TryGetValue(factionId, out var p) ? p : (int)Schemas.TradePolicy.Open;
        return ComputeTerritoryRegime(policy, repTier);
    }
}
