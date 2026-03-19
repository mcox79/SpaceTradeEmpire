using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S5.SEC_LANES.SYSTEM.001: Security lane system — patrol presence + piracy heat to security level.
public static class SecurityLaneSystem
{
    private sealed class Scratch
    {
        public readonly List<string> EdgeIds = new();
        public readonly List<string> FleetIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    /// <summary>
    /// Process security levels on all edges each tick.
    /// Patrol fleets on adjacent nodes raise security. Economic heat lowers it.
    /// Natural drift pulls toward default.
    /// </summary>
    public static void ProcessSecurityLanes(SimState state)
    {
        if (state == null) return;

        // GATE.S7.ENFORCEMENT.HEAT_ACCUM.001: Decay heat before security calculation.
        DecayHeat(state);

        var scratch = s_scratch.GetOrCreateValue(state);
        var edgeIds = scratch.EdgeIds;
        edgeIds.Clear();
        foreach (var k in state.Edges.Keys) edgeIds.Add(k);
        edgeIds.Sort(StringComparer.Ordinal);

        foreach (var edgeId in edgeIds)
        {
            var edge = state.Edges[edgeId];

            // Count patrol fleets on adjacent nodes
            int patrolCount = CountPatrolFleetsAtNode(state, edge.FromNodeId)
                            + CountPatrolFleetsAtNode(state, edge.ToNodeId);

            // Patrol boost
            int patrolBoost = patrolCount * SecurityTweaksV0.PatrolBoostBps;

            // Heat penalty (economic heat on the edge drives insecurity)
            int heatPenalty = (int)(edge.Heat * SecurityTweaksV0.HeatPenaltyBps);

            // Natural drift toward default
            int drift = CalculateDrift(edge.SecurityLevelBps);

            // Apply delta
            int newLevel = edge.SecurityLevelBps + patrolBoost - heatPenalty + drift;
            edge.SecurityLevelBps = Math.Clamp(newLevel,
                SecurityTweaksV0.MinSecurityBps,
                SecurityTweaksV0.MaxSecurityBps);
        }

        // GATE.S7.ENFORCEMENT.CONFISCATION.001: Check confiscation triggers.
        ProcessConfiscation(state);
    }

    private static int CountPatrolFleetsAtNode(SimState state, string nodeId)
    {
        int count = 0;
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.Role == FleetRole.Patrol && fleet.CurrentNodeId == nodeId)
                count++;
        }
        return count;
    }

    private static int CalculateDrift(int currentBps)
    {
        if (currentBps < SecurityTweaksV0.DefaultSecurityBps)
            return SecurityTweaksV0.DriftToDefaultBps;
        if (currentBps > SecurityTweaksV0.DefaultSecurityBps)
            return -SecurityTweaksV0.DriftToDefaultBps;
        return 0;
    }

    /// <summary>
    /// Classify the security band for a given BPS value.
    /// </summary>
    public static string GetSecurityBand(int bps)
    {
        if (bps < SecurityTweaksV0.HostileBps) return "hostile";
        if (bps < SecurityTweaksV0.DangerousBps) return "dangerous";
        if (bps < SecurityTweaksV0.ModerateBps) return "moderate";
        if (bps >= SecurityTweaksV0.SafeBps) return "safe";
        return "moderate";
    }

    // GATE.S7.ENFORCEMENT.HEAT_ACCUM.001: Pattern-based heat accumulation.
    // Called when a fleet traverses an edge with cargo. Adds heat based on:
    // 1. Base traffic heat (cargoVolume * 0.01f)
    // 2. High-value trades (cargo value > threshold adds bonus)
    // 3. Route repetition (3+ traversals in window adds bonus)
    // 4. Hostile counterparty (destination controlled by hostile faction)
    public static void RegisterPatternedTraffic(SimState state, string edgeId, int cargoVolume, int cargoValueCredits, string? destinationNodeId)
    {
        if (state is null || !state.Edges.TryGetValue(edgeId, out var edge)) return;

        // Base traffic heat.
        float heat = cargoVolume * 0.01f;

        // High-value trade bonus.
        if (cargoValueCredits > SecurityTweaksV0.HighValueThresholdCredits)
        {
            heat += (cargoValueCredits / 100) * SecurityTweaksV0.HighValueHeatPerHundredCredits;
        }

        // Route repetition.
        edge.TraversalCount++;
        if (edge.TraversalCount >= SecurityTweaksV0.RepetitionThreshold)
        {
            heat += SecurityTweaksV0.RepetitionBonusHeat;
        }

        // Hostile counterparty.
        if (!string.IsNullOrEmpty(destinationNodeId)
            && state.NodeFactionId.TryGetValue(destinationNodeId, out var factionId)
            && !string.IsNullOrEmpty(factionId))
        {
            int rep = ReputationSystem.GetReputation(state, factionId);
            if (rep < FactionTweaksV0.TradeBlockedRepThreshold)
            {
                heat += SecurityTweaksV0.HostileCounterpartyHeat;
            }
        }

        edge.Heat += heat;
    }

    // GATE.S7.ENFORCEMENT.HEAT_ACCUM.001: Decay heat and reset traversal counters.
    private static void DecayHeat(SimState state)
    {
        foreach (var edge in state.Edges.Values)
        {
            if (edge.Heat > 0)
            {
                edge.Heat -= SecurityTweaksV0.HeatDecayPerTick;
                if (edge.Heat < 0) edge.Heat = 0;
            }

            if (state.Tick > 0 && state.Tick % SecurityTweaksV0.TraversalWindowTicks == 0)
            {
                edge.TraversalCount = 0;
            }
        }
    }

    // GATE.S7.ENFORCEMENT.CONFISCATION.001: Confiscation at high heat.
    // Player fleets traveling on hot edges risk confiscation of their highest-value cargo.
    private static void ProcessConfiscation(SimState state)
    {
        if (state.Fleets is null || state.Fleets.Count == 0) return;

        var scratch2 = s_scratch.GetOrCreateValue(state);
        var fleetIds = scratch2.FleetIds;
        fleetIds.Clear();
        foreach (var k in state.Fleets.Keys) fleetIds.Add(k);
        fleetIds.Sort(StringComparer.Ordinal);

        foreach (var fleetId in fleetIds)
        {
            var fleet = state.Fleets[fleetId];
            if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (fleet.State != Entities.FleetState.Traveling) continue;
            if (string.IsNullOrEmpty(fleet.CurrentEdgeId)) continue;
            if (fleet.Cargo is null || fleet.Cargo.Count == 0) continue;

            // Cooldown check.
            if (fleet.LastConfiscationTick >= 0
                && state.Tick - fleet.LastConfiscationTick < SecurityTweaksV0.ConfiscationCooldownTicks)
                continue;

            // Heat check.
            if (!state.Edges.TryGetValue(fleet.CurrentEdgeId, out var edge)) continue;
            if (edge.Heat < SecurityTweaksV0.ConfiscationHeatThreshold) continue;

            // Find highest-quantity cargo item (deterministic: sort by key for ties).
            string bestGoodId = "";
            int bestQty = 0;
            foreach (var kv in fleet.Cargo)
            {
                if (kv.Value > bestQty || (kv.Value == bestQty && StringComparer.Ordinal.Compare(kv.Key, bestGoodId) < 0))
                {
                    bestGoodId = kv.Key;
                    bestQty = kv.Value;
                }
            }
            if (string.IsNullOrEmpty(bestGoodId) || bestQty <= 0) continue;

            // Confiscate: capped by MaxUnits.
            int seized = Math.Min(bestQty, SecurityTweaksV0.ConfiscationMaxUnits);
            fleet.Cargo[bestGoodId] -= seized;
            if (fleet.Cargo[bestGoodId] <= 0) fleet.Cargo.Remove(bestGoodId);

            // Fine: percentage of cargo value estimate (use base price for simplicity).
            int fineCredits = seized * SecurityTweaksV0.ConfiscationFineBps / 100;
            if (fineCredits < 1) fineCredits = 1;

            // Deduct fine from player credits.
            state.PlayerCredits = Math.Max(0, state.PlayerCredits - fineCredits);

            // Update cooldown.
            fleet.LastConfiscationTick = state.Tick;

            // Emit event.
            state.EmitSecurityEvent(new Events.SecurityEvents.Event
            {
                Type = Events.SecurityEvents.SecurityEventType.Confiscation,
                EdgeId = edge.Id,
                FromNodeId = edge.FromNodeId,
                ToNodeId = edge.ToNodeId,
                ConfiscatedGoodId = bestGoodId,
                ConfiscatedUnits = seized,
                FineCredits = fineCredits,
                CauseChain = "v1 confiscation heat=" + edge.Heat.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    + " threshold=" + SecurityTweaksV0.ConfiscationHeatThreshold.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Note = "CONFISCATION_V0"
            });
        }
    }
}
