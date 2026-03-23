#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // GATE.T45.DEEP_DREAD.BRIDGE.001: Deep dread state queries.

    private Godot.Collections.Dictionary _cachedDreadStateV0 = new();

    /// <summary>
    /// GetDreadStateV0: phase, hop distance from nearest faction capital,
    /// patrol density level, drain rate, exposure, adapted flag.
    /// </summary>
    public Godot.Collections.Dictionary GetDreadStateV0()
    {
        TryExecuteSafeRead(state =>
        {
            var playerNodeId = state.PlayerLocationNodeId;
            int phase = 0;
            if (!string.IsNullOrEmpty(playerNodeId)
                && state.Nodes.TryGetValue(playerNodeId, out var node))
            {
                phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
            }

            // Compute min hops from nearest faction home.
            int minHops = int.MaxValue;
            if (!string.IsNullOrEmpty(playerNodeId) && state.FactionHomeNodes != null)
            {
                foreach (var kv in state.FactionHomeNodes)
                {
                    int h = NpcTradeSystem.ComputeHopsFromFactionHome(state, kv.Key, playerNodeId);
                    if (h < minHops) minHops = h;
                }
            }
            if (minHops == int.MaxValue) minHops = -1; // STRUCTURAL: no faction homes found

            // Patrol density: "full", "half", "none"
            string patrolDensity = "full";
            if (minHops >= DeepDreadTweaksV0.PatrolHalfDensityMaxHops + 1)
                patrolDensity = "none";
            else if (minHops > DeepDreadTweaksV0.PatrolFullDensityMaxHops)
                patrolDensity = "half";

            // Drain rate (HP per tick, 0 if no drain)
            int drainRate = 0;
            if (phase == 2) drainRate = DeepDreadTweaksV0.Phase2DrainAmount;
            else if (phase == 3) drainRate = DeepDreadTweaksV0.Phase3DrainAmount;
            // Phase 4 = 0 (void paradox)

            int drainInterval = 0;
            if (phase == 2) drainInterval = DeepDreadTweaksV0.Phase2DrainIntervalTicks;
            else if (phase == 3) drainInterval = DeepDreadTweaksV0.Phase3DrainIntervalTicks;

            var d = new Godot.Collections.Dictionary
            {
                ["phase"] = phase,
                ["hops_from_capital"] = minHops,
                ["patrol_density"] = patrolDensity,
                ["drain_rate"] = drainRate,
                ["drain_interval"] = drainInterval,
                ["exposure"] = state.DeepExposure,
                ["adapted"] = ExposureTrackSystem.IsAdapted(state),
                ["exposure_mild"] = state.DeepExposure >= DeepDreadTweaksV0.ExposureMildThreshold,
                ["exposure_heavy"] = state.DeepExposure >= DeepDreadTweaksV0.ExposureHeavyThreshold,
            };

            lock (_snapshotLock)
            {
                _cachedDreadStateV0 = d;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedDreadStateV0; }
    }

    private Godot.Collections.Array<Godot.Collections.Dictionary> _cachedSensorGhostsV0 = new();

    /// <summary>
    /// GetSensorGhostsV0: array of current sensor ghost contacts.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetSensorGhostsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            if (state.SensorGhosts != null)
            {
                foreach (var g in state.SensorGhosts)
                {
                    arr.Add(new Godot.Collections.Dictionary
                    {
                        ["id"] = g.Id,
                        ["node_id"] = g.NodeId,
                        ["fleet_type"] = g.ApparentFleetType,
                        ["spawn_tick"] = g.SpawnTick,
                        ["expiry_tick"] = g.ExpiryTick,
                    });
                }
            }

            lock (_snapshotLock)
            {
                _cachedSensorGhostsV0 = arr;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedSensorGhostsV0; }
    }

    private Godot.Collections.Array<Godot.Collections.Dictionary> _cachedLatticeFaunaV0 = new();

    /// <summary>
    /// GetLatticeFaunaV0: array of lattice fauna near/at player node.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetLatticeFaunaV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            if (state.LatticeFauna != null)
            {
                foreach (var f in state.LatticeFauna)
                {
                    arr.Add(new Godot.Collections.Dictionary
                    {
                        ["id"] = f.Id,
                        ["node_id"] = f.NodeId,
                        ["state"] = (int)f.State,
                        ["spawn_tick"] = f.SpawnTick,
                        ["arrival_tick"] = f.ArrivalTick,
                        ["dark_ticks"] = f.DarkTicksAccumulated,
                    });
                }
            }

            lock (_snapshotLock)
            {
                _cachedLatticeFaunaV0 = arr;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedLatticeFaunaV0; }
    }

    private Godot.Collections.Dictionary _cachedExposureV0 = new();

    /// <summary>
    /// GetExposureV0: cumulative deep exposure with milestone flags.
    /// </summary>
    public Godot.Collections.Dictionary GetExposureV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["exposure"] = state.DeepExposure,
                ["mild"] = state.DeepExposure >= DeepDreadTweaksV0.ExposureMildThreshold,
                ["heavy"] = state.DeepExposure >= DeepDreadTweaksV0.ExposureHeavyThreshold,
                ["adapted"] = ExposureTrackSystem.IsAdapted(state),
                ["disagreement_narrow_bps"] = ExposureTrackSystem.GetDisagreementNarrowBps(state),
            };

            lock (_snapshotLock)
            {
                _cachedExposureV0 = d;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedExposureV0; }
    }

    private Godot.Collections.Dictionary _cachedInfoFogV0 = new();

    /// <summary>
    /// GetInfoFogV0: staleness level for a given node (or player's current node).
    /// </summary>
    public Godot.Collections.Dictionary GetInfoFogV0(string nodeId = "")
    {
        TryExecuteSafeRead(state =>
        {
            string targetNode = string.IsNullOrEmpty(nodeId)
                ? state.PlayerLocationNodeId ?? ""
                : nodeId;

            int staleness = InformationFogSystem.GetDataStaleness(state, targetNode);

            var d = new Godot.Collections.Dictionary
            {
                ["node_id"] = targetNode,
                ["staleness"] = staleness,
                ["detail_level"] = staleness == 0 ? "fresh" : (staleness == 1 ? "stale" : "very_stale"),
            };

            lock (_snapshotLock)
            {
                _cachedInfoFogV0 = d;
            }
        }, 0);

        lock (_snapshotLock) { return _cachedInfoFogV0; }
    }
}
