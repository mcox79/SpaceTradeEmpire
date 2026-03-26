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

    // GATE.S1.FLEET_VISUAL.MAP.001: returns FleetRole as int (0=Trader, 1=Hauler, 2=Patrol).
    public int GetFleetRoleV0(string fleetId)
    {
        int role = 0; // default Trader
        TryExecuteSafeRead(state =>
        {
            if (state.Fleets.TryGetValue(fleetId, out var fleet))
                role = (int)fleet.Role;
        }, 0);
        return role;
    }

    // GATE.T30.GALPOP.BRIDGE_TRANSIT.007: returns fleet OwnerId (faction or "ai").
    public string GetFleetOwnerIdV0(string fleetId)
    {
        string ownerId = "";
        TryExecuteSafeRead(state =>
        {
            if (state.Fleets.TryGetValue(fleetId, out var fleet))
                ownerId = fleet.OwnerId ?? "";
        }, 0);
        return ownerId;
    }

    // GATE.S11.GAME_FEEL.FLEET_STATUS.001: Returns fleet role breakdown at a node.
    // {traders (int), haulers (int), patrols (int), summary (string)}
    // summary is a compact label like "2T 1P" for galaxy map display.
    private Godot.Collections.Dictionary _cachedFleetBreakdownV0 = new();
    private string _cachedFleetBreakdownNodeV0 = "";

    public Godot.Collections.Dictionary GetNodeFleetBreakdownV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(nodeId))
        {
            result["traders"] = 0;
            result["haulers"] = 0;
            result["patrols"] = 0;
            result["summary"] = "";
            return result;
        }

        TryExecuteSafeRead(state =>
        {
            int traders = 0, haulers = 0, patrols = 0;
            foreach (var fleet in state.Fleets.Values)
            {
                if (!StringComparer.Ordinal.Equals(fleet.CurrentNodeId, nodeId))
                    continue;
                switch (fleet.Role)
                {
                    case SimCore.Entities.FleetRole.Trader: traders++; break;
                    case SimCore.Entities.FleetRole.Hauler: haulers++; break;
                    case SimCore.Entities.FleetRole.Patrol: patrols++; break;
                    default: traders++; break;
                }
            }

            var parts = new System.Collections.Generic.List<string>(3);
            if (traders > 0) parts.Add($"{traders}T");
            if (haulers > 0) parts.Add($"{haulers}H");
            if (patrols > 0) parts.Add($"{patrols}P");

            var d = new Godot.Collections.Dictionary
            {
                ["traders"] = traders,
                ["haulers"] = haulers,
                ["patrols"] = patrols,
                ["summary"] = parts.Count > 0 ? string.Join(" ", parts) : ""
            };

            lock (_snapshotLock)
            {
                _cachedFleetBreakdownV0 = d;
                _cachedFleetBreakdownNodeV0 = nodeId;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedFleetBreakdownV0; }
    }

    // GATE.S16.NPC_ALIVE.TRANSIT_SNAP.001: Fleet transit snapshot for a local system.
    // Returns all fleets at or transiting through a node (current or destination matches).
    // GDScript uses this to position and animate NPC ships along lanes.
    private Godot.Collections.Array _cachedFleetTransitV0 = new();
    private string _cachedFleetTransitNodeV0 = "";

    public Godot.Collections.Array GetFleetTransitFactsV0(string nodeId)
    {
        var arr = new Godot.Collections.Array();
        if (string.IsNullOrEmpty(nodeId)) return arr;

        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            foreach (var fleet in state.Fleets.Values)
            {
                // Fleet is relevant if it's at this node or traveling to/from it.
                bool atNode = StringComparer.Ordinal.Equals(fleet.CurrentNodeId, nodeId);
                bool travelingTo = fleet.State == SimCore.Entities.FleetState.Traveling
                    && StringComparer.Ordinal.Equals(fleet.DestinationNodeId, nodeId);

                if (!atNode && !travelingTo) continue;

                var d = new Godot.Collections.Dictionary
                {
                    ["fleet_id"] = fleet.Id ?? "",
                    ["role"] = (int)fleet.Role,
                    ["state"] = fleet.State.ToString(),
                    ["current_node_id"] = fleet.CurrentNodeId ?? "",
                    ["destination_node_id"] = fleet.DestinationNodeId ?? "",
                    ["current_edge_id"] = fleet.CurrentEdgeId ?? "",
                    ["travel_progress"] = fleet.TravelProgress,
                    ["speed"] = fleet.Speed,
                    ["hull_hp"] = fleet.HullHp,
                    ["hull_hp_max"] = fleet.HullHpMax,
                    // GATE.T30.GALPOP.HOSTILE_FIX.003: Compute hostile from faction reputation.
                    ["is_hostile"] = ComputeFleetHostileV0(state, fleet),
                    ["owner_id"] = fleet.OwnerId ?? "",
                    ["final_destination_node_id"] = fleet.FinalDestinationNodeId ?? "",
                    ["current_task"] = fleet.CurrentTask ?? "",
                };
                result.Add(d);
            }

            lock (_snapshotLock)
            {
                _cachedFleetTransitV0 = result;
                _cachedFleetTransitNodeV0 = nodeId;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedFleetTransitV0; }
    }

    // GATE.T30.GALPOP.HOSTILE_FIX.003 + GATE.T55.COMBAT.PIRATE_FACTION.001 + GATE.T55.COMBAT.TERRITORY_ENFORCE.001:
    // Compute hostile status from fleet owner faction + player reputation.
    // Only Patrol fleets owned by a non-player faction can be hostile.
    // Default: non-hostile (safe fallback). Hostility requires reputation below aggro threshold.
    // Pirates are always hostile (AggroReputationThreshold = 999, any rep triggers).
    // Territory enforcement: at Closed-regime nodes, faction patrols are hostile when rep < TerritoryHostileThreshold.
    private static bool ComputeFleetHostileV0(SimCore.SimState state, SimCore.Entities.Fleet fleet)
    {
        if (fleet.Role != SimCore.Entities.FleetRole.Patrol)
            return false;
        if (StringComparer.Ordinal.Equals(fleet.OwnerId, "player"))
            return false;

        // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirates are always hostile.
        if (StringComparer.Ordinal.Equals(fleet.OwnerId, SimCore.Tweaks.FactionTweaksV0.PirateId))
            return true;

        // Check player reputation with fleet's owner faction.
        int rep = SimCore.Tweaks.FactionTweaksV0.ReputationDefault;
        if (!string.IsNullOrEmpty(fleet.OwnerId)
            && state.FactionReputation.TryGetValue(fleet.OwnerId, out var storedRep))
        {
            rep = storedRep;
        }

        // Standard aggro check: reputation below threshold.
        if (rep < SimCore.Tweaks.FactionTweaksV0.AggroReputationThreshold)
            return true;

        // GATE.T55.COMBAT.TERRITORY_ENFORCE.001: Territory enforcement at Closed-regime nodes.
        // If the node's committed regime is Hostile (derived from Closed trade policy + low rep),
        // AND player rep with the fleet's faction is below TerritoryHostileThreshold, mark hostile.
        string nodeId = fleet.CurrentNodeId ?? "";
        if (!string.IsNullOrEmpty(nodeId))
        {
            var regime = SimCore.Systems.ReputationSystem.GetEffectiveRegime(state, nodeId);
            if (regime == SimCore.Systems.TerritoryRegime.Hostile
                && rep < SimCore.Tweaks.FactionTweaksV0.TerritoryHostileThreshold)
            {
                return true;
            }
        }

        return false;
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

    // GATE.S7.FLEET_TAB.LIST.001: Fleet roster for the master list panel.
    // Returns player-owned fleets as an Array of Dicts, ordered by Id Ordinal asc.
    // Each dict: ship_id (string), ship_class (string), hull_hp_pct (float 0-1),
    // shield_hp_pct (float 0-1), location_name (string), job_status (string).
    // Nonblocking: returns last cached array if read lock is unavailable.
    private Godot.Collections.Array _cachedFleetRosterV0 = new();

    public Godot.Collections.Array GetFleetRosterV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();

            // Deterministic ordering: Fleet.Id Ordinal, player-owned only.
            var fleets = state.Fleets.Values
                .Where(f => string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
                .OrderBy(f => f.Id, StringComparer.Ordinal)
                .ToArray();

            foreach (var f in fleets)
            {
                // Ship class display name from content registry.
                var classDef = SimCore.Content.ShipClassContentV0.GetById(f.ShipClassId ?? "");
                string shipClass = classDef?.DisplayName ?? f.ShipClassId ?? "Unknown";

                // HP percentages (0-1). Guard against uninitialized (-1) or zero max.
                float hullPct = (f.HullHpMax > 0) ? Math.Clamp((float)f.HullHp / f.HullHpMax, 0f, 1f) : 1f;
                float shieldPct = (f.ShieldHpMax > 0) ? Math.Clamp((float)f.ShieldHp / f.ShieldHpMax, 0f, 1f) : 1f;

                // Location display name from node registry.
                string locationName = f.CurrentNodeId ?? "";
                if (!string.IsNullOrEmpty(f.CurrentNodeId)
                    && state.Nodes.TryGetValue(f.CurrentNodeId, out var node))
                {
                    locationName = node.Name ?? f.CurrentNodeId ?? "";
                }

                // Job status: concise summary for the roster row.
                string jobStatus;
                if (f.CurrentJob != null)
                {
                    var phase = f.CurrentJob.Phase.ToString();
                    var good = f.CurrentJob.GoodId ?? "";
                    jobStatus = string.IsNullOrEmpty(good) ? phase : $"{phase}:{good}";
                }
                else
                {
                    jobStatus = f.CurrentTask ?? "Idle";
                }

                // GATE.T59.SHIP.BRIDGE_FLEET.001: Ship class stats for roster display.
                int combatIndex = 0;
                int tradeIndex = 0;
                int exploreIndex = 0;
                string shipClassId = f.ShipClassId ?? "";
                if (classDef != null)
                {
                    // Combat index: hull + shield + (weapon slot count * 10).
                    int weaponSlots = 0;
                    if (f.Slots != null)
                    {
                        foreach (var slot in f.Slots)
                        {
                            if (slot.SlotKind == SimCore.Entities.SlotKind.Weapon
                                && !string.IsNullOrEmpty(slot.InstalledModuleId))
                                weaponSlots++;
                        }
                    }
                    combatIndex = classDef.CoreHull + classDef.BaseShield + (weaponSlots * 10); // STRUCTURAL: 10 per weapon slot
                    // Trade index: cargo capacity.
                    tradeIndex = classDef.CargoCapacity;
                    // Explore index: scan range + fuel capacity / 10.
                    exploreIndex = classDef.ScanRange + (classDef.BaseFuelCapacity / 10); // STRUCTURAL: /10 scale fuel
                }

                var d = new Godot.Collections.Dictionary
                {
                    ["ship_id"] = f.Id ?? "",
                    ["ship_class"] = shipClass,
                    ["ship_class_id"] = shipClassId,
                    ["hull_hp_pct"] = hullPct,
                    ["shield_hp_pct"] = shieldPct,
                    ["location_name"] = locationName,
                    ["job_status"] = jobStatus,
                    ["is_stored"] = f.IsStored,
                    // GATE.T59.SHIP.BRIDGE_FLEET.001: Per-fleet class stats.
                    ["combat_index"] = combatIndex,
                    ["trade_index"] = tradeIndex,
                    ["explore_index"] = exploreIndex,
                };

                result.Add(d);
            }

            lock (_snapshotLock)
            {
                _cachedFleetRosterV0 = result;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedFleetRosterV0; }
    }

    // GATE.S7.FLEET_TAB.DETAIL.001: Detailed ship info for the fleet detail panel.
    // Returns a Dictionary with: ship_id, ship_class, hull_hp, hull_hp_max, shield_hp,
    // shield_hp_max, speed, modules (Array of Dicts with slot_id, module_id, display_name),
    // location_name, job_status.
    // Nonblocking: returns last cached dict if read lock is unavailable.
    private Godot.Collections.Dictionary _cachedFleetShipDetailV0 = new();
    private string _cachedFleetShipDetailIdV0 = "";

    public Godot.Collections.Dictionary GetFleetShipDetailV0(string shipId)
    {
        var empty = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(shipId)) return empty;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(shipId, out var fleet)) return;
            bool hasR1 = state.StoryState?.HasRevelation(SimCore.Entities.RevelationFlags.R1_Module) ?? false;

            // Ship class display name from content registry.
            var classDef = SimCore.Content.ShipClassContentV0.GetById(fleet.ShipClassId ?? "");
            string shipClass = classDef?.DisplayName ?? fleet.ShipClassId ?? "Unknown";

            // Location display name from node registry.
            string locationName = fleet.CurrentNodeId ?? "";
            if (!string.IsNullOrEmpty(fleet.CurrentNodeId)
                && state.Nodes.TryGetValue(fleet.CurrentNodeId, out var node))
            {
                locationName = node.Name ?? fleet.CurrentNodeId ?? "";
            }

            // Job status: concise summary.
            string jobStatus;
            if (fleet.CurrentJob != null)
            {
                var phase = fleet.CurrentJob.Phase.ToString();
                var good = fleet.CurrentJob.GoodId ?? "";
                jobStatus = string.IsNullOrEmpty(good) ? phase : $"{phase}:{good}";
            }
            else
            {
                jobStatus = fleet.CurrentTask ?? "Idle";
            }

            // Module loadout with display names from UpgradeContentV0.
            var modules = new Godot.Collections.Array();
            if (fleet.Slots != null)
            {
                foreach (var slot in fleet.Slots.OrderBy(s => s.SlotId, StringComparer.Ordinal))
                {
                    var modDict = new Godot.Collections.Dictionary();
                    modDict["slot_id"] = slot.SlotId;
                    modDict["module_id"] = slot.InstalledModuleId ?? "";

                    string displayName = "";
                    if (!string.IsNullOrEmpty(slot.InstalledModuleId))
                    {
                        var modDef = SimCore.Content.UpgradeContentV0.GetById(slot.InstalledModuleId);
                        displayName = modDef?.DisplayName ?? slot.InstalledModuleId;
                        // GATE.X.COVER_STORY.BRIDGE_WIRE.001: Apply cover-story naming.
                        displayName = ApplyCoverName(displayName, hasR1);
                    }
                    modDict["display_name"] = displayName;

                    modules.Add(modDict);
                }
            }

            var d = new Godot.Collections.Dictionary
            {
                ["ship_id"] = fleet.Id ?? "",
                ["ship_class"] = shipClass,
                ["hull_hp"] = fleet.HullHp,
                ["hull_hp_max"] = fleet.HullHpMax,
                ["shield_hp"] = fleet.ShieldHp,
                ["shield_hp_max"] = fleet.ShieldHpMax,
                ["speed"] = fleet.Speed,
                ["modules"] = modules,
                ["location_name"] = locationName,
                ["job_status"] = jobStatus
            };

            lock (_snapshotLock)
            {
                _cachedFleetShipDetailV0 = d;
                _cachedFleetShipDetailIdV0 = shipId;
            }
        }, 0);

        lock (_snapshotLock)
        {
            // Return cached result only if it matches the requested shipId.
            if (StringComparer.Ordinal.Equals(_cachedFleetShipDetailIdV0, shipId))
                return _cachedFleetShipDetailV0;
            return empty;
        }
    }

    // GATE.S7.FLEET_TAB.ACTIONS.001: Recall a fleet ship back to the player's current node.
    // Reuses FleetSetDestinationCommand with the player's current location as the target.
    public bool FleetRecallV0(string shipId)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(shipId)) return false;

        // Find the player's current node to use as the recall destination.
        string playerNodeId = "";
        TryExecuteSafeRead(state =>
        {
            playerNodeId = state.PlayerLocationNodeId ?? "";
        });

        if (string.IsNullOrEmpty(playerNodeId)) return false;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.FleetSetDestinationCommand(shipId, playerNodeId, "ui_recall"));
            return true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // GATE.S7.FLEET_TAB.ACTIONS.001: Dismiss (remove) a fleet ship from the player's fleet.
    // Removes the fleet entry from state.Fleets entirely. Cannot dismiss the player's own ship.
    public bool FleetDismissV0(string shipId)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(shipId)) return false;

        // Never allow dismissing the player's own hero ship.
        if (string.Equals(shipId, "fleet_trader_1", StringComparison.Ordinal)) return false;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (state.Fleets.ContainsKey(shipId))
            {
                state.Fleets.Remove(shipId);
                return true;
            }
            return false;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // GATE.S7.FLEET_TAB.ACTIONS.001: Rename a fleet ship (placeholder — does nothing yet).
    public bool FleetRenameV0(string shipId, string newName)
    {
        // Placeholder: rename functionality is not yet implemented.
        return true;
    }

    // GATE.S5.TRACTOR.BRIDGE.001: Tractor beam range query.
    // Returns {range (int), has_tractor (bool), module_id (string)}.
    // has_tractor is true only if an equipped module provides range > fallback.
    public Godot.Collections.Dictionary GetTractorRangeV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["range"] = SimCore.Tweaks.HavenTweaksV0.TractorFallbackRange,
            ["has_tractor"] = false,
            ["module_id"] = ""
        };
        if (string.IsNullOrEmpty(fleetId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;

            int bestRange = 0;
            string bestModuleId = "";
            if (fleet.Slots != null)
            {
                foreach (var slot in fleet.Slots)
                {
                    if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                    if (slot.Disabled) continue;
                    var def = SimCore.Content.UpgradeContentV0.GetById(slot.InstalledModuleId);
                    if (def != null && def.TractorRange > bestRange)
                    {
                        bestRange = def.TractorRange;
                        bestModuleId = slot.InstalledModuleId;
                    }
                }
            }

            int effectiveRange = bestRange > 0 ? bestRange : SimCore.Tweaks.HavenTweaksV0.TractorFallbackRange;
            bool hasTractor = bestRange > SimCore.Tweaks.HavenTweaksV0.TractorFallbackRange;

            result["range"] = effectiveRange;
            result["has_tractor"] = hasTractor;
            result["module_id"] = bestModuleId;
        });

        return result;
    }

    // GATE.X.FLEET_UPKEEP.BRIDGE.001: Per-fleet upkeep cost and cycle info.
    // Returns {fleet_id, upkeep_cost, cycle_ticks, is_docked, delinquent_cycles}.
    public Godot.Collections.Dictionary GetFleetUpkeepV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["fleet_id"] = fleetId ?? "",
            ["upkeep_cost"] = 0,
            ["cycle_ticks"] = SimCore.Tweaks.FleetUpkeepTweaksV0.UpkeepCycleTicks,
            ["is_docked"] = false,
            ["delinquent_cycles"] = 0,
        };
        if (string.IsNullOrEmpty(fleetId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;

            int baseCost = FleetUpkeepSystem.GetUpkeepForClass(fleet.ShipClassId);
            bool isDocked = !fleet.IsMoving && !string.IsNullOrEmpty(fleet.CurrentNodeId);
            int cost = isDocked
                ? (int)((long)baseCost * SimCore.Tweaks.FleetUpkeepTweaksV0.DockedMultiplierBps / SimCore.Tweaks.FleetUpkeepTweaksV0.BpsDivisor)
                : baseCost;
            if (cost <= 0) cost = 1;

            result["upkeep_cost"] = cost;
            result["is_docked"] = isDocked;
            result["delinquent_cycles"] = fleet.UpkeepDelinquentCycles;
        });

        return result;
    }

    // GATE.X.FLEET_UPKEEP.BRIDGE.001: Empire total upkeep.
    // Returns {total_upkeep, fleet_count, cycle_ticks}.
    public Godot.Collections.Dictionary GetTotalUpkeepV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["total_upkeep"] = 0,
            ["fleet_count"] = 0,
            ["cycle_ticks"] = SimCore.Tweaks.FleetUpkeepTweaksV0.UpkeepCycleTicks,
        };

        TryExecuteSafeRead(state =>
        {
            int total = 0;
            int count = 0;
            foreach (var fleet in state.Fleets.Values)
            {
                if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;

                int baseCost = FleetUpkeepSystem.GetUpkeepForClass(fleet.ShipClassId);
                if (baseCost <= 0) continue;

                bool isDocked = !fleet.IsMoving && !string.IsNullOrEmpty(fleet.CurrentNodeId);
                int cost = isDocked
                    ? (int)((long)baseCost * SimCore.Tweaks.FleetUpkeepTweaksV0.DockedMultiplierBps / SimCore.Tweaks.FleetUpkeepTweaksV0.BpsDivisor)
                    : baseCost;
                if (cost <= 0) cost = 1;

                total += cost;
                count++;
            }
            result["total_upkeep"] = total;
            result["fleet_count"] = count;
        });

        return result;
    }

    // GATE.T48.TENSION.UPKEEP_BRIDGE.001: Aggregate fleet upkeep summary for HUD + dock display.
    // Returns {credits_per_cycle, fuel_per_cycle, hull_degrad_per_cycle, wage_per_cycle,
    //          runway_ticks, is_docked}.
    private Godot.Collections.Dictionary _cachedFleetUpkeepSummaryV0 = new();

    public Godot.Collections.Dictionary GetFleetUpkeepSummaryV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["credits_per_cycle"] = 0,
            ["fuel_per_cycle"] = 0,
            ["hull_degrad_per_cycle"] = 0,
            ["wage_per_cycle"] = 0,
            ["runway_ticks"] = 9999,
            ["is_docked"] = false,
        };

        TryExecuteSafeRead(state =>
        {
            int totalCreditsPerCycle = 0;
            int totalFuelPerCycle = 0;
            int totalHullDegradPerCycle = 0;
            int totalWagePerCycle = 0;
            bool anyDocked = false;

            foreach (var fleet in state.Fleets.Values)
            {
                if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;

                bool isDocked = !fleet.IsMoving && !string.IsNullOrEmpty(fleet.CurrentNodeId);
                if (isDocked) anyDocked = true;

                // Credit upkeep per cycle.
                int baseCost = FleetUpkeepSystem.GetUpkeepForClass(fleet.ShipClassId);
                if (baseCost > 0)
                {
                    int cost = isDocked
                        ? (int)((long)baseCost * SimCore.Tweaks.FleetUpkeepTweaksV0.DockedMultiplierBps / SimCore.Tweaks.FleetUpkeepTweaksV0.BpsDivisor)
                        : baseCost;
                    if (cost <= 0) cost = 1; // STRUCT_MIN
                    totalCreditsPerCycle += cost;
                }

                // Fuel per cycle (only when not docked).
                if (!isDocked)
                {
                    int fuelCost = FleetUpkeepSystem.GetFuelPerCycle(fleet.ShipClassId);
                    totalFuelPerCycle += fuelCost;
                }

                // Hull degradation per cycle (only when not docked).
                if (!isDocked)
                {
                    int hullDmg = FleetUpkeepSystem.GetHullDegradPerCycle(fleet.ShipClassId);
                    totalHullDegradPerCycle += hullDmg;
                }

                // Wages per cycle.
                int baseWage = FleetUpkeepSystem.GetWagePerCycle(fleet.ShipClassId);
                if (baseWage > 0)
                {
                    int wage = isDocked
                        ? (int)((long)baseWage * SimCore.Tweaks.FleetUpkeepTweaksV0.DockedWageMultiplierBps / SimCore.Tweaks.FleetUpkeepTweaksV0.BpsDivisor)
                        : baseWage;
                    if (wage <= 0) wage = 1; // STRUCT_MIN
                    totalWagePerCycle += wage;
                }
            }

            // Runway: estimated ticks before credits run out.
            int totalDrainPerCycle = totalCreditsPerCycle + totalWagePerCycle;
            int runway = 9999; // STRUCTURAL: default stable
            if (totalDrainPerCycle > 0 && state.PlayerCredits >= 0)
            {
                int cyclesLeft = (int)(state.PlayerCredits / totalDrainPerCycle);
                runway = cyclesLeft * SimCore.Tweaks.FleetUpkeepTweaksV0.UpkeepCycleTicks;
                if (runway > 9999) runway = 9999; // STRUCTURAL: cap
            }

            result["credits_per_cycle"] = totalCreditsPerCycle;
            result["fuel_per_cycle"] = totalFuelPerCycle;
            result["hull_degrad_per_cycle"] = totalHullDegradPerCycle;
            result["wage_per_cycle"] = totalWagePerCycle;
            result["runway_ticks"] = runway;
            result["is_docked"] = anyDocked;

            _cachedFleetUpkeepSummaryV0 = result;
        });

        return _cachedFleetUpkeepSummaryV0;
    }

    // GATE.S7.SUSTAIN.BRIDGE_PROOF.001: Fleet sustain status — fuel level, module sustain health.
    public Godot.Collections.Dictionary GetFleetSustainStatusV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(fleetId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;

            result["fleet_id"] = fleet.Id;
            result["fuel"] = fleet.FuelCurrent;
            result["fuel_capacity"] = fleet.FuelCapacity;
            result["state"] = fleet.State.ToString();
            result["current_task"] = fleet.CurrentTask ?? "";
            result["is_immobilized"] = string.Equals(fleet.CurrentTask, "Immobilized:NoFuel", StringComparison.Ordinal);

            // Module sustain info.
            var modules = new Godot.Collections.Array();
            if (fleet.Slots != null)
            {
                foreach (var slot in fleet.Slots)
                {
                    if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                    var mod = new Godot.Collections.Dictionary();
                    mod["slot_id"] = slot.SlotId;
                    mod["module_id"] = slot.InstalledModuleId;
                    mod["condition"] = slot.Condition;
                    mod["disabled"] = slot.Disabled;
                    mod["power_draw"] = slot.PowerDraw;
                    modules.Add(mod);
                }
            }
            result["modules"] = modules;
        });

        return result;
    }

    // GATE.T59.SHIP.BRIDGE_FLEET.001: Swap active (hero) ship with a stored fleet.
    // Delegates to HavenHangarSystem.SwapShip after validation.
    // Returns {success (bool), message (string)}.
    public Godot.Collections.Dictionary SetActiveFleetV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["success"] = false,
            ["message"] = ""
        };

        if (IsLoading)
        {
            result["message"] = "Loading in progress";
            return result;
        }

        if (string.IsNullOrWhiteSpace(fleetId))
        {
            result["message"] = "Invalid fleet ID";
            return result;
        }

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;

            // Validate target fleet exists and is stored.
            if (!state.Fleets.TryGetValue(fleetId, out var targetFleet))
            {
                result["message"] = "Fleet not found";
                return result;
            }
            if (!string.Equals(targetFleet.OwnerId, "player", StringComparison.Ordinal))
            {
                result["message"] = "Not a player-owned fleet";
                return result;
            }
            if (!targetFleet.IsStored)
            {
                result["message"] = "Fleet is not stored";
                return result;
            }

            // Find current hero fleet (player-owned, not stored).
            string heroFleetId = "";
            foreach (var kv in state.Fleets)
            {
                if (string.Equals(kv.Value.OwnerId, "player", StringComparison.Ordinal)
                    && !kv.Value.IsStored)
                {
                    heroFleetId = kv.Key;
                    break;
                }
            }
            if (string.IsNullOrEmpty(heroFleetId))
            {
                result["message"] = "No active hero fleet found";
                return result;
            }

            // Delegate to HavenHangarSystem.SwapShip (validates Haven discovered + hero at Haven node).
            bool swapped = SimCore.Systems.HavenHangarSystem.SwapShip(state, heroFleetId, fleetId);
            if (!swapped)
            {
                // SwapShip fails if Haven is not discovered, hero is not at Haven node,
                // or target fleet is not in StoredShipIds.
                result["message"] = "Swap failed — hero must be docked at Haven";
                return result;
            }

            result["success"] = true;
            result["message"] = "Ship swapped successfully";
            return result;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // GATE.S5.TRACTOR.AUTO_TARGET.001: Returns nearest loot drop at the player's current node.
    // Returns {has_loot, loot_id, rarity, credits, goods_count} or empty if none.
    public Godot.Collections.Dictionary GetNearestLootV0()
    {
        var result = new Godot.Collections.Dictionary { ["has_loot"] = false };
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var fleet)) return;
            var nodeId = fleet.CurrentNodeId;
            if (string.IsNullOrEmpty(nodeId)) return;

            // Check if fleet has a tractor module installed.
            int tractorRange = 0;
            foreach (var slot in fleet.Slots)
            {
                if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                var modDef = SimCore.Content.UpgradeContentV0.GetById(slot.InstalledModuleId);
                if (modDef != null && modDef.TractorRange > tractorRange)
                    tractorRange = modDef.TractorRange;
            }
            if (tractorRange <= 0) return;

            // Find first loot drop at the same node (deterministic: earliest TickCreated).
            SimCore.Entities.LootDrop? nearest = null;
            foreach (var kv in state.LootDrops)
            {
                if (!string.Equals(kv.Value.NodeId, nodeId, StringComparison.Ordinal)) continue;
                if (nearest == null || kv.Value.TickCreated < nearest.TickCreated)
                    nearest = kv.Value;
            }

            if (nearest != null)
            {
                result["has_loot"] = true;
                result["loot_id"] = nearest.Id;
                result["rarity"] = nearest.Rarity.ToString();
                result["credits"] = nearest.Credits;
                result["goods_count"] = nearest.Goods.Count;
            }
        }, 0);
        return result;
    }

}
