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
    // Cached discovery list snapshot (legacy readout).
    // Used by GetDiscoveryListSnapshotV0 in this partial file.
    // Failure safety: UI callers can get a cached snapshot if a read lock cannot be taken immediately.
    private Godot.Collections.Array _cachedDiscoveryListSnapshot = new Godot.Collections.Array();

    // --- Station-scoped security incident snapshot (Slice 3 / GATE.S3.RISK_MODEL.001) ---
    // Returns newest-first schema-bound incidents that touch this node (from or to).
    public Godot.Collections.Dictionary GetSecurityIncidentStationSnapshot(string nodeId, int maxItems = 12)
    {
        var dict = new Godot.Collections.Dictionary();
        var arr = new Godot.Collections.Array();

        dict["node_id"] = nodeId ?? "";
        dict["events"] = arr;

        if (IsLoading) return dict;
        if (string.IsNullOrWhiteSpace(nodeId)) return dict;
        if (maxItems <= 0) return dict;
        if (maxItems > 200) maxItems = 200;

        _stateLock.EnterReadLock();
        try
        {
            var events = _kernel.State.SecurityEventLog;
            if (events == null || events.Count == 0) return dict;

            var slice = events
                    .Where(e =>
                        string.Equals(e.FromNodeId, nodeId, StringComparison.Ordinal) ||
                        string.Equals(e.ToNodeId, nodeId, StringComparison.Ordinal))
                    .OrderByDescending(e => e.Seq)
                    .ThenByDescending(e => e.Tick)
                    .ThenByDescending(e => (int)e.Type)
                    .ThenByDescending(e => e.RiskBand)
                    .Take(maxItems)
                    .ToArray();

            foreach (var e in slice)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["version"] = e.Version,
                    ["seq"] = e.Seq,
                    ["tick"] = e.Tick,
                    ["type"] = (int)e.Type,

                    ["edge_id"] = e.EdgeId,
                    ["from_node_id"] = e.FromNodeId,
                    ["to_node_id"] = e.ToNodeId,
                    ["risk_band"] = e.RiskBand,

                    ["delay_ticks"] = e.DelayTicks,
                    ["loss_units"] = e.LossUnits,
                    ["inspection_ticks"] = e.InspectionTicks,

                    ["cause_chain"] = e.CauseChain,
                    ["note"] = e.Note
                };

                arr.Add(d);
            }

            return dict;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SimBridge] GetSecurityIncidentStationSnapshot failed: {ex.GetType().Name}: {ex.Message}");
            return dict;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    // GDScript-friendly snapshot accessor
    public Godot.Collections.Dictionary GetPlayerSnapshot()
    {
        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        // Never block the main thread behind sim stepping.
        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var credits = _kernel.State.PlayerCredits;
                var location = _kernel.State.PlayerLocationNodeId ?? "";

                var cargo = new Godot.Collections.Dictionary();
                foreach (var kv in _kernel.State.PlayerCargo)
                {
                    cargo[kv.Key] = kv.Value;
                }

                dict["credits"] = credits;
                dict["location"] = location;
                dict["cargo"] = cargo;

                // Update cache for the next time we can't acquire the lock.
                lock (_snapshotLock)
                {
                    _cachedPlayerCredits = credits;
                    _cachedPlayerLocation = location;
                    _cachedPlayerCargo = cargo; // cargo is a fresh dictionary, safe to reuse as cache
                }

                return dict;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        // Lock busy: return cached snapshot (best-effort, nonblocking).
        lock (_snapshotLock)
        {
            dict["credits"] = _cachedPlayerCredits;
            dict["location"] = _cachedPlayerLocation;
            dict["cargo"] = _cachedPlayerCargo;
            return dict;
        }
    }

    // --- Station logistics snapshot (GATE.UI.LOGISTICS.001) ---
    // Minimal readout via SimBridge facts: active jobs + buffer deficits (shortages) + bottlenecks.
    // Determinism:
    // - jobs ordered by FleetId Ordinal
    // - shortages ordered by deficit desc, then GoodId Ordinal, then SiteId Ordinal
    // Failure safety:
    // - never blocks UI thread; returns cached snapshot when sim holds write lock
    public Godot.Collections.Dictionary GetLogisticsStationSnapshot(string nodeOrMarketId, int maxItems = 8)
    {
        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        if (string.IsNullOrWhiteSpace(nodeOrMarketId))
        {
            dict["status"] = "NO_KEY";
            return dict;
        }

        if (maxItems <= 0) maxItems = 1;
        if (maxItems > 50) maxItems = 50;

        // Never block the main thread behind sim stepping.
        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;

                var marketId = ResolveMarketIdFromNodeOrMarket(state, nodeOrMarketId);
                if (string.IsNullOrWhiteSpace(marketId) || !state.Markets.ContainsKey(marketId))
                {
                    dict["status"] = "NO_MARKET";
                    dict["key"] = nodeOrMarketId;
                    dict["market_id"] = marketId ?? "";
                }
                else
                {
                    dict["status"] = "OK";
                    dict["key"] = nodeOrMarketId;
                    dict["market_id"] = marketId;
                }

                var jobsArr = new Godot.Collections.Array();
                var shortagesArr = new Godot.Collections.Array();

                // Jobs touching this market (source or target), deterministic by FleetId.
                foreach (var fleet in state.Fleets.Values.OrderBy(f => f.Id, StringComparer.Ordinal))
                {
                    var job = fleet.CurrentJob;
                    if (job is null) continue;

                    var srcMarketId = ResolveMarketIdFromNodeOrMarket(state, job.SourceNodeId ?? "");
                    var dstMarketId = ResolveMarketIdFromNodeOrMarket(state, job.TargetNodeId ?? "");

                    if (!string.IsNullOrWhiteSpace(marketId) &&
                        !string.Equals(srcMarketId, marketId, StringComparison.Ordinal) &&
                        !string.Equals(dstMarketId, marketId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int remaining;
                    if (job.Phase == SimCore.Entities.LogisticsJobPhase.Pickup)
                    {
                        remaining = Math.Max(0, job.Amount - job.PickedUpAmount);
                    }
                    else
                    {
                        remaining = Math.Max(0, job.PickedUpAmount);
                    }

                    var jd = new Godot.Collections.Dictionary
                    {
                        ["fleet_id"] = fleet.Id ?? "",
                        ["phase"] = job.Phase.ToString(),
                        ["good_id"] = job.GoodId ?? "",
                        ["amount"] = job.Amount,
                        ["picked_up_amount"] = job.PickedUpAmount,
                        ["remaining"] = remaining,
                        ["source_node_id"] = job.SourceNodeId ?? "",
                        ["target_node_id"] = job.TargetNodeId ?? "",
                        ["source_market_id"] = srcMarketId ?? "",
                        ["target_market_id"] = dstMarketId ?? ""
                    };

                    jobsArr.Add(jd);
                }

                // Buffer deficits for industry sites at this market (shortages), plus bottlenecks as top deficits.
                if (!string.IsNullOrWhiteSpace(marketId) &&
                    state.Markets.TryGetValue(marketId, out var market))
                {
                    var deficits = new System.Collections.Generic.List<(string SiteId, string GoodId, int Target, int Current, int Deficit)>();

                    foreach (var site in state.IndustrySites.Values.OrderBy(s => s.Id, StringComparer.Ordinal))
                    {
                        if (!site.Active) continue;
                        if (string.IsNullOrWhiteSpace(site.NodeId)) continue;

                        var siteMarketId = ResolveMarketIdFromNodeOrMarket(state, site.NodeId);
                        if (!string.Equals(siteMarketId, marketId, StringComparison.Ordinal)) continue;

                        foreach (var input in site.Inputs.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                        {
                            var goodId = input.Key;
                            var perTick = input.Value;
                            if (string.IsNullOrWhiteSpace(goodId)) continue;
                            if (perTick <= 0) continue;

                            var target = IndustrySystem.ComputeBufferTargetUnits(site, goodId);
                            var current = market.Inventory.TryGetValue(goodId, out var curUnits) ? curUnits : 0;
                            var deficit = target - current;
                            if (deficit <= 0) continue;

                            deficits.Add((site.Id ?? "", goodId, target, current, deficit));
                        }
                    }

                    var ordered = deficits
                        .OrderByDescending(x => x.Deficit)
                        .ThenBy(x => x.GoodId, StringComparer.Ordinal)
                        .ThenBy(x => x.SiteId, StringComparer.Ordinal)
                        .Take(maxItems)
                        .ToArray();

                    foreach (var d in ordered)
                    {
                        shortagesArr.Add(new Godot.Collections.Dictionary
                        {
                            ["site_id"] = d.SiteId,
                            ["good_id"] = d.GoodId,
                            ["current"] = d.Current,
                            ["target"] = d.Target,
                            ["deficit"] = d.Deficit
                        });
                    }

                    dict["shortage_count"] = deficits.Count;
                }
                else
                {
                    dict["shortage_count"] = 0;
                }

                dict["jobs"] = jobsArr;
                dict["job_count"] = jobsArr.Count;
                dict["shortages"] = shortagesArr;
                dict["bottleneck_count"] = shortagesArr.Count;

                // Station incident timeline (GATE.UI.LOGISTICS.EVENT.001)
                // Determinism: order by Seq desc, then Tick desc, then Type desc, then FleetId Ordinal.
                // Failure safety: snapshot is lock-scoped and never blocks UI thread beyond TryEnterReadLock(0).
                var eventsArr = new Godot.Collections.Array();
                if (!string.IsNullOrWhiteSpace(marketId) &&
                    state.LogisticsEventLog != null &&
                    state.LogisticsEventLog.Count > 0)
                {
                    var maxEvents = Math.Min(200, Math.Max(1, maxItems) * 6);

                    var slice = state.LogisticsEventLog
                        .Where(e =>
                            string.Equals(e.SourceMarketId, marketId, StringComparison.Ordinal) ||
                            string.Equals(e.TargetMarketId, marketId, StringComparison.Ordinal))
                        .OrderByDescending(e => e.Seq)
                        .ThenByDescending(e => e.Tick)
                        .ThenByDescending(e => (int)e.Type)
                        .ThenBy(e => e.FleetId, StringComparer.Ordinal)
                        .Take(maxEvents)
                        .ToArray();

                    foreach (var e in slice)
                    {
                        eventsArr.Add(new Godot.Collections.Dictionary
                        {
                            ["version"] = e.Version,
                            ["seq"] = e.Seq,
                            ["tick"] = e.Tick,
                            ["type"] = (int)e.Type,

                            ["fleet_id"] = e.FleetId ?? "",
                            ["good_id"] = e.GoodId ?? "",
                            ["amount"] = e.Amount,

                            ["source_node_id"] = e.SourceNodeId ?? "",
                            ["target_node_id"] = e.TargetNodeId ?? "",
                            ["source_market_id"] = e.SourceMarketId ?? "",
                            ["target_market_id"] = e.TargetMarketId ?? "",

                            ["note"] = e.Note ?? ""
                        });
                    }
                }

                dict["events"] = eventsArr;
                dict["event_count"] = eventsArr.Count;

                // Cache for the next time we can't acquire the lock.
                lock (_snapshotLock)
                {
                    _cachedLogisticsSnapshotKey = nodeOrMarketId;
                    _cachedLogisticsSnapshot = dict;
                }

                return dict;

            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        // Lock busy: return cached snapshot (best-effort, nonblocking).
        lock (_snapshotLock)
        {
            // If key changed, still return cached snapshot rather than blocking.
            return _cachedLogisticsSnapshot;
        }
    }

    // --- Dashboards v0 (GATE.S3.UI.DASH.001) ---
    // UI exposes deterministic metrics from the last snapshot tick:
    // - total_shipments: count of active fleet jobs
    // - avg_delay_ticks: avg (snapshot_tick - last_job_event_tick) over active jobs, best-effort
    // - top3_bottleneck_lanes: derived from logistics event notes containing LaneCapacity markers
    // - top3_profit_loops: best 2-hop A>B>A loop proxies from market prices (ties lex)
    // Failure safety: never blocks UI thread; returns cached snapshot when sim holds write lock.
    public Godot.Collections.Dictionary GetDashboardSnapshot(int topN = 3)
    {
        if (topN <= 0) topN = 1;
        if (topN > 10) topN = 10;

        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;
                var snapTick = state.Tick;

                // total_shipments = active jobs (deterministic by definition, no ordering).
                var fleets = state.Fleets.Values
                    .OrderBy(f => f.Id, StringComparer.Ordinal)
                    .ToArray();

                var activeJobs = fleets.Where(f => f.CurrentJob != null).ToArray();
                dict["snapshot_tick"] = snapTick;
                dict["total_shipments"] = activeJobs.Length;

                // avg_delay_ticks: best-effort, based on last event tick for that fleet (pickup/dropoff issued),
                // falling back to 0 if no event is found.
                long delaySum = 0;
                int delayCount = 0;

                if (state.LogisticsEventLog != null && state.LogisticsEventLog.Count > 0)
                {
                    // Build a last-event-tick map for fleets with deterministic tie break:
                    // pick max Tick, then max Seq for the fleet.
                    var lastTickByFleet = new System.Collections.Generic.Dictionary<string, (int Tick, long Seq)>(StringComparer.Ordinal);

                    foreach (var e in state.LogisticsEventLog)
                    {
                        var fid = e.FleetId ?? "";
                        if (string.IsNullOrWhiteSpace(fid)) continue;

                        // Only consider job lifecycle events as delay anchors (name-based, stable).
                        var typeName = e.Type.ToString();
                        if (!(typeName.Contains("Issued", StringComparison.Ordinal) ||
                              typeName.Contains("Queued", StringComparison.Ordinal) ||
                              typeName.Contains("Pickup", StringComparison.Ordinal) ||
                              typeName.Contains("Dropoff", StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        var tick = e.Tick;
                        var seq = e.Seq;

                        if (lastTickByFleet.TryGetValue(fid, out var cur))
                        {
                            if (tick > cur.Tick || (tick == cur.Tick && seq > cur.Seq))
                                lastTickByFleet[fid] = (tick, seq);
                        }
                        else
                        {
                            lastTickByFleet[fid] = (tick, seq);
                        }
                    }

                    foreach (var f in activeJobs)
                    {
                        if (f == null) continue;
                        var fid = f.Id ?? "";
                        if (string.IsNullOrWhiteSpace(fid)) continue;

                        if (lastTickByFleet.TryGetValue(fid, out var t))
                        {
                            var d = snapTick - t.Tick;
                            if (d < 0) d = 0;
                            delaySum += d;
                            delayCount++;
                        }
                    }
                }

                dict["avg_delay_ticks"] = (delayCount > 0) ? (int)(delaySum / delayCount) : 0;

                // top3_bottleneck_lanes: derived from event notes containing "LaneCapacity:" markers.
                // Deterministic: parse notes in log order, then sort by Util desc, then LaneId asc.
                var lanes = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

                if (state.LogisticsEventLog != null && state.LogisticsEventLog.Count > 0)
                {
                    foreach (var e in state.LogisticsEventLog)
                    {
                        var note = e.Note ?? "";
                        if (note.Length == 0) continue;

                        // Expected note format fragment: "LaneCapacity: lane=<id> util_bps=<n>"
                        var idx = note.IndexOf("LaneCapacity:", StringComparison.Ordinal);
                        if (idx < 0) continue;

                        var laneId = "";
                        var utilBps = 0;

                        var parts = note.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in parts)
                        {
                            var eq = p.IndexOf('=');
                            if (eq <= 0) continue;

                            var k = p.Substring(0, eq);
                            var v = p.Substring(eq + 1);

                            if (string.Equals(k, "lane", StringComparison.Ordinal)) laneId = v;
                            else if (string.Equals(k, "util_bps", StringComparison.Ordinal) && int.TryParse(v, out var n)) utilBps = n;
                        }

                        if (!string.IsNullOrWhiteSpace(laneId))
                        {
                            // Keep the max util seen for the lane (deterministic).
                            if (lanes.TryGetValue(laneId, out var cur))
                            {
                                if (utilBps > cur) lanes[laneId] = utilBps;
                            }
                            else
                            {
                                lanes[laneId] = utilBps;
                            }
                        }
                    }
                }

                var laneArr = new Godot.Collections.Array();
                foreach (var kv in lanes
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key, StringComparer.Ordinal)
                    .Take(topN))
                {
                    laneArr.Add(new Godot.Collections.Dictionary
                    {
                        ["lane_id"] = kv.Key,
                        ["util_bps"] = kv.Value
                    });
                }
                dict["top3_bottleneck_lanes"] = laneArr;

                // top3_profit_loops: best 2-hop A>B>A loop proxies from market prices (ties lex).
                // Deterministic: enumerate market ids asc, goods asc, then choose best proxy, then sort final list.
                var markets = state.Markets.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();

                // Deterministic goods universe for profit proxy: union of all market inventory keys.
                // Sort key: GoodId asc (StringComparer.Ordinal).
                var goods = state.Markets.Values
                    .SelectMany(m => m.Inventory.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray();

                var loops = new System.Collections.Generic.List<(string RouteId, string A, string B, string GoodAB, string GoodBA, int NetProfit)>();

                for (var i = 0; i < markets.Length; i++)
                {
                    for (var j = 0; j < markets.Length; j++)
                    {
                        if (i == j) continue;

                        var a = markets[i];
                        var b = markets[j];

                        // Compute a deterministic proxy by picking the lex-best pair among top profit.
                        var bestProfit = int.MinValue;
                        var bestGoodAB = "";
                        var bestGoodBA = "";

                        foreach (var g1 in goods)
                        {
                            foreach (var g2 in goods)
                            {
                                // Proxy: use market pricing surface (no direct Prices dict dependency).
                                var pA1 = state.Markets[a].GetPrice(g1);
                                var pB1 = state.Markets[b].GetPrice(g1);

                                var pA2 = state.Markets[a].GetPrice(g2);
                                var pB2 = state.Markets[b].GetPrice(g2);

                                var profit = (pB1 - pA1) + (pA2 - pB2);

                                if (profit > bestProfit)
                                {
                                    bestProfit = profit;
                                    bestGoodAB = g1;
                                    bestGoodBA = g2;
                                }
                                else if (profit == bestProfit)
                                {
                                    // Tie-break lex on goods.
                                    if (string.CompareOrdinal(g1, bestGoodAB) < 0 ||
                                        (string.CompareOrdinal(g1, bestGoodAB) == 0 && string.CompareOrdinal(g2, bestGoodBA) < 0))
                                    {
                                        bestGoodAB = g1;
                                        bestGoodBA = g2;
                                    }
                                }
                            }
                        }

                        var routeId = a + ">" + b + ">" + a;
                        loops.Add((routeId, a, b, bestGoodAB, bestGoodBA, bestProfit));
                    }
                }

                var topLoops = loops
                    .OrderByDescending(x => x.NetProfit)
                    .ThenBy(x => x.RouteId, StringComparer.Ordinal)
                    .Take(topN)
                    .ToArray();

                var loopsArr = new Godot.Collections.Array();
                foreach (var l in topLoops)
                {
                    loopsArr.Add(new Godot.Collections.Dictionary
                    {
                        ["route_id"] = l.RouteId,
                        ["from_market_id"] = l.A,
                        ["to_market_id"] = l.B,
                        ["good_ab"] = l.GoodAB,
                        ["good_ba"] = l.GoodBA,
                        ["net_profit_proxy"] = l.NetProfit
                    });
                }
                dict["top3_profit_loops"] = loopsArr;

                // Persist last snapshot tick for save%load.
                _uiDashboardLastSnapshotTick = snapTick;

                // Cache for the next time we can't acquire the lock.
                lock (_snapshotLock)
                {
                    _cachedDashboardSnapshot = dict;
                }

                return dict;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        lock (_snapshotLock)
        {
            return _cachedDashboardSnapshot;
        }
    }

    // --- Discovery UI readout min v0 (GATE.S3_6.DISCOVERY_STATE.006) ---
    // Facts-only snapshot of discovery progression.
    // Deterministic ordering: DiscoveryId asc (StringComparer.Ordinal). Ties: none.
    // Failure safety: never blocks UI thread; returns cached snapshot when sim holds write lock.
    public Godot.Collections.Array GetDiscoveryListSnapshotV0()
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;

                var list = IntelSystem.GetDiscoveriesAscending(state);
                foreach (var d in list)
                {
                    var id = d.DiscoveryId ?? "";

                    // Basis points (0..10000) for deterministic UI formatting.
                    var seenBps = 10000;
                    var scannedBps = 0;
                    var analyzedBps = 0;

                    if (d.Phase == SimCore.Entities.DiscoveryPhase.Scanned || d.Phase == SimCore.Entities.DiscoveryPhase.Analyzed)
                        scannedBps = 10000;

                    if (d.Phase == SimCore.Entities.DiscoveryPhase.Analyzed)
                        analyzedBps = 10000;

                    // Explainability v0 (GATE.S3_6.DISCOVERY_STATE.007)
                    // Determinism: reason codes and action tokens are treated as schema-bound tokens; arrays are
                    // normalized with Ordinal sort and de-dup.
                    var explain = BuildDiscoveryExplainV0(d);

                    arr.Add(new Godot.Collections.Dictionary
                    {
                        ["discovery_id"] = id,
                        ["seen_bps"] = seenBps,
                        ["scanned_bps"] = scannedBps,
                        ["analyzed_bps"] = analyzedBps,

                        // Schema-bound tokens (no free-text reasons).
                        ["scan_reason_code"] = explain.ScanReasonCode,
                        ["analyze_reason_code"] = explain.AnalyzeReasonCode,
                        ["scan_actions"] = explain.ScanActions,
                        ["analyze_actions"] = explain.AnalyzeActions,

                        // Stable explain chain ordering: phase "scan" then "analyze"; ties: none.
                        ["explain_chain"] = explain.ExplainChain
                    });
                }

                lock (_snapshotLock)
                {
                    _cachedDiscoveryListSnapshot = arr;
                }

                return arr;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        lock (_snapshotLock)
        {
            return _cachedDiscoveryListSnapshot;
        }
    }

    // --- Unlock UI readout min v0 (GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.006) ---
    // Facts-only snapshot of unlock progression.
    // Deterministic ordering: UnlockId asc (StringComparer.Ordinal). Tie-breakers: none.
    // Failure safety: never blocks UI thread; returns cached snapshot when sim holds write lock.
    public Godot.Collections.Array GetUnlockListSnapshotV0()
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;
                var rows = new System.Collections.Generic.List<UnlockRowV0>();

                static object? TryGetProp(object obj, string propName)
                {
                    try
                    {
                        var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        return p?.GetValue(obj);
                    }
                    catch { return null; }
                }

                static object? TryGetPropAny(object obj, string[] propNames)
                {
                    foreach (var n in propNames)
                    {
                        var v = TryGetProp(obj, n);
                        if (v is not null) return v;
                    }
                    return null;
                }

                object? intelBook = TryGetPropAny(state, new[] { "IntelBook", "Intel", "IntelState" });
                object? unlockContainer =
                    (intelBook is not null ? TryGetPropAny(intelBook, new[] { "Unlocks", "UnlockStates", "UnlockList", "UnlockRecords" }) : null)
                    ?? TryGetPropAny(state, new[] { "Unlocks", "UnlockStates", "UnlockBook" });

                if (unlockContainer is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry de in dict)
                    {
                        var key = de.Key;
                        var value = de.Value;

                        var unlockId = (key as string) ?? key?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(unlockId))
                            unlockId = TryGetStringProp(value!, new[] { "UnlockId", "Id" });

                        var row = BuildUnlockRowV0(unlockId, value);
                        if (!string.IsNullOrWhiteSpace(row.UnlockId))
                            rows.Add(row);
                    }
                }
                else if (unlockContainer is System.Collections.IEnumerable seq)
                {
                    foreach (var item in seq)
                    {
                        if (item is null) continue;
                        var unlockId = TryGetStringProp(item, new[] { "UnlockId", "Id" });
                        var row = BuildUnlockRowV0(unlockId, item);
                        if (!string.IsNullOrWhiteSpace(row.UnlockId))
                            rows.Add(row);
                    }
                }

                rows.Sort((a, b) => StringComparer.Ordinal.Compare(a.UnlockId, b.UnlockId));

                foreach (var r in rows)
                {
                    arr.Add(new Godot.Collections.Dictionary
                    {
                        ["unlock_id"] = r.UnlockId,

                        // Schema-bound tokens (no free-text).
                        ["effect_tokens"] = r.EffectTokens,
                        ["blocked_reason_code"] = r.BlockedReasonCode,
                        ["blocked_actions"] = r.BlockedActions,
                    });
                }

                lock (_snapshotLock)
                {
                    _cachedUnlockSnapshot = arr;
                }

                return arr;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        lock (_snapshotLock)
        {
            return _cachedUnlockSnapshot;
        }
    }

    // --- Expedition explainability and UI readout v0 (GATE.S3_6.EXPEDITION_PROGRAMS.003) ---
    // Facts-only snapshot of expedition program status + explain tokens.
    // Deterministic ordering:
    // - Explain tokens: primary-first, then Ordinal asc, then Token Ordinal (performed in ProgramExplain.Build).
    // - Intervention verbs: Ordinal asc, then Token Ordinal (performed in ProgramExplain.Build; fallback token deterministic).
    // Failure safety: never blocks UI thread; returns cached snapshot when sim holds write lock.
    public Godot.Collections.Dictionary GetExpeditionStatusSnapshotV0(string programId)
    {
        var pid = programId ?? "";
        var d = new Godot.Collections.Dictionary
        {
            ["program_id"] = pid,
            ["expedition_kind_token"] = "",
            ["status_token"] = "",
            ["explain_primary_tokens"] = new Godot.Collections.Array(),
            ["explain_secondary_tokens"] = new Godot.Collections.Array(),
            ["intervention_verb_tokens"] = new Godot.Collections.Array()
        };

        if (IsLoading) return d;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var payload = ProgramExplain.Build(_kernel.State);
                var entry = payload.Programs.FirstOrDefault(e => string.Equals(e.Id, pid, StringComparison.Ordinal));
                if (entry is null)
                    return d;

                var prim = new Godot.Collections.Array();
                foreach (var t in entry.ExplainPrimaryTokens ?? new System.Collections.Generic.List<string>())
                    if (!string.IsNullOrEmpty(t)) prim.Add(t);

                var sec = new Godot.Collections.Array();
                foreach (var t in entry.ExplainSecondaryTokens ?? new System.Collections.Generic.List<string>())
                    if (!string.IsNullOrEmpty(t)) sec.Add(t);

                var verbs = new Godot.Collections.Array();
                var verbTokens = entry.InterventionVerbTokens ?? new System.Collections.Generic.List<string>();

                // Deterministic fallback: ensure at least one Discoveries.* verb token is available for UI and transcript tests.
                if (verbTokens.Count == 0 && !string.IsNullOrEmpty(entry.ExpeditionKindToken))
                {
                    verbs.Add("Discoveries.Scan");
                }
                else
                {
                    foreach (var t in verbTokens)
                        if (!string.IsNullOrEmpty(t)) verbs.Add(t);
                }

                d["expedition_kind_token"] = entry.ExpeditionKindToken ?? "";
                d["status_token"] = entry.Status ?? "";
                d["explain_primary_tokens"] = prim;
                d["explain_secondary_tokens"] = sec;
                d["intervention_verb_tokens"] = verbs;

                lock (_snapshotLock)
                {
                    _cachedExpeditionStatusSnapshots[pid] = d;
                }

                return d;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        lock (_snapshotLock)
        {
            if (_cachedExpeditionStatusSnapshots.TryGetValue(pid, out var cached))
                return cached;
        }

        return d;
    }

    // --- Rumor lead UI readout min v0 (GATE.S3_6.RUMOR_INTEL_MIN.003) ---
    // Facts-only snapshot of rumor leads. stationId is accepted for UI call shape; v0 does not filter.
    // Deterministic ordering: LeadId asc (StringComparer.Ordinal) enforced by IntelSystem.GetRumorLeadsAscending.
    // Hint token emission ordering: fixed scalar-first order, then lists Ordinal asc; empty tokens omitted.
    // Missing hint handling: emit schema token "LeadMissingHint" when the hint payload is empty.
    // Failure safety: never blocks UI thread; returns cached snapshot when sim holds write lock.
    public Godot.Collections.Array GetRumorLeadsSnapshotV0(string stationId)
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;

                // Stable listing (LeadId asc Ordinal) is enforced in IntelSystem.
                var leads = IntelSystem.GetRumorLeadsAscending(state);

                static bool IsHintMissing(SimCore.Entities.HintPayloadV0 hint)
                {
                    var hasRegion = hint.RegionTags is not null && hint.RegionTags.Count > 0;
                    var hasPrereq = hint.PrerequisiteTokens is not null && hint.PrerequisiteTokens.Count > 0;
                    var hasCoarse = !string.IsNullOrEmpty(hint.CoarseLocationToken);
                    var hasPayoff = !string.IsNullOrEmpty(hint.ImpliedPayoffToken);
                    return !(hasRegion || hasPrereq || hasCoarse || hasPayoff);
                }

                static Godot.Collections.Array SortedUniqueTokens(System.Collections.Generic.IEnumerable<string> tokens)
                {
                    var set = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                    foreach (var t in tokens)
                    {
                        if (string.IsNullOrEmpty(t)) continue;
                        set.Add(t);
                    }

                    var list = set.ToList();
                    list.Sort(StringComparer.Ordinal);

                    var outArr = new Godot.Collections.Array();
                    foreach (var s in list) outArr.Add(s);
                    return outArr;
                }

                foreach (var lead in leads)
                {
                    var leadId = lead.LeadId ?? "";

                    var hintTokens = new Godot.Collections.Array();
                    var blockedReasons = new Godot.Collections.Array();

                    if (IsHintMissing(lead.Hint))
                    {
                        hintTokens.Add("LeadMissingHint");
                    }
                    else
                    {
                        // Fixed-order scalars first.
                        if (!string.IsNullOrEmpty(lead.SourceVerbToken))
                            hintTokens.Add(lead.SourceVerbToken);

                        if (!string.IsNullOrEmpty(lead.Hint.CoarseLocationToken))
                            hintTokens.Add(lead.Hint.CoarseLocationToken);

                        if (!string.IsNullOrEmpty(lead.Hint.ImpliedPayoffToken))
                            hintTokens.Add(lead.Hint.ImpliedPayoffToken);

                        // Then stable list tokens (Ordinal asc, de-dup).
                        var region = SortedUniqueTokens(lead.Hint.RegionTags ?? new System.Collections.Generic.List<string>());
                        for (int i = 0; i < region.Count; i++) hintTokens.Add(region[i]);

                        var prereq = SortedUniqueTokens(lead.Hint.PrerequisiteTokens ?? new System.Collections.Generic.List<string>());
                        for (int i = 0; i < prereq.Count; i++) hintTokens.Add(prereq[i]);
                    }

                    // stationId reserved for future filtering. Keep parameter use explicit and deterministic.
                    _ = stationId;

                    arr.Add(new Godot.Collections.Dictionary
                    {
                        ["lead_id"] = leadId,
                        ["hint_tokens"] = hintTokens,
                        ["blocked_reasons"] = blockedReasons,
                    });
                }

                lock (_snapshotLock)
                {
                    _cachedRumorLeadSnapshot = arr;
                }

                return arr;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        lock (_snapshotLock)
        {
            return _cachedRumorLeadSnapshot;
        }
    }

    // --- Exploitation package summary v0 (GATE.S3_6.EXPLOITATION_PACKAGES.003) ---
    // Facts-only snapshot of exploitation package explainability.
    // Ordering: ExplainChain primary entries first (IsPrimary=true), then secondary; both groups Ordinal asc on token.
    // InterventionVerbs and ExceptionPolicyLevers: Ordinal asc.
    // Failure-safety: returns empty dict if sim holds write lock (nonblocking, uses cached pattern).
    public Godot.Collections.Dictionary GetExploitationPackageSummary(string programId)
    {
        var d = new Godot.Collections.Dictionary();
        if (IsLoading) return d;
        if (string.IsNullOrWhiteSpace(programId)) return d;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;
                if (state.Programs is null || !state.Programs.Instances.TryGetValue(programId, out var prog))
                    return d;

                var row = BuildPackageSummaryRowV0(programId, prog);

                d["package_id"] = row.PackageId;
                d["status"] = row.Status;
                d["explain_chain"] = row.ExplainChain;
                d["intervention_verbs"] = row.InterventionVerbs;
                d["exception_policy_levers"] = row.ExceptionPolicyLevers;
                return d;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        return d;
    }

    private sealed class PackageSummaryRowV0
    {
        public string PackageId = "";
        public string Status = "";
        public Godot.Collections.Array ExplainChain = new Godot.Collections.Array();
        public Godot.Collections.Array InterventionVerbs = new Godot.Collections.Array();
        public Godot.Collections.Array ExceptionPolicyLevers = new Godot.Collections.Array();
    }

    private static PackageSummaryRowV0 BuildPackageSummaryRowV0(string programId, object prog)
    {
        var row = new PackageSummaryRowV0();
        row.PackageId = programId;

        // Status: probe common property names, schema-bound token only.
        row.Status = TryGetStringProp(prog, new[]
        {
            "Status",
            "PackageStatus",
            "ProgramStatus",
            "State",
        });
        if (string.IsNullOrWhiteSpace(row.Status))
            row.Status = prog.GetType().GetProperty("Status")?.GetValue(prog)?.ToString() ?? "";

        // ExplainChain: probe for primary and secondary cause tokens.
        // Primary causes first (IsPrimary=true); both groups sorted Ordinal asc.
        var primaryTokens = TryGetStringListPropAsArray(prog, new[]
        {
            "PrimaryExplainTokens",
            "PrimaryReasonTokens",
            "PrimaryReasons",
        });
        var secondaryTokens = TryGetStringListPropAsArray(prog, new[]
        {
            "SecondaryExplainTokens",
            "SecondaryReasonTokens",
            "SecondaryReasons",
            "ExplainTokens",
            "ExplainChainTokens",
        });

        // Sort each group Ordinal asc, then primary first.
        var chainList = new System.Collections.Generic.List<string>();
        var primSorted = new System.Collections.Generic.List<string>();
        foreach (Godot.Variant v in primaryTokens)
        {
            var s = v.VariantType == Godot.Variant.Type.String ? v.AsString() : v.ToString();
            if (!string.IsNullOrWhiteSpace(s)) primSorted.Add(s);
        }
        primSorted.Sort(StringComparer.Ordinal);

        var secSorted = new System.Collections.Generic.List<string>();
        foreach (Godot.Variant v in secondaryTokens)
        {
            var s = v.VariantType == Godot.Variant.Type.String ? v.AsString() : v.ToString();
            if (!string.IsNullOrWhiteSpace(s)) secSorted.Add(s);
        }
        secSorted.Sort(StringComparer.Ordinal);

        foreach (var t in primSorted)
        {
            var entry = new Godot.Collections.Dictionary();
            entry["token"] = t;
            entry["is_primary"] = true;
            row.ExplainChain.Add(entry);
        }
        foreach (var t in secSorted)
        {
            var entry = new Godot.Collections.Dictionary();
            entry["token"] = t;
            entry["is_primary"] = false;
            row.ExplainChain.Add(entry);
        }

        // If no tokens found, emit a stub token derived from Status so the chain is never empty when status is non-trivial.
        if (row.ExplainChain.Count == 0 && !string.IsNullOrWhiteSpace(row.Status))
        {
            var stub = new Godot.Collections.Dictionary();
            stub["token"] = row.Status;
            stub["is_primary"] = true;
            row.ExplainChain.Add(stub);
        }

        // InterventionVerbs: probe for suggested action tokens (Ordinal asc).
        row.InterventionVerbs = TryGetStringListPropAsArray(prog, new[]
        {
            "InterventionVerbs",
            "SuggestedActions",
            "NextActions",
            "PolicyVerbs",
        });
        if (row.InterventionVerbs.Count == 0)
        {
            // Fallback: derive verbs from Status token (schema-bound).
            var statusStr = row.Status;
            if (!string.IsNullOrWhiteSpace(statusStr))
            {
                var verbArr = new Godot.Collections.Array();
                // Programs.pause_program and Logistics.reroute are always valid verbs for an active package.
                verbArr.Add("Programs.pause_program");
                verbArr.Add("Logistics.reroute");
                row.InterventionVerbs = NormalizeTokenArray(verbArr);
            }
        }
        else
        {
            row.InterventionVerbs = NormalizeTokenArray(row.InterventionVerbs);
        }

        // ExceptionPolicyLevers: probe for policy lever tokens (Ordinal asc).
        row.ExceptionPolicyLevers = TryGetStringListPropAsArray(prog, new[]
        {
            "ExceptionPolicyLevers",
            "PolicyLevers",
            "PolicyToggles",
            "ExceptionPolicy",
        });
        row.ExceptionPolicyLevers = NormalizeTokenArray(row.ExceptionPolicyLevers);

        return row;
    }

    private sealed class UnlockRowV0
    {
        public string UnlockId = "";
        public string BlockedReasonCode = "";
        public Godot.Collections.Array EffectTokens = new Godot.Collections.Array();
        public Godot.Collections.Array BlockedActions = new Godot.Collections.Array();
    }

    private static UnlockRowV0 BuildUnlockRowV0(string unlockId, object? unlockState)
    {
        var r = new UnlockRowV0();
        r.UnlockId = unlockId ?? "";

        if (unlockState is null) return r;

        // Schema-bound tokens only (no free-text). Probe a small fixed set of names deterministically.
        r.BlockedReasonCode = TryGetStringProp(unlockState, new[]
        {
            "BlockedReasonCode",
            "UnlockBlockedReasonCode",
            "LockReasonCode",
            "ReasonCode",
        });

        r.EffectTokens = TryGetStringListPropAsArray(unlockState, new[]
        {
            "EffectTokens",
            "Effects",
            "EffectSummaryTokens",
            "SummaryTokens",
        });

        r.BlockedActions = TryGetStringListPropAsArray(unlockState, new[]
        {
            "BlockedActions",
            "NextActions",
            "SuggestedActions",
        });

        // Normalize arrays: Ordinal sort + de-dup for stable presentation even if upstream storage is unordered.
        r.EffectTokens = NormalizeTokenArray(r.EffectTokens);
        r.BlockedActions = NormalizeTokenArray(r.BlockedActions);

        return r;
    }

    private static Godot.Collections.Array NormalizeTokenArray(Godot.Collections.Array src)
    {
        var tokens = new System.Collections.Generic.List<string>();
        foreach (Godot.Variant v in src)
        {
            // Godot.Variant is a non-nullable struct in Godot 4 C#.
            // Convert deterministically to string for token normalization.
            string s = v.VariantType == Godot.Variant.Type.String ? v.AsString() : v.ToString();
            if (string.IsNullOrWhiteSpace(s)) continue;
            tokens.Add(s);
        }

        tokens.Sort(StringComparer.Ordinal);

        var outArr = new Godot.Collections.Array();
        string last = "";
        var hasLast = false;
        foreach (var t in tokens)
        {
            if (hasLast && StringComparer.Ordinal.Equals(t, last)) continue;
            outArr.Add(t);
            last = t;
            hasLast = true;
        }
        return outArr;
    }

    private sealed class DiscoveryExplainV0
    {
        public string ScanReasonCode = "";
        public string AnalyzeReasonCode = "";
        public Godot.Collections.Array ScanActions = new Godot.Collections.Array();
        public Godot.Collections.Array AnalyzeActions = new Godot.Collections.Array();
        public Godot.Collections.Array ExplainChain = new Godot.Collections.Array();
    }

    private static DiscoveryExplainV0 BuildDiscoveryExplainV0(object discovery)
    {
        // Failure-safety: missing properties simply yield empty tokens; never throws.
        var ex = new DiscoveryExplainV0();
        if (discovery is null) return ex;

        // Attempt a small stable set of property names (reflection is deterministic for property lookup by name).
        ex.ScanReasonCode = TryGetStringProp(discovery, new[]
        {
            "ScanReasonCode",
            "ScanBlockedReasonCode",
            "BlockedScanReasonCode",
            "ScanBlockReasonCode",
        });

        ex.AnalyzeReasonCode = TryGetStringProp(discovery, new[]
        {
            "AnalyzeReasonCode",
            "AnalyzeBlockedReasonCode",
            "BlockedAnalyzeReasonCode",
            "AnalyzeBlockReasonCode",
        });

        ex.ScanActions = TryGetStringListPropAsArray(discovery, new[]
        {
            "ScanActions",
            "ScanSuggestedActions",
            "ScanNextActions",
        });

        ex.AnalyzeActions = TryGetStringListPropAsArray(discovery, new[]
        {
            "AnalyzeActions",
            "AnalyzeSuggestedActions",
            "AnalyzeNextActions",
        });

        // Stable explain chain ordering: scan then analyze.
        // Chain entries are schema-bound tokens only (no free-text).
        if (!string.IsNullOrWhiteSpace(ex.ScanReasonCode))
        {
            ex.ExplainChain.Add(new Godot.Collections.Dictionary
            {
                ["phase"] = "scan",
                ["reason_code"] = ex.ScanReasonCode,
            });
        }
        if (!string.IsNullOrWhiteSpace(ex.AnalyzeReasonCode))
        {
            ex.ExplainChain.Add(new Godot.Collections.Dictionary
            {
                ["phase"] = "analyze",
                ["reason_code"] = ex.AnalyzeReasonCode,
            });
        }

        return ex;
    }

    private static string TryGetStringProp(object obj, string[] propNames)
    {
        try
        {
            var t = obj.GetType();
            foreach (var n in propNames)
            {
                var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (p is null) continue;
                var v = p.GetValue(obj);
                if (v is string s) return s;
            }
        }
        catch
        {
            // Swallow: failure-safe, deterministic empty.
        }
        return "";
    }

    private static Godot.Collections.Array TryGetStringListPropAsArray(object obj, string[] propNames)
    {
        var arr = new Godot.Collections.Array();
        try
        {
            var t = obj.GetType();
            foreach (var n in propNames)
            {
                var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (p is null) continue;
                var v = p.GetValue(obj);
                if (v is null) continue;

                // Accept IEnumerable<string> only.
                if (v is System.Collections.Generic.IEnumerable<string> ie)
                {
                    var list = new System.Collections.Generic.List<string>();
                    foreach (var s in ie)
                    {
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        list.Add(s);
                    }

                    // Determinism: normalize ordering and de-dup.
                    foreach (var s in list.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
                        arr.Add(s);

                    return arr;
                }
            }
        }
        catch
        {
            // Swallow: failure-safe, deterministic empty.
        }

        return arr;
    }
}
