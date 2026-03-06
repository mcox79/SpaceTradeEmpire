#nullable enable

using Godot;
using SimCore;
using SimCore.Programs;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// GATE.S5.ESCORT_PROG.BRIDGE.001: Escort and Patrol program bridge methods.
public partial class SimBridge
{
    /// <summary>
    /// Creates an Escort program: fleet travels from origin to destination node.
    /// Returns program id (empty on failure).
    /// </summary>
    public string CreateEscortProgramV0(string fleetId, string originNodeId, string destNodeId, int cadenceTicks)
    {
        if (IsLoading) return "";
        if (string.IsNullOrWhiteSpace(fleetId)) return "";
        if (string.IsNullOrWhiteSpace(originNodeId)) return "";
        if (string.IsNullOrWhiteSpace(destNodeId)) return "";
        if (cadenceTicks <= 0) cadenceTicks = 1;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            state.Programs ??= new ProgramBook();

            var id = $"P{state.NextProgramSeq}";
            state.NextProgramSeq = checked(state.NextProgramSeq + 1);

            var p = new ProgramInstance
            {
                Id = id,
                Kind = ProgramKind.EscortV0,
                Status = ProgramStatus.Paused,
                CreatedTick = state.Tick,
                CadenceTicks = cadenceTicks,
                NextRunTick = state.Tick,
                LastRunTick = -1,
                FleetId = fleetId,
                SourceMarketId = originNodeId,
                MarketId = destNodeId,
                ExpeditionTicksRemaining = 0,
            };

            state.Programs.Instances[id] = p;
            return id;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Creates a Patrol program: fleet cycles between two nodes.
    /// Returns program id (empty on failure).
    /// </summary>
    public string CreatePatrolProgramV0(string fleetId, string nodeA, string nodeB, int cadenceTicks)
    {
        if (IsLoading) return "";
        if (string.IsNullOrWhiteSpace(fleetId)) return "";
        if (string.IsNullOrWhiteSpace(nodeA)) return "";
        if (string.IsNullOrWhiteSpace(nodeB)) return "";
        if (cadenceTicks <= 0) cadenceTicks = 1;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            state.Programs ??= new ProgramBook();

            var id = $"P{state.NextProgramSeq}";
            state.NextProgramSeq = checked(state.NextProgramSeq + 1);

            var p = new ProgramInstance
            {
                Id = id,
                Kind = ProgramKind.PatrolV0,
                Status = ProgramStatus.Paused,
                CreatedTick = state.Tick,
                CadenceTicks = cadenceTicks,
                NextRunTick = state.Tick,
                LastRunTick = -1,
                FleetId = fleetId,
                MarketId = nodeA,
                SourceMarketId = nodeB,
                ExpeditionTicksRemaining = 0,
            };

            state.Programs.Instances[id] = p;
            return id;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns status of an escort/patrol program.
    /// </summary>
    public Godot.Collections.Dictionary GetEscortStatusV0(string programId)
    {
        var d = new Godot.Collections.Dictionary();
        if (IsLoading) return d;
        if (string.IsNullOrWhiteSpace(programId)) return d;

        TryExecuteSafeRead(state =>
        {
            if (state.Programs?.Instances == null) return;
            if (!state.Programs.Instances.TryGetValue(programId, out var p)) return;
            if (p.Kind != ProgramKind.EscortV0 && p.Kind != ProgramKind.PatrolV0) return;

            var result = new Godot.Collections.Dictionary
            {
                ["program_id"] = p.Id ?? "",
                ["kind"] = p.Kind ?? "",
                ["status"] = p.Status.ToString(),
                ["fleet_id"] = p.FleetId ?? "",
                ["origin_node_id"] = p.SourceMarketId ?? "",
                ["dest_node_id"] = p.MarketId ?? "",
                ["progress_ticks"] = p.ExpeditionTicksRemaining,
                ["last_run_tick"] = p.LastRunTick,
                ["created_tick"] = p.CreatedTick,
            };
            lock (_snapshotLock) { d = result; }
        });

        lock (_snapshotLock) { return d; }
    }

    /// <summary>
    /// Returns status of a patrol program (alias for GetEscortStatusV0).
    /// </summary>
    public Godot.Collections.Dictionary GetPatrolStatusV0(string programId)
        => GetEscortStatusV0(programId);
}
