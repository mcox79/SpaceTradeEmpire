using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.REPUTATION.ACCESS_TIERS.001: Reputation tier enum.
public enum RepTier { Allied, Friendly, Neutral, Hostile, Enemy }

// GATE.S7.TERRITORY.REGIME_MODEL.001: Territory regime enum.
// Computed from TradePolicy + RepTier.
public enum TerritoryRegime { Open, Guarded, Restricted, Hostile }

// GATE.S7.FACTION.REPUTATION_SYS.001: Player faction reputation system.
// Standing per faction clamped to [-100, 100]. Modified by trade and combat.
// GATE.S7.REPUTATION.TRADE_DRIFT.001: Natural decay toward neutral over time.
public static class ReputationSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedFactionIds = new();
        public readonly List<string> SortedNodeIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        // GATE.S7.REPUTATION.TRADE_DRIFT.001: Natural decay toward 0.
        // Every RepDecayIntervalTicks, all non-zero rep values drift 1 point toward 0.
        if (state.Tick > 0 && state.Tick % FactionTweaksV0.RepDecayIntervalTicks == 0 // STRUCTURAL: tick guard + modulo
            && state.FactionReputation.Count > 0) // STRUCTURAL: empty guard
        {
            // Snapshot keys to avoid modifying during iteration.
            var scratch = s_scratch.GetOrCreateValue(state);
            var factionIds = scratch.SortedFactionIds;
            factionIds.Clear();
            foreach (var k in state.FactionReputation.Keys) factionIds.Add(k);
            factionIds.Sort(StringComparer.Ordinal);
            foreach (var factionId in factionIds)
            {
                int rep = state.FactionReputation[factionId];
                if (rep > 0) // STRUCTURAL: direction check
                    state.FactionReputation[factionId] = rep - FactionTweaksV0.RepDecayAmount;
                else if (rep < 0) // STRUCTURAL: direction check
                    state.FactionReputation[factionId] = rep + FactionTweaksV0.RepDecayAmount;
            }
        }

        // GATE.S7.TERRITORY.HYSTERESIS.001: Update committed regimes with hysteresis.
        ProcessRegimeHysteresis(state);
    }

    // GATE.S7.TERRITORY.HYSTERESIS.001: Asymmetric hysteresis for territory regime transitions.
    // Worsening (toward Hostile) commits instantly. Improvement (toward Open) requires
    // sustained stability for RegimeHysteresisMinTicks before committing.
    public static void ProcessRegimeHysteresis(SimState state)
    {
        if (state.NodeFactionId.Count == 0) return; // STRUCTURAL: empty guard

        var scratch = s_scratch.GetOrCreateValue(state);
        var nodeIds = scratch.SortedNodeIds;
        nodeIds.Clear();
        foreach (var k in state.NodeFactionId.Keys) nodeIds.Add(k);
        nodeIds.Sort(StringComparer.Ordinal);
        foreach (var nodeId in nodeIds)
        {
            var rawRegime = ComputeTerritoryRegime(state, nodeId);
            int rawVal = (int)rawRegime;

            if (!state.NodeRegimeCommitted.TryGetValue(nodeId, out var committedVal))
            {
                // First time: commit immediately (no hysteresis for initial assignment).
                state.NodeRegimeCommitted[nodeId] = rawVal;
                continue;
            }

            if (rawVal == committedVal)
            {
                // Stable at committed: clear any pending proposal.
                state.NodeRegimeProposed.Remove(nodeId);
                state.NodeRegimeProposedSinceTick.Remove(nodeId);
                continue;
            }

            if (rawVal > committedVal)
            {
                // Worsening (higher enum = more hostile): commit instantly.
                state.NodeRegimeCommitted[nodeId] = rawVal;
                state.NodeRegimeProposed.Remove(nodeId);
                state.NodeRegimeProposedSinceTick.Remove(nodeId);
            }
            else
            {
                // Improving (lower enum = more open): apply hysteresis.
                if (state.NodeRegimeProposed.TryGetValue(nodeId, out var proposedVal) && proposedVal == rawVal)
                {
                    // Same proposal persists — check duration.
                    int sinceTick = state.NodeRegimeProposedSinceTick.TryGetValue(nodeId, out var st) ? st : state.Tick;
                    if (state.Tick - sinceTick >= FactionTweaksV0.RegimeHysteresisMinTicks)
                    {
                        // Sustained long enough: commit improvement.
                        state.NodeRegimeCommitted[nodeId] = rawVal;
                        state.NodeRegimeProposed.Remove(nodeId);
                        state.NodeRegimeProposedSinceTick.Remove(nodeId);
                    }
                }
                else
                {
                    // New or changed proposal direction: start fresh.
                    state.NodeRegimeProposed[nodeId] = rawVal;
                    state.NodeRegimeProposedSinceTick[nodeId] = state.Tick;
                }
            }
        }
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
    /// GATE.S7.FACTION_COMMISSION.INFAMY.001: Also clamped by infamy cap.
    /// </summary>
    public static void AdjustReputation(SimState state, string factionId, int delta)
    {
        if (string.IsNullOrEmpty(factionId)) return;

        // GATE.S8.HAVEN.ACCOMMODATION_FX.001: Harmony thread bonus amplifies positive rep gains.
        if (delta > 0)
        {
            int harmonyPct = HavenEndgameSystem.GetAccommodationBonusPct(state, Entities.AccommodationThreadIds.Harmony);
            if (harmonyPct > 0)
                delta = delta + delta * harmonyPct / 100;
        }

        var current = GetReputation(state, factionId);
        int maxRep = GetMaxRepForInfamy(state, factionId);
        var next = Math.Clamp(current + delta, FactionTweaksV0.ReputationMin, maxRep);
        state.FactionReputation[factionId] = next;
    }

    // GATE.S7.FACTION_COMMISSION.INFAMY.001: Get max achievable reputation for a faction based on infamy.
    public static int GetMaxRepForInfamy(SimState state, string factionId)
    {
        if (state is null || string.IsNullOrEmpty(factionId)) return FactionTweaksV0.ReputationMax;
        if (!state.InfamyByFaction.TryGetValue(factionId, out int infamy) || infamy <= 0) // STRUCTURAL: no infamy
            return FactionTweaksV0.ReputationMax;

        if (infamy >= CommissionTweaksV0.InfamyCapNeutral)
            return FactionTweaksV0.NeutralThreshold; // capped at Neutral ceiling
        if (infamy >= CommissionTweaksV0.InfamyCapFriendly)
            return FactionTweaksV0.FriendlyThreshold; // capped at Friendly ceiling

        return FactionTweaksV0.ReputationMax;
    }

    // GATE.S7.FACTION_COMMISSION.INFAMY.001: Accumulate infamy with a faction.
    public static void AccumulateInfamy(SimState state, string factionId, int amount)
    {
        if (state is null || string.IsNullOrEmpty(factionId) || amount <= 0) return; // STRUCTURAL: guard
        state.InfamyByFaction.TryGetValue(factionId, out int current);
        state.InfamyByFaction[factionId] = current + amount;
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
    /// GATE.S7.FACTION_COMMISSION.INFAMY.001: Also accumulates infamy.
    /// </summary>
    public static void OnAttackFactionShip(SimState state, string factionId)
    {
        AdjustReputation(state, factionId, FactionTweaksV0.AttackRepLoss);
        AccumulateInfamy(state, factionId, CommissionTweaksV0.InfamyPerAttack);
    }

    // GATE.S7.REPUTATION.WAR_PROFITEER.001: War profiteering rep effects.
    // Selling war-critical goods at a belligerent faction market gives +rep with buyer, -rep with enemy.
    public static void OnWarProfiteerTrade(SimState state, string buyerFactionId, string goodId)
    {
        if (state is null || string.IsNullOrEmpty(buyerFactionId) || string.IsNullOrEmpty(goodId)) return;

        // Check if good is war-critical.
        bool isWarCritical = false;
        foreach (var wg in FactionTweaksV0.WarCriticalGoods)
        {
            if (StringComparer.Ordinal.Equals(wg, goodId)) { isWarCritical = true; break; }
        }
        if (!isWarCritical) return;

        // Check if buyer is in an active warfront.
        if (state.Warfronts is null) return;
        foreach (var wf in state.Warfronts.Values)
        {
            if (wf.Intensity <= WarfrontIntensity.Peace) continue;

            string? enemy = null;
            if (StringComparer.Ordinal.Equals(wf.CombatantA, buyerFactionId))
                enemy = wf.CombatantB;
            else if (StringComparer.Ordinal.Equals(wf.CombatantB, buyerFactionId))
                enemy = wf.CombatantA;

            if (enemy is not null)
            {
                AdjustReputation(state, buyerFactionId, FactionTweaksV0.WarProfiteerBuyerGain);
                AdjustReputation(state, enemy, FactionTweaksV0.WarProfiteerEnemyLoss);
                // GATE.S7.FACTION_COMMISSION.INFAMY.001: War profiteering adds infamy with enemy.
                AccumulateInfamy(state, enemy, CommissionTweaksV0.InfamyPerWarProfiteer);
                return; // Only apply once per trade, even if multiple warfronts.
            }
        }
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

    // GATE.S7.TERRITORY.HYSTERESIS.001: Returns the committed (hysteresis-buffered) regime for a node.
    // Falls back to ComputeTerritoryRegime if no committed value exists yet.
    public static TerritoryRegime GetEffectiveRegime(SimState state, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return TerritoryRegime.Open;
        if (state.NodeRegimeCommitted.TryGetValue(nodeId, out var committed))
            return (TerritoryRegime)committed;
        return ComputeTerritoryRegime(state, nodeId);
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
    // GATE.S7.TERRITORY.REGIME_TRANSITION.001: Incorporates warfront intensity.
    // Intensity >= 3 → minimum Restricted. Intensity >= 4 → Hostile (trade blocked for non-allied).
    // Hysteresis: regime only improves (toward Open) at intensity <= 1.
    public static TerritoryRegime ComputeTerritoryRegime(SimState state, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return TerritoryRegime.Open;
        if (!state.NodeFactionId.TryGetValue(nodeId, out var factionId) || string.IsNullOrEmpty(factionId))
            return TerritoryRegime.Open;

        var repTier = GetRepTier(state, factionId);
        int policy = state.FactionTradePolicy.TryGetValue(factionId, out var p) ? p : (int)Schemas.TradePolicy.Open;
        var baseRegime = ComputeTerritoryRegime(policy, repTier);

        // GATE.S7.TERRITORY.REGIME_TRANSITION.001: War-driven regime escalation.
        // Find node's market, check warfront intensity.
        if (state.Nodes.TryGetValue(nodeId, out var node) && !string.IsNullOrEmpty(node.MarketId))
        {
            int warIntensity = MarketSystem.GetNodeWarfrontIntensity(state, node.MarketId);
            if (warIntensity >= WarfrontTweaksV0.TotalWarIntensity)
            {
                // Total war: Hostile for non-allied.
                if (repTier != RepTier.Allied)
                    return TerritoryRegime.Hostile;
            }
            else if (warIntensity >= WarfrontTweaksV0.OpenWarIntensity)
            {
                // Open war: at least Restricted.
                if (baseRegime < TerritoryRegime.Restricted)
                    return TerritoryRegime.Restricted;
            }
        }

        return baseRegime;
    }
}
