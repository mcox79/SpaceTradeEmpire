#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

// GATE.S7.DIPLOMACY.BRIDGE.001: Diplomacy queries + actions.
public partial class SimBridge
{
    private Godot.Collections.Array _cachedActiveTreatiesV0 = new Godot.Collections.Array();
    private Godot.Collections.Array _cachedAvailableBountiesV0 = new Godot.Collections.Array();
    private Godot.Collections.Array _cachedDiplomaticProposalsV0 = new Godot.Collections.Array();
    private Godot.Collections.Array _cachedSanctionsV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns active treaties: [{id, faction_id, tariff_reduction_bps, safe_passage, expiry_tick, ticks_remaining}]
    /// </summary>
    public Godot.Collections.Array GetActiveTreatiesV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var kv in state.DiplomaticActs)
            {
                var act = kv.Value;
                if (act.ActType != SimCore.Entities.DiplomaticActType.Treaty) continue;
                if (act.Status != SimCore.Entities.DiplomaticActStatus.Active) continue;
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = act.Id,
                    ["faction_id"] = act.FactionId,
                    ["tariff_reduction_bps"] = act.TariffReductionBps,
                    ["safe_passage"] = act.SafePassage,
                    ["expiry_tick"] = act.ExpiryTick,
                    ["ticks_remaining"] = Math.Max(0, act.ExpiryTick - state.Tick),
                });
            }
            lock (_snapshotLock) { _cachedActiveTreatiesV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedActiveTreatiesV0; }
    }

    /// <summary>
    /// Returns available bounties: [{id, faction_id, target_fleet_id, reward_credits, reward_rep, expiry_tick, ticks_remaining}]
    /// </summary>
    public Godot.Collections.Array GetAvailableBountiesV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var kv in state.DiplomaticActs)
            {
                var act = kv.Value;
                if (act.ActType != SimCore.Entities.DiplomaticActType.Bounty) continue;
                if (act.Status != SimCore.Entities.DiplomaticActStatus.Active) continue;
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = act.Id,
                    ["faction_id"] = act.FactionId,
                    ["target_fleet_id"] = act.BountyTargetFleetId,
                    ["reward_credits"] = act.BountyRewardCredits,
                    ["reward_rep"] = act.BountyRewardRep,
                    ["expiry_tick"] = act.ExpiryTick,
                    ["ticks_remaining"] = Math.Max(0, act.ExpiryTick - state.Tick),
                });
            }
            lock (_snapshotLock) { _cachedAvailableBountiesV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedAvailableBountiesV0; }
    }

    /// <summary>
    /// Returns pending diplomatic proposals: [{id, faction_id, tariff_reduction_bps, safe_passage, expiry_tick, ticks_remaining}]
    /// </summary>
    public Godot.Collections.Array GetDiplomaticProposalsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var kv in state.DiplomaticActs)
            {
                var act = kv.Value;
                if (act.ActType != SimCore.Entities.DiplomaticActType.Proposal) continue;
                if (act.Status != SimCore.Entities.DiplomaticActStatus.Pending) continue;
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = act.Id,
                    ["faction_id"] = act.FactionId,
                    ["tariff_reduction_bps"] = act.TariffReductionBps,
                    ["safe_passage"] = act.SafePassage,
                    ["expiry_tick"] = act.ExpiryTick,
                    ["ticks_remaining"] = Math.Max(0, act.ExpiryTick - state.Tick),
                });
            }
            lock (_snapshotLock) { _cachedDiplomaticProposalsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedDiplomaticProposalsV0; }
    }

    /// <summary>
    /// Returns active sanctions: [{id, faction_id, tariff_increase_bps, rep_penalty, expiry_tick, ticks_remaining}]
    /// </summary>
    public Godot.Collections.Array GetSanctionsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var kv in state.DiplomaticActs)
            {
                var act = kv.Value;
                if (act.ActType != SimCore.Entities.DiplomaticActType.Sanction) continue;
                if (act.Status != SimCore.Entities.DiplomaticActStatus.Active) continue;
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = act.Id,
                    ["faction_id"] = act.FactionId,
                    ["tariff_increase_bps"] = act.SanctionTariffIncreaseBps,
                    ["rep_penalty"] = act.SanctionRepPenalty,
                    ["expiry_tick"] = act.ExpiryTick,
                    ["ticks_remaining"] = Math.Max(0, act.ExpiryTick - state.Tick),
                });
            }
            lock (_snapshotLock) { _cachedSanctionsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedSanctionsV0; }
    }

    /// <summary>
    /// Propose a treaty with a faction. Returns true if accepted.
    /// </summary>
    public bool ProposeTreatyV0(string factionId)
    {
        bool accepted = false;
        _stateLock.EnterWriteLock();
        try
        {
            accepted = DiplomacySystem.ProposeTreaty(_kernel.State, factionId);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return accepted;
    }

    /// <summary>
    /// Accept a pending diplomatic proposal.
    /// </summary>
    public bool AcceptProposalV0(string actId)
    {
        bool success = false;
        _stateLock.EnterWriteLock();
        try
        {
            success = DiplomacySystem.AcceptProposal(_kernel.State, actId);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return success;
    }

    /// <summary>
    /// Reject a pending diplomatic proposal.
    /// </summary>
    public bool RejectProposalV0(string actId)
    {
        bool success = false;
        _stateLock.EnterWriteLock();
        try
        {
            success = DiplomacySystem.RejectProposal(_kernel.State, actId);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return success;
    }
}
