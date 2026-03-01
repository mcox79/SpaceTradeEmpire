#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // --- Fleet UI commands (Slice 3 / GATE.UI.FLEET.002, GATE.UI.FLEET.003) ---

    public bool CancelFleetJob(string fleetId, string note = "")
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(fleetId)) return false;

        int tickBefore;
        _stateLock.EnterReadLock();
        try
        {
            tickBefore = _kernel.State.Tick;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.FleetJobCancelCommand(fleetId, note));
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        var timeoutMs = Math.Max(250, (TickDelayMs * 3) + 50);
        WaitForTickAdvance(tickBefore, timeoutMs);
        return true;
    }


    // targetNodeId = "" clears manual override
    public bool SetFleetDestination(string fleetId, string targetNodeId, string note = "")
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(fleetId)) return false;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.FleetSetDestinationCommand(fleetId, targetNodeId ?? "", note));
            return true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    public Godot.Collections.Array GetFleetExplainSnapshot()
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            // Deterministic ordering: Fleet.Id Ordinal
            var fleets = state.Fleets.Values
                    .OrderBy(f => f.Id, StringComparer.Ordinal)
                    .ToArray();

            foreach (var f in fleets)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["id"] = f.Id,
                    ["current_node_id"] = f.CurrentNodeId,
                    ["state"] = f.State.ToString(),
                    ["task"] = f.CurrentTask,

                    // Authority surface required by Slice 3 UI/play capstones
                    ["active_controller"] = f.ActiveController.ToString(),
                    ["program_id"] = f.ProgramId ?? "",
                    ["manual_override_node_id"] = f.ManualOverrideNodeId ?? "",

                    // Destination surfaces (stable strings)
                    ["destination_node_id"] = f.DestinationNodeId ?? "",
                    ["final_destination_node_id"] = f.FinalDestinationNodeId ?? "",

                    // Route progress required by GATE.UI.FLEET.001
                    ["route_edge_index"] = f.RouteEdgeIndex,
                    ["route_edge_total"] = (f.RouteEdgeIds != null) ? f.RouteEdgeIds.Count : 0,
                    ["route_progress"] = $"{f.RouteEdgeIndex}/{((f.RouteEdgeIds != null) ? f.RouteEdgeIds.Count : 0)}"
                };

                // Cargo summary required by GATE.UI.FLEET.001
                if (f.Cargo != null && f.Cargo.Count > 0)
                {
                    var parts = f.Cargo
                            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}:{kv.Value}")
                            .ToArray();
                    d["cargo_summary"] = string.Join(", ", parts);
                }
                else
                {
                    d["cargo_summary"] = "(empty)";
                }

                // Job fields required by GATE.UI.FLEET.001
                if (f.CurrentJob != null)
                {
                    var j = f.CurrentJob;
                    d["job_phase"] = j.Phase.ToString();
                    d["job_good_id"] = j.GoodId ?? "";
                    d["job_amount"] = j.Amount;
                    d["job_picked_up_amount"] = j.PickedUpAmount;

                    // "remaining" for UI: while picking up, remaining = Amount - PickedUpAmount (best effort),
                    // while delivering, remaining = PickedUpAmount (amount to deliver).
                    int remaining;
                    if (j.Phase == SimCore.Entities.LogisticsJobPhase.Pickup)
                    {
                        remaining = Math.Max(0, j.Amount - j.PickedUpAmount);
                    }
                    else
                    {
                        remaining = Math.Max(0, j.PickedUpAmount);
                    }
                    d["job_remaining"] = remaining;
                }
                else
                {
                    d["job_phase"] = "";
                    d["job_good_id"] = "";
                    d["job_amount"] = 0;
                    d["job_picked_up_amount"] = 0;
                    d["job_remaining"] = 0;
                }

                arr.Add(d);
            }

            return arr;
        }
        catch
        {
            return arr;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }


    // --- Fleet UI event log snapshot (Slice 3 / GATE.UI.FLEET.EVENT.001) ---
    // Returns the last N schema-bound logistics events for the given fleet, newest-first.
    // Determinism: filter by FleetId Ordinal, order by Seq desc with stable tie-breakers.
    public Godot.Collections.Array GetFleetEventLogSnapshot(string fleetId, int maxEvents = 25)
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;
        if (string.IsNullOrWhiteSpace(fleetId)) return arr;
        if (maxEvents <= 0) return arr;
        if (maxEvents > 200) maxEvents = 200;

        _stateLock.EnterReadLock();
        try
        {
            var events = _kernel.State.LogisticsEventLog;
            if (events == null || events.Count == 0) return arr;

            var slice = events
                    .Where(e => string.Equals(e.FleetId, fleetId, StringComparison.Ordinal))
                    .OrderByDescending(e => e.Seq)
                    .ThenByDescending(e => e.Tick)
                    .ThenByDescending(e => (int)e.Type)
                    .Take(maxEvents)
                    .ToArray();

            foreach (var e in slice)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["version"] = e.Version,
                    ["seq"] = e.Seq,
                    ["tick"] = e.Tick,
                    ["type"] = (int)e.Type,

                    ["fleet_id"] = e.FleetId,
                    ["good_id"] = e.GoodId,
                    ["amount"] = e.Amount,

                    ["source_node_id"] = e.SourceNodeId,
                    ["target_node_id"] = e.TargetNodeId,
                    ["source_market_id"] = e.SourceMarketId,
                    ["target_market_id"] = e.TargetMarketId,

                    ["note"] = e.Note
                };

                arr.Add(d);
            }

            return arr;
        }
        catch
        {
            return arr;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public string GetFleetPlayabilityTranscript(int maxEventsPerFleet = 10)
    {
        if (IsLoading) return "";
        if (maxEventsPerFleet < 0) maxEventsPerFleet = 0;
        if (maxEventsPerFleet > 200) maxEventsPerFleet = 200;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var lines = new System.Collections.Generic.List<string>(256);
            lines.Add($"seed={WorldSeed} star_count={StarCount} tick={state.Tick}");

            // Deterministic ordering: Fleet.Id Ordinal
            var fleets = state.Fleets.Values
                    .OrderBy(f => f.Id, StringComparer.Ordinal)
                    .ToArray();

            foreach (var f in fleets)
            {
                var ctrl = f.ActiveController.ToString();
                var overrideTarget = f.ManualOverrideNodeId ?? "";
                var jobPhase = (f.CurrentJob != null) ? f.CurrentJob.Phase.ToString() : "";
                var jobGood = (f.CurrentJob != null) ? (f.CurrentJob.GoodId ?? "") : "";
                var jobAmt = (f.CurrentJob != null) ? f.CurrentJob.Amount : 0;
                var jobPicked = (f.CurrentJob != null) ? f.CurrentJob.PickedUpAmount : 0;

                lines.Add($"fleet={f.Id} node={f.CurrentNodeId} state={f.State} ctrl={ctrl} override={overrideTarget} task={f.CurrentTask} job_phase={jobPhase} job_good={jobGood} job_amt={jobAmt} job_picked={jobPicked} route={f.RouteEdgeIndex}/{((f.RouteEdgeIds != null) ? f.RouteEdgeIds.Count : 0)}");

                if (f.Cargo != null && f.Cargo.Count > 0)
                {
                    var cargoParts = f.Cargo
                            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}:{kv.Value}")
                            .ToArray();
                    lines.Add($"  cargo={string.Join(",", cargoParts)}");
                }
                else
                {
                    lines.Add("  cargo=(empty)");
                }

                if (maxEventsPerFleet > 0 && state.LogisticsEventLog != null && state.LogisticsEventLog.Count > 0)
                {
                    var slice = state.LogisticsEventLog
                            .Where(e => string.Equals(e.FleetId, f.Id, StringComparison.Ordinal))
                            .OrderByDescending(e => e.Seq)
                            .ThenByDescending(e => e.Tick)
                            .ThenByDescending(e => (int)e.Type)
                            .Take(maxEventsPerFleet)
                            .ToArray();

                    foreach (var e in slice)
                    {
                        lines.Add($"  ev seq={e.Seq} tick={e.Tick} type={(int)e.Type} src_node={e.SourceNodeId} dst_node={e.TargetNodeId} src_mkt={e.SourceMarketId} dst_mkt={e.TargetMarketId} good={e.GoodId} amt={e.Amount} note={e.Note}");
                    }
                }
            }

            var transcript = string.Join("\n", lines);
            var hash = Fnv1a64(transcript);
            return $"hash64={hash:X16}\n" + transcript;
        }
        catch
        {
            return "";
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }
}
