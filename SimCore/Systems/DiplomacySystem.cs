using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.DIPLOMACY.FRAMEWORK.001: Diplomacy system — treaties, bounties, sanctions.
public static class DiplomacySystem
{
    private sealed class Scratch
    {
        public readonly List<string> TempIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    /// <summary>
    /// Per-tick processing: expire acts, generate faction proposals, check bounties.
    /// Called from SimKernel.Step().
    /// </summary>
    public static void Process(SimState state)
    {
        if (state is null) return;

        ProcessExpiry(state);
        GenerateProposals(state);
    }

    // ── Expiry ──

    private static void ProcessExpiry(SimState state)
    {
        var scratch = s_scratch.GetOrCreateValue(state);
        var toRemove = scratch.TempIds;
        toRemove.Clear();
        foreach (var kv in state.DiplomaticActs)
        {
            var act = kv.Value;
            if (act.ExpiryTick > 0 && state.Tick >= act.ExpiryTick)
            {
                if (act.Status == DiplomaticActStatus.Active || act.Status == DiplomaticActStatus.Pending)
                    toRemove.Add(kv.Key);
            }
        }
        foreach (var id in toRemove)
            state.DiplomaticActs.Remove(id);
    }

    // ── Proposal generation (faction AI) ──
    // GATE.S7.DIPLOMACY.FACTION_AI.001

    private static void GenerateProposals(SimState state)
    {
        if (state.Tick % DiplomacyTweaksV0.FactionProposalIntervalTicks != 0) return;

        foreach (var factionId in FactionTweaksV0.AllFactionIds)
        {
            // Skip if player rep is too low (hostile/enemy won't propose)
            int rep = 0;
            state.FactionReputation.TryGetValue(factionId, out rep);
            if (rep < DiplomacyTweaksV0.ProposalAutoRejectRepMax) continue;

            // Check if faction already has max active treaties
            int activeTreaties = 0;
            foreach (var act in state.DiplomaticActs.Values)
            {
                if (string.Equals(act.FactionId, factionId, StringComparison.Ordinal)
                    && act.ActType == DiplomaticActType.Treaty
                    && act.Status == DiplomaticActStatus.Active)
                    activeTreaties++;
            }

            // Determine proposal type based on faction personality
            int treatyWeight = GetTreatyWeight(factionId);
            ulong hash = Fnv1a64($"diplo_proposal_{factionId}_{state.Tick}");
            int roll = (int)(hash % 100UL);

            if (roll < treatyWeight && activeTreaties < DiplomacyTweaksV0.MaxActiveTreatiesPerFaction)
            {
                // Propose treaty
                CreateTreatyProposal(state, factionId);
            }
            else
            {
                // Generate bounty (if under max)
                int activeBounties = 0;
                foreach (var act in state.DiplomaticActs.Values)
                {
                    if (act.ActType == DiplomaticActType.Bounty && act.Status == DiplomaticActStatus.Active)
                        activeBounties++;
                }
                if (activeBounties < DiplomacyTweaksV0.MaxActiveBounties)
                    CreateBounty(state, factionId);
            }
        }
    }

    private static void CreateTreatyProposal(SimState state, string factionId)
    {
        var actId = $"diplo_{state.NextDiplomaticActSeq}";
        state.NextDiplomaticActSeq++;

        var act = new DiplomaticAct
        {
            Id = actId,
            FactionId = factionId,
            ActType = DiplomaticActType.Proposal,
            Status = DiplomaticActStatus.Pending,
            CreatedTick = state.Tick,
            ExpiryTick = state.Tick + DiplomacyTweaksV0.TreatyCooldownTicks,
            TariffReductionBps = DiplomacyTweaksV0.TreatyTariffReductionBps,
            SafePassage = true,
        };
        state.DiplomaticActs[actId] = act;
    }

    // GATE.S7.DIPLOMACY.BOUNTY.001: Create bounty on a hostile NPC fleet.
    private static void CreateBounty(SimState state, string factionId)
    {
        // Find a hostile NPC fleet (not owned by this faction, not player)
        string targetFleetId = "";
        foreach (var fleet in state.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (string.Equals(fleet.OwnerId, factionId, StringComparison.Ordinal)) continue;
            if (fleet.IsStored || fleet.HullHpMax <= 0) continue;
            targetFleetId = fleet.Id;
            break; // Take first eligible
        }
        if (string.IsNullOrEmpty(targetFleetId)) return;

        var actId = $"diplo_{state.NextDiplomaticActSeq}";
        state.NextDiplomaticActSeq++;

        ulong rewardHash = Fnv1a64($"bounty_reward_{factionId}_{state.Tick}");
        int rewardCredits = DiplomacyTweaksV0.BountyRewardCreditsMin
            + (int)(rewardHash % (ulong)DiplomacyTweaksV0.BountyRewardCreditsRange);

        var act = new DiplomaticAct
        {
            Id = actId,
            FactionId = factionId,
            ActType = DiplomaticActType.Bounty,
            Status = DiplomaticActStatus.Active,
            CreatedTick = state.Tick,
            ExpiryTick = state.Tick + DiplomacyTweaksV0.BountyDurationTicks,
            BountyTargetFleetId = targetFleetId,
            BountyRewardCredits = rewardCredits,
            BountyRewardRep = DiplomacyTweaksV0.BountyRewardRep,
        };
        state.DiplomaticActs[actId] = act;
    }

    // ── Treaty acceptance ──

    /// <summary>
    /// Accept a pending proposal. Converts Proposal to active Treaty.
    /// </summary>
    public static bool AcceptProposal(SimState state, string actId)
    {
        if (state is null || string.IsNullOrEmpty(actId)) return false;
        if (!state.DiplomaticActs.TryGetValue(actId, out var act)) return false;
        if (act.Status != DiplomaticActStatus.Pending) return false;

        act.ActType = DiplomaticActType.Treaty;
        act.Status = DiplomaticActStatus.Active;
        act.ExpiryTick = state.Tick + DiplomacyTweaksV0.TreatyDurationTicks;
        return true;
    }

    /// <summary>
    /// Reject a pending proposal.
    /// </summary>
    public static bool RejectProposal(SimState state, string actId)
    {
        if (state is null || string.IsNullOrEmpty(actId)) return false;
        if (!state.DiplomaticActs.TryGetValue(actId, out var act)) return false;
        if (act.Status != DiplomaticActStatus.Pending) return false;

        act.Status = DiplomaticActStatus.Rejected;
        return true;
    }

    /// <summary>
    /// Player-initiated treaty proposal. Uses rep tier for auto-accept logic.
    /// </summary>
    public static bool ProposeTreaty(SimState state, string factionId)
    {
        if (state is null || string.IsNullOrEmpty(factionId)) return false;

        int rep = 0;
        state.FactionReputation.TryGetValue(factionId, out rep);

        // Check if already at max treaties
        int activeTreaties = 0;
        foreach (var act in state.DiplomaticActs.Values)
        {
            if (string.Equals(act.FactionId, factionId, StringComparison.Ordinal)
                && act.ActType == DiplomaticActType.Treaty
                && act.Status == DiplomaticActStatus.Active)
                activeTreaties++;
        }
        if (activeTreaties >= DiplomacyTweaksV0.MaxActiveTreatiesPerFaction) return false;

        // Determine acceptance
        bool accepted;
        if (rep >= DiplomacyTweaksV0.ProposalAutoAcceptRepMin)
            accepted = true;
        else if (rep <= DiplomacyTweaksV0.ProposalAutoRejectRepMax)
            accepted = false;
        else
        {
            // Neutral: 50% based on hash
            ulong hash = Fnv1a64($"player_treaty_{factionId}_{state.Tick}");
            accepted = (hash % 2UL) == 0;
        }

        var actId = $"diplo_{state.NextDiplomaticActSeq}";
        state.NextDiplomaticActSeq++;

        if (accepted)
        {
            state.DiplomaticActs[actId] = new DiplomaticAct
            {
                Id = actId,
                FactionId = factionId,
                ActType = DiplomaticActType.Treaty,
                Status = DiplomaticActStatus.Active,
                CreatedTick = state.Tick,
                ExpiryTick = state.Tick + DiplomacyTweaksV0.TreatyDurationTicks,
                TariffReductionBps = DiplomacyTweaksV0.TreatyTariffReductionBps,
                SafePassage = true,
            };
        }
        return accepted;
    }

    // ── Bounty completion ──
    // GATE.S7.DIPLOMACY.BOUNTY.001

    /// <summary>
    /// Called when an NPC fleet is destroyed. Checks for matching bounties and rewards player.
    /// </summary>
    public static void CheckBountyCompletion(SimState state, string destroyedFleetId)
    {
        if (state is null || string.IsNullOrEmpty(destroyedFleetId)) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var completedBounties = scratch.TempIds;
        completedBounties.Clear();
        foreach (var kv in state.DiplomaticActs)
        {
            var act = kv.Value;
            if (act.ActType != DiplomaticActType.Bounty) continue;
            if (act.Status != DiplomaticActStatus.Active) continue;
            if (!string.Equals(act.BountyTargetFleetId, destroyedFleetId, StringComparison.Ordinal)) continue;

            // Reward
            state.PlayerCredits += act.BountyRewardCredits;

            // Rep boost with posting faction
            if (state.FactionReputation.ContainsKey(act.FactionId))
                state.FactionReputation[act.FactionId] = Math.Min(
                    FactionTweaksV0.ReputationMax,
                    state.FactionReputation[act.FactionId] + act.BountyRewardRep);

            completedBounties.Add(kv.Key);
        }

        foreach (var id in completedBounties)
        {
            state.DiplomaticActs[id].Status = DiplomaticActStatus.Completed;
        }
    }

    // ── Treaty violation (consequences) ──
    // GATE.S7.DIPLOMACY.CONSEQUENCES.001

    /// <summary>
    /// Called when player attacks a faction fleet. Checks for treaty violation.
    /// </summary>
    public static void CheckTreatyViolation(SimState state, string attackedFactionId)
    {
        if (state is null || string.IsNullOrEmpty(attackedFactionId)) return;

        var scratch2 = s_scratch.GetOrCreateValue(state);
        var violated = scratch2.TempIds;
        violated.Clear();
        foreach (var kv in state.DiplomaticActs)
        {
            var act = kv.Value;
            if (act.ActType != DiplomaticActType.Treaty) continue;
            if (act.Status != DiplomaticActStatus.Active) continue;
            if (!string.Equals(act.FactionId, attackedFactionId, StringComparison.Ordinal)) continue;

            act.Status = DiplomaticActStatus.Violated;
            violated.Add(kv.Key);
        }

        if (violated.Count == 0) return;

        // Apply sanction
        var sanctionId = $"diplo_{state.NextDiplomaticActSeq}";
        state.NextDiplomaticActSeq++;

        state.DiplomaticActs[sanctionId] = new DiplomaticAct
        {
            Id = sanctionId,
            FactionId = attackedFactionId,
            ActType = DiplomaticActType.Sanction,
            Status = DiplomaticActStatus.Active,
            CreatedTick = state.Tick,
            ExpiryTick = state.Tick + DiplomacyTweaksV0.SanctionDurationTicks,
            SanctionTariffIncreaseBps = DiplomacyTweaksV0.SanctionTariffIncreaseBps,
            SanctionRepPenalty = DiplomacyTweaksV0.SanctionRepPenalty,
        };

        // Apply rep penalty
        if (state.FactionReputation.ContainsKey(attackedFactionId))
            state.FactionReputation[attackedFactionId] = Math.Max(
                FactionTweaksV0.ReputationMin,
                state.FactionReputation[attackedFactionId] - DiplomacyTweaksV0.SanctionRepPenalty);
    }

    /// <summary>
    /// Get total tariff modifier from active treaties and sanctions for a faction.
    /// Returns basis points adjustment (negative = discount, positive = surcharge).
    /// </summary>
    public static int GetTariffModifierBps(SimState state, string factionId)
    {
        if (state is null || string.IsNullOrEmpty(factionId)) return 0;

        int totalBps = 0;
        foreach (var act in state.DiplomaticActs.Values)
        {
            if (!string.Equals(act.FactionId, factionId, StringComparison.Ordinal)) continue;
            if (act.Status != DiplomaticActStatus.Active) continue;

            if (act.ActType == DiplomaticActType.Treaty)
                totalBps -= act.TariffReductionBps;
            else if (act.ActType == DiplomaticActType.Sanction)
                totalBps += act.SanctionTariffIncreaseBps;
        }
        return totalBps;
    }

    /// <summary>
    /// Check if player has safe passage with a faction (active treaty).
    /// </summary>
    public static bool HasSafePassage(SimState state, string factionId)
    {
        if (state is null || string.IsNullOrEmpty(factionId)) return false;

        foreach (var act in state.DiplomaticActs.Values)
        {
            if (!string.Equals(act.FactionId, factionId, StringComparison.Ordinal)) continue;
            if (act.ActType != DiplomaticActType.Treaty) continue;
            if (act.Status != DiplomaticActStatus.Active) continue;
            if (act.SafePassage) return true;
        }
        return false;
    }

    // ── Helpers ──

    private static int GetTreatyWeight(string factionId)
    {
        return factionId switch
        {
            "concord" => DiplomacyTweaksV0.ConcordTreatyWeight,
            "chitin" => DiplomacyTweaksV0.ChitinTreatyWeight,
            "weavers" => DiplomacyTweaksV0.WeaversTreatyWeight,
            "valorin" => DiplomacyTweaksV0.ValorinTreatyWeight,
            "communion" => DiplomacyTweaksV0.CommunionTreatyWeight,
            _ => 50, // STRUCTURAL: default 50/50 for unknown factions
        };
    }

    private static ulong Fnv1a64(string input)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in input) { hash ^= (byte)c; hash *= 1099511628211UL; }
        return hash;
    }
}
