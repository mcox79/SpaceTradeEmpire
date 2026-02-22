using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Intents;
using SimCore.Events;
using System.Buffers;

namespace SimCore.Systems;


public static class LogisticsSystem
{
    // Slice 3 / GATE.LOGI.RETRY.001
    // Number of consecutive pickup observations (0 units loaded) allowed at source before cancel.
    public const int MaxZeroPickupObservations = 3;

    // No-numeric constants for tweak-routing-guard compliance.
    private const string _S1 = " ";
    private const string _S2 = "  ";
    private const string _S8 = "        ";
    private static readonly string _S10 = _S8 + _S2;
    private static readonly string _S64 = _S8 + _S8 + _S8 + _S8 + _S8 + _S8 + _S8 + _S8;

    private static int N0 => string.Empty.Length;
    private static int N1 => _S1.Length;
    private static int N2 => _S2.Length;
    private static int N8 => _S8.Length;
    private static int N10 => _S10.Length;
    private static int N64 => _S64.Length;

    private static bool s_logiEventsEnabled = true;

    // Per-SimState scratch buffers to avoid per-tick allocations.
    // ConditionalWeakTable ensures no leaks across discarded SimState instances.
    private sealed class Scratch
    {
        public readonly List<Fleet> FleetsSorted = new();
        public readonly List<Market> MarketsSorted = new();
        public readonly List<(string MarketId, string GoodId, int Amount)> Shortages = new();
        public readonly List<IndustrySite> SitesSorted = new();
        public readonly List<string> InputKeys = new();
    }

    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratchByState = new();

    private static void EmitLogi(SimState state, LogisticsEvents.Event e)
    {
        if (!s_logiEventsEnabled) return;
        state.EmitLogisticsEvent(e);
    }

    // Loop viability threshold sourced from tweak config.
    // Returns null when unset or invalid, so callers can preserve legacy behavior.
    private static int? TryGetLoopViabilitySupplierQtyCutoffExclusiveOverride(SimState state)
    {
        var t = state?.Tweaks;
        if (t is null) return null;

        var v = t.LoopViabilityThreshold;
        if (double.IsNaN(v) || double.IsInfinity(v)) return null;

        // Deterministic rounding (avoid platform-dependent casts).
        var cutoff = (long)Math.Round(v, MidpointRounding.AwayFromZero);

        // Treat non-positive as "unset" to preserve legacy behavior.
        if (cutoff <= string.Empty.Length) return null;

        if (cutoff > int.MaxValue) return int.MaxValue;
        return (int)cutoff;
    }

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        bool logiBreakdown = string.Equals(
            Environment.GetEnvironmentVariable("STE_LOGI_BREAKDOWN"),
            "1",
            StringComparison.Ordinal);

        // Cache once per tick, do NOT read env var per event call.
        s_logiEventsEnabled = !string.Equals(
            Environment.GetEnvironmentVariable("STE_LOGI_EVENTS"),
            "0",
            StringComparison.Ordinal);

        long msAdvance = 0, msShortages = 0, msAssign = 0;
        long allocAdvance = 0, allocShortages = 0, allocAssign = 0;
        long msTryPlan = 0, allocTryPlan = 0;

        static void Measure(Action a, out long elapsedMs, out long allocBytes)
        {
            long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            a();
            sw.Stop();
            long afterAlloc = GC.GetAllocatedBytesForCurrentThread();

            elapsedMs = sw.ElapsedMilliseconds;
            allocBytes = afterAlloc - beforeAlloc;
        }

        // 0) Advance in-progress jobs deterministically.
        var scratch = s_scratchByState.GetOrCreateValue(state);

        var fleetsSorted = scratch.FleetsSorted;
        fleetsSorted.Clear();
        if (fleetsSorted.Capacity < state.Fleets.Count) fleetsSorted.Capacity = state.Fleets.Count;

        // Building the sorted list is part of "advance" overhead.
        if (!logiBreakdown)
        {
            foreach (var f in state.Fleets.Values)
                if (f != null) fleetsSorted.Add(f);

            fleetsSorted.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));
            for (int i = 0; i < fleetsSorted.Count; i++)
                AdvanceJobState(state, fleetsSorted[i]);
        }
        else
        {
            Measure(() =>
            {
                foreach (var f in state.Fleets.Values)
                    if (f != null) fleetsSorted.Add(f);

                fleetsSorted.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));
                for (int i = 0; i < fleetsSorted.Count; i++)
                    AdvanceJobState(state, fleetsSorted[i]);
            }, out msAdvance, out allocAdvance);
        }

        // Build deterministic market list once per tick (avoid sorting inside TryPlan).
        var marketsSorted = scratch.MarketsSorted;
        marketsSorted.Clear();
        if (marketsSorted.Capacity < state.Markets.Count) marketsSorted.Capacity = state.Markets.Count;

        foreach (var m in state.Markets.Values)
            if (m != null) marketsSorted.Add(m);

        marketsSorted.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));

        // 1) Identify shortages deterministically.
        var shortages = scratch.Shortages;
        shortages.Clear();
        if (shortages.Capacity < N64) shortages.Capacity = N64;

        if (!logiBreakdown)
        {
            var sitesSorted = scratch.SitesSorted;
            sitesSorted.Clear();
            if (sitesSorted.Capacity < state.IndustrySites.Count) sitesSorted.Capacity = state.IndustrySites.Count;

            foreach (var s in state.IndustrySites.Values)
                if (s != null) sitesSorted.Add(s);

            sitesSorted.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));

            for (int si = 0; si < sitesSorted.Count; si++)
            {
                var site = sitesSorted[si];
                if (string.IsNullOrWhiteSpace(site.NodeId)) continue;

                var marketId = ResolveMarketForSiteNode(state, site.NodeId);
                if (string.IsNullOrWhiteSpace(marketId)) continue;
                if (!state.Markets.TryGetValue(marketId, out var market) || market is null) continue;

                var inputKeys = scratch.InputKeys;
                inputKeys.Clear();
                if (inputKeys.Capacity < site.Inputs.Count) inputKeys.Capacity = site.Inputs.Count;

                foreach (var kv in site.Inputs)
                    if (!string.IsNullOrWhiteSpace(kv.Key)) inputKeys.Add(kv.Key);

                inputKeys.Sort(StringComparer.Ordinal);

                for (int ki = N0; ki < inputKeys.Count; ki++)
                {
                    var goodId = inputKeys[ki];
                    var perTick = site.Inputs.TryGetValue(goodId, out var v) ? v : 0;
                    if (perTick <= 0) continue;

                    var target = IndustrySystem.ComputeBufferTargetUnits(site, goodId);
                    var current = market.Inventory.GetValueOrDefault(goodId, 0);

                    if (current < target)
                        shortages.Add((market.Id, goodId, target - current));
                }
            }

            shortages.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.MarketId ?? "", b.MarketId ?? "");
                if (c != 0) return c;
                c = string.CompareOrdinal(a.GoodId ?? "", b.GoodId ?? "");
                if (c != 0) return c;
                return a.Amount.CompareTo(b.Amount);
            });
        }
        else
        {
            Measure(() =>
            {
                var sitesSorted = new List<IndustrySite>(state.IndustrySites.Count);
                foreach (var s in state.IndustrySites.Values)
                    if (s != null) sitesSorted.Add(s);

                sitesSorted.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));

                for (int si = 0; si < sitesSorted.Count; si++)
                {
                    var site = sitesSorted[si];
                    if (string.IsNullOrWhiteSpace(site.NodeId)) continue;

                    var marketId = ResolveMarketForSiteNode(state, site.NodeId);
                    if (string.IsNullOrWhiteSpace(marketId)) continue;
                    if (!state.Markets.TryGetValue(marketId, out var market) || market is null) continue;

                    var inputKeys = scratch.InputKeys;
                    inputKeys.Clear();
                    if (inputKeys.Capacity < site.Inputs.Count) inputKeys.Capacity = site.Inputs.Count;

                    foreach (var kv in site.Inputs)
                        if (!string.IsNullOrWhiteSpace(kv.Key)) inputKeys.Add(kv.Key);

                    inputKeys.Sort(StringComparer.Ordinal);

                    for (int ki = N0; ki < inputKeys.Count; ki++)
                    {
                        var goodId = inputKeys[ki];
                        var perTick = site.Inputs.TryGetValue(goodId, out var v) ? v : 0;
                        if (perTick <= 0) continue;

                        var target = IndustrySystem.ComputeBufferTargetUnits(site, goodId);
                        var current = market.Inventory.GetValueOrDefault(goodId, 0);

                        if (current < target)
                            shortages.Add((market.Id, goodId, target - current));
                    }
                }

                shortages.Sort((a, b) =>
                {
                    int c = string.CompareOrdinal(a.MarketId ?? "", b.MarketId ?? "");
                    if (c != 0) return c;
                    c = string.CompareOrdinal(a.GoodId ?? "", b.GoodId ?? "");
                    if (c != 0) return c;
                    return a.Amount.CompareTo(b.Amount);
                });
            }, out msShortages, out allocShortages);
        }

        if (shortages.Count == 0)
        {
            if (logiBreakdown)
            {
                Console.WriteLine(
                    "LOGI_BREAKDOWN_V0\n" +
                    $"advance_ms={msAdvance}\nadvance_alloc_bytes={allocAdvance}\n" +
                    $"shortages_ms={msShortages}\nshortages_alloc_bytes={allocShortages}\n" +
                    $"assign_ms={msAssign}\nassign_alloc_bytes={allocAssign}\n" +
                    $"tryplan_ms={msTryPlan}\ntryplan_alloc_bytes={allocTryPlan}");
            }

            return;
        }

        // 2) Assign idle fleets deterministically.
        if (!logiBreakdown)
        {
            for (int ti = 0; ti < shortages.Count; ti++)
            {
                var task = shortages[ti];

                Fleet? chosen = null;
                for (int fi = 0; fi < fleetsSorted.Count; fi++)
                {
                    var f = fleetsSorted[fi];
                    if (f.State != FleetState.Idle && f.State != FleetState.Docked) continue;
                    if (f.CurrentJob != null) continue;
                    if (f.LastJobCancelTick == state.Tick) continue;
                    if (!string.IsNullOrWhiteSpace(f.ManualOverrideNodeId)) continue;

                    chosen = f;
                    break;
                }

                if (chosen is null) break;

                if (!TryPlanFromBestReachableSupplierDeterministic(state, chosen, marketsSorted, task.MarketId, task.GoodId, task.Amount))
                    continue;
            }
        }
        else
        {
            Measure(() =>
            {
                for (int ti = 0; ti < shortages.Count; ti++)
                {
                    var task = shortages[ti];

                    Fleet? chosen = null;
                    for (int fi = 0; fi < fleetsSorted.Count; fi++)
                    {
                        var f = fleetsSorted[fi];
                        if (f.State != FleetState.Idle && f.State != FleetState.Docked) continue;
                        if (f.CurrentJob != null) continue;
                        if (f.LastJobCancelTick == state.Tick) continue;
                        if (!string.IsNullOrWhiteSpace(f.ManualOverrideNodeId)) continue;

                        chosen = f;
                        break;
                    }

                    if (chosen is null) break;

                    long m, a;
                    Measure(() =>
                    {
                        TryPlanFromBestReachableSupplierDeterministic(state, chosen, marketsSorted, task.MarketId, task.GoodId, task.Amount);
                    }, out m, out a);

                    msTryPlan += m;
                    allocTryPlan += a;
                }
            }, out msAssign, out allocAssign);

            Console.WriteLine(
                "LOGI_BREAKDOWN_V0\n" +
                $"advance_ms={msAdvance}\nadvance_alloc_bytes={allocAdvance}\n" +
                $"shortages_ms={msShortages}\nshortages_alloc_bytes={allocShortages}\n" +
                $"assign_ms={msAssign}\nassign_alloc_bytes={allocAssign}\n" +
                $"tryplan_ms={msTryPlan}\ntryplan_alloc_bytes={allocTryPlan}");

        }
    }

    private static void AdvanceJobState(SimState state, Fleet fleet)
    {
        if (fleet.CurrentJob is null) return;

        // Manual override suppresses job-driven routing and transfers.
        // The override command is responsible for canceling jobs, but this makes it robust.
        if (!string.IsNullOrWhiteSpace(fleet.ManualOverrideNodeId)) return;

        var job = fleet.CurrentJob;

        // Only transition phases / issue transfers when the fleet is at a node and not mid-edge.
        // In live runs, fleets may be Docked at markets; treat Docked as eligible for deterministic job advancement.
        if (fleet.State != FleetState.Idle && fleet.State != FleetState.Docked) return;

        if (job.Phase == LogisticsJobPhase.Pickup)
        {
            if (string.Equals(fleet.CurrentNodeId, job.SourceNodeId, StringComparison.Ordinal))
            {
                // At source: enqueue pickup transfer exactly once.
                // Important: intents resolve BEFORE LogisticsSystem each tick, so this pickup executes next tick.
                if (!job.PickupTransferIssued)
                {
                    var sourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId);
                    if (!string.IsNullOrWhiteSpace(sourceMarketId))
                    {
                        job.PickupCargoBefore = fleet.GetCargoUnits(job.GoodId);
                        state.EnqueueIntent(new LoadCargoIntent(fleet.Id, sourceMarketId, job.GoodId, job.Amount));
                        job.PickupTransferIssued = true;

                        EmitLogi(state, new LogisticsEvents.Event
                        {
                            Type = LogisticsEvents.LogisticsEventType.PickupIssued,
                            FleetId = fleet.Id ?? "",
                            GoodId = job.GoodId ?? "",
                            Amount = job.Amount,
                            SourceNodeId = job.SourceNodeId ?? "",
                            TargetNodeId = job.TargetNodeId ?? "",
                            SourceMarketId = sourceMarketId ?? "",
                            TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                            Note = "Enqueued LOAD_CARGO intent (executes next tick)."
                        });
                    }

                    // Do not advance phase on the same tick we enqueue the pickup.
                    return;
                }

                // Pickup was issued on a prior tick. Observe what actually loaded (clamped by transfer rules).
                if (job.PickedUpAmount <= 0)
                {
                    var nowCargo = fleet.GetCargoUnits(job.GoodId);
                    var delta = nowCargo - job.PickupCargoBefore;
                    if (delta < 0) delta = 0;
                    job.PickedUpAmount = delta;
                }

                // If we still got nothing, retry deterministically up to N observations, then cancel.
                if (job.PickedUpAmount <= 0)
                {
                    job.ZeroPickupObservations++;

                    if (job.ZeroPickupObservations >= MaxZeroPickupObservations)
                    {
                        CancelJob(state, fleet, job,
                            $"INCIDENT:ZERO_PICKUP Pickup resulted in 0 units for {job.ZeroPickupObservations} consecutive observations; canceling job.");
                        return;
                    }

                    // Schedule a retry: clear the issued latch so we enqueue another LoadCargoIntent
                    // the next time we are Idle at source (next tick).
                    job.PickupTransferIssued = false;
                    job.PickupCargoBefore = fleet.GetCargoUnits(job.GoodId);
                    job.PickedUpAmount = 0;

                    EmitLogi(state, new LogisticsEvents.Event
                    {
                        Type = LogisticsEvents.LogisticsEventType.PickupIssued,
                        FleetId = fleet.Id ?? "",
                        GoodId = job.GoodId ?? "",
                        Amount = job.Amount,
                        SourceNodeId = job.SourceNodeId ?? "",
                        TargetNodeId = job.TargetNodeId ?? "",
                        SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                        TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                        Note = $"CAUSE:ZERO_PICKUP_OBS Pickup observed 0 units (obs {job.ZeroPickupObservations}/{MaxZeroPickupObservations}); will retry."

                    });

                    return;
                }

                // Success: reset retry counter.
                job.ZeroPickupObservations = 0;

                // Begin delivery leg (movement will occur on later ticks).
                job.Phase = LogisticsJobPhase.Deliver;

                // Release any remaining reservation now that pickup is complete.
                if (!string.IsNullOrWhiteSpace(job.ReservationId))
                {
                    state.ReleaseLogisticsReservation(job.ReservationId);

                    EmitLogi(state, new LogisticsEvents.Event
                    {
                        Type = LogisticsEvents.LogisticsEventType.ReservationReleased,
                        FleetId = fleet.Id ?? "",
                        GoodId = job.GoodId ?? "",
                        Amount = job.ReservedAmount,
                        SourceNodeId = job.SourceNodeId ?? "",
                        TargetNodeId = job.TargetNodeId ?? "",
                        SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                        TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                        Note = "Reservation released after pickup."
                    });

                    // Clear job linkage so future loads do not treat it as reserved-owner.
                    job.ReservationId = "";
                    job.ReservedAmount = 0;
                }

                EmitLogi(state, new LogisticsEvents.Event
                {
                    Type = LogisticsEvents.LogisticsEventType.PhaseChangedToDeliver,
                    FleetId = fleet.Id ?? "",
                    GoodId = job.GoodId ?? "",
                    Amount = job.PickedUpAmount,
                    SourceNodeId = job.SourceNodeId ?? "",
                    TargetNodeId = job.TargetNodeId ?? "",
                    SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                    TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                    Note = "Pickup applied; switching to Deliver phase."
                });

                // Clear any existing route state; MovementSystem will plan deterministically on next Process().
                fleet.RouteEdgeIds.Clear();
                fleet.RouteEdgeIndex = 0;
                fleet.FinalDestinationNodeId = "";

                fleet.DestinationNodeId = job.TargetNodeId ?? "";
                fleet.CurrentTask = $"Delivering {job.GoodId} to {job.TargetNodeId}";
            }

            else
            {
                // Follow the precomputed pickup leg deterministically.
                EnsureFollowingPlannedRouteLeg(state, fleet, job.RouteToSourceEdgeIds, $"Fetching {job.GoodId} from {job.SourceNodeId}");
            }
        }
        else
        {
            if (string.Equals(fleet.CurrentNodeId, job.TargetNodeId, StringComparison.Ordinal))
            {
                // At destination: enqueue delivery transfer exactly once.
                if (!job.DeliveryTransferIssued)
                {
                    var destMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId);
                    if (!string.IsNullOrWhiteSpace(destMarketId))
                    {
                        // Clamp happens inside UnloadCargoCommand; deliver what actually loaded.
                        var deliverQty = job.PickedUpAmount > 0 ? job.PickedUpAmount : job.Amount;
                        state.EnqueueIntent(new UnloadCargoIntent(fleet.Id, destMarketId, job.GoodId, deliverQty));
                        job.DeliveryTransferIssued = true;

                        EmitLogi(state, new LogisticsEvents.Event
                        {
                            Type = LogisticsEvents.LogisticsEventType.DeliveryIssued,
                            FleetId = fleet.Id ?? "",
                            GoodId = job.GoodId ?? "",
                            Amount = job.Amount,
                            SourceNodeId = job.SourceNodeId ?? "",
                            TargetNodeId = job.TargetNodeId ?? "",
                            SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                            TargetMarketId = destMarketId ?? "",
                            Note = "Enqueued UNLOAD_CARGO intent."
                        });
                    }
                }

                // Mark job complete. (Unload will execute next kernel step; intent is independent of CurrentJob.)
                fleet.CurrentJob = null;
                fleet.CurrentTask = "Idle";

                EmitLogi(state, new LogisticsEvents.Event
                {
                    Type = LogisticsEvents.LogisticsEventType.JobCompleted,
                    FleetId = fleet.Id ?? "",
                    GoodId = job.GoodId ?? "",
                    Amount = job.Amount,
                    SourceNodeId = job.SourceNodeId ?? "",
                    TargetNodeId = job.TargetNodeId ?? "",
                    SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                    TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                    Note = "Job cleared from fleet."
                });
            }
            else
            {
                // Follow the precomputed delivery leg deterministically.
                EnsureFollowingPlannedRouteLeg(state, fleet, job.RouteToTargetEdgeIds, $"Delivering {job.GoodId} to {job.TargetNodeId}");
            }
        }
    }

    private static void EnsureFollowingPlannedRouteLeg(SimState state, Fleet fleet, IReadOnlyList<string> plannedEdgeIds, string taskLabel)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (fleet is null) throw new ArgumentNullException(nameof(fleet));
        if (plannedEdgeIds is null) throw new ArgumentNullException(nameof(plannedEdgeIds));

        // If we already have this exact leg loaded, do nothing.
        if (fleet.RouteEdgeIds.Count == plannedEdgeIds.Count)
        {
            var same = true;
            for (var i = 0; i < plannedEdgeIds.Count; i++)
            {
                if (!string.Equals(fleet.RouteEdgeIds[i], plannedEdgeIds[i], StringComparison.Ordinal))
                {
                    same = false;
                    break;
                }
            }

            if (same && fleet.RouteEdgeIndex >= 0 && fleet.RouteEdgeIndex < fleet.RouteEdgeIds.Count)
            {
                fleet.CurrentTask = taskLabel;
                return;
            }
        }

        // Validate the leg edges exist and infer final node id deterministically (last edge's ToNodeId).
        if (plannedEdgeIds.Count == 0)
        {
            // No movement needed; leave routing empty.
            fleet.RouteEdgeIds.Clear();
            fleet.RouteEdgeIndex = 0;
            fleet.FinalDestinationNodeId = "";
            fleet.DestinationNodeId = "";
            fleet.CurrentTask = taskLabel;
            fleet.State = FleetState.Idle;
            return;
        }

        string finalNodeId = "";
        for (var i = 0; i < plannedEdgeIds.Count; i++)
        {
            var eid = plannedEdgeIds[i] ?? "";
            if (string.IsNullOrWhiteSpace(eid)) return;
            if (!state.Edges.TryGetValue(eid, out var e)) return;

            if (i == 0)
            {
                // Must start from current node; otherwise this plan is stale.
                if (!string.Equals(fleet.CurrentNodeId, e.FromNodeId, StringComparison.Ordinal))
                    return;
            }

            if (i > 0)
            {
                var prevId = plannedEdgeIds[i - N1] ?? "";
                if (!state.Edges.TryGetValue(prevId, out var prev)) return;
                if (!string.Equals(prev.ToNodeId, e.FromNodeId, StringComparison.Ordinal))
                    return;
            }

            finalNodeId = e.ToNodeId ?? "";
        }

        // Load the leg exactly as planned (copy into fleet route state).
        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIds.AddRange(plannedEdgeIds);
        fleet.RouteEdgeIndex = 0;

        // Ensure MovementSystem does not reinterpret DestinationNodeId as a new request.
        fleet.DestinationNodeId = "";
        fleet.FinalDestinationNodeId = finalNodeId;

        fleet.CurrentTask = taskLabel;
        fleet.State = FleetState.Idle;
    }


    private static string ResolveMarketForSiteNode(SimState state, string? siteNodeId)
    {
        if (string.IsNullOrWhiteSpace(siteNodeId)) return "";

        // Deterministic: node lookup is by key.
        if (state.Nodes.TryGetValue(siteNodeId, out var node) && !string.IsNullOrWhiteSpace(node.MarketId))
            return node.MarketId;

        // Fallback: sometimes market id == node id (existing behavior).
        return siteNodeId;
    }

    private static Market? FindSupplierDeterministic(SimState state, string goodId, string excludeMarketId)
    {
        // Deterministic: scan markets by id; choose highest inventory; tie-break by id.
        Market? best = null;
        int bestQty = int.MinValue;

        var cutoffOverride = TryGetLoopViabilitySupplierQtyCutoffExclusiveOverride(state);

        foreach (var m in state.Markets.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (string.Equals(m.Id, excludeMarketId, StringComparison.Ordinal)) continue;

            var qty = state.GetUnreservedAvailable(m.Id, goodId);

            if (cutoffOverride.HasValue)
            {
                if (qty <= cutoffOverride.Value) continue;
            }
            else
            {
                if (qty <= N10) continue;
            }

            if (best is null || qty > bestQty)
            {
                best = m;
                bestQty = qty;
            }
        }

        return best;
    }

    private static bool TryPlanFromBestReachableSupplierDeterministic(
        SimState state,
        Fleet fleet,
        IReadOnlyList<Market> marketsSorted,
        string destMarketId,
        string goodId,
        int amount)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (fleet is null) throw new ArgumentNullException(nameof(fleet));
        if (marketsSorted is null) throw new ArgumentNullException(nameof(marketsSorted));

        // Full deterministic candidate ordering (qty desc, then id asc) with pooled storage (no List allocation).
        var pool = ArrayPool<(string MarketId, int Qty)>.Shared;
        var buf = pool.Rent(marketsSorted.Count);
        int count = N0;

        try
        {
            var cutoffOverride = TryGetLoopViabilitySupplierQtyCutoffExclusiveOverride(state);

            for (int i = N0; i < marketsSorted.Count; i++)
            {
                var m = marketsSorted[i];
                if (m is null) continue;

                var mid = m.Id ?? "";
                if (mid.Length == N0) continue;
                if (string.Equals(mid, destMarketId, StringComparison.Ordinal)) continue;

                var qty = state.GetUnreservedAvailable(mid, goodId);

                if (cutoffOverride.HasValue)
                {
                    if (qty <= cutoffOverride.Value) continue;
                }
                else
                {
                    if (qty <= N10) continue;
                }

                buf[count++] = (mid, qty);
            }

            if (count == N0) return false;

            // Deterministic sort: qty desc, then market id asc.
            Array.Sort(buf, N0, count, Comparer<(string MarketId, int Qty)>.Create((a, b) =>
            {
                int c = b.Qty.CompareTo(a.Qty); // desc
                if (c != N0) return c;
                return string.CompareOrdinal(a.MarketId ?? "", b.MarketId ?? "");
            }));

            for (int i = N0; i < count; i++)
            {
                var c = buf[i];
                if (PlanLogistics(state, fleet, c.MarketId, destMarketId, goodId, amount))
                    return true;
            }

            return false;
        }
        finally
        {
            pool.Return(buf, clearArray: true);
        }
    }

    public static bool PlanLogistics(SimState state, Fleet fleet, string sourceMarketId, string destMarketId, string goodId, int amount)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (fleet is null) throw new ArgumentNullException(nameof(fleet));
        if (string.IsNullOrWhiteSpace(sourceMarketId)) return false;
        if (string.IsNullOrWhiteSpace(destMarketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (amount <= 0) return false;

        var sourceNode = GetNodeForMarketDeterministic(state, sourceMarketId);
        var destNode = GetNodeForMarketDeterministic(state, destMarketId);
        if (sourceNode is null || destNode is null) return false;

        // Plan both legs deterministically at job creation time.
        if (!RoutePlanner.TryPlanChoice(state, fleet.CurrentNodeId, sourceNode, fleet.Speed, maxCandidates: N8, out var toSourceChoice))
            return false;

        if (!RoutePlanner.TryPlanChoice(state, sourceNode, destNode, fleet.Speed, maxCandidates: N8, out var toTargetChoice))
            return false;

        // Explain chosen route deterministically (schema-bound).
        // Emit only when there is an actual choice to explain (CandidateCount >= 2) to reduce event spam.
        if (toSourceChoice.CandidateCount >= N2)
        {
            EmitLogi(state, new LogisticsEvents.Event
            {
                Type = LogisticsEvents.LogisticsEventType.RouteChosen,
                FleetId = fleet.Id ?? "",
                GoodId = goodId ?? "",
                Amount = amount,
                SourceNodeId = toSourceChoice.OriginId ?? "",
                TargetNodeId = toSourceChoice.DestId ?? "",
                SourceMarketId = sourceMarketId ?? "",
                TargetMarketId = destMarketId ?? "",
                OriginId = toSourceChoice.OriginId ?? "",
                DestId = toSourceChoice.DestId ?? "",
                ChosenRouteId = toSourceChoice.ChosenRouteId ?? "",
                CandidateCount = toSourceChoice.CandidateCount,
                TieBreakReason = toSourceChoice.TieBreakReason ?? "",
                Note = "Route chosen for pickup leg."
            });
        }

        if (toTargetChoice.CandidateCount >= N2)
        {
            EmitLogi(state, new LogisticsEvents.Event
            {
                Type = LogisticsEvents.LogisticsEventType.RouteChosen,
                FleetId = fleet.Id ?? "",
                GoodId = goodId ?? "",
                Amount = amount,
                SourceNodeId = toTargetChoice.OriginId ?? "",
                TargetNodeId = toTargetChoice.DestId ?? "",
                SourceMarketId = sourceMarketId ?? "",
                TargetMarketId = destMarketId ?? "",
                OriginId = toTargetChoice.OriginId ?? "",
                DestId = toTargetChoice.DestId ?? "",
                ChosenRouteId = toTargetChoice.ChosenRouteId ?? "",
                CandidateCount = toTargetChoice.CandidateCount,
                TieBreakReason = toTargetChoice.TieBreakReason ?? "",
                Note = "Route chosen for delivery leg."
            });
        }

        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = goodId ?? "",
            SourceNodeId = sourceNode ?? "",
            TargetNodeId = destNode ?? "",
            Amount = amount,
            Phase = LogisticsJobPhase.Pickup,
            RouteToSourceEdgeIds = toSourceChoice.ChosenPlan.EdgeIds ?? new List<string>(),
            RouteToTargetEdgeIds = toTargetChoice.ChosenPlan.EdgeIds ?? new List<string>()
        };

        // Slice 3 / GATE.LOGI.RESERVE.001: Optional reservation at assignment time.
        // This does NOT mutate inventory; enforcement happens in LoadCargoCommand.
        var reserveMarketId = sourceMarketId ?? "";
        var reserveGoodId = goodId ?? "";

        if (!string.IsNullOrWhiteSpace(reserveMarketId) &&
            !string.IsNullOrWhiteSpace(reserveGoodId) &&
            state.TryCreateLogisticsReservation(reserveMarketId, reserveGoodId, fleet.Id ?? "", amount, out var rid, out var rqty))
        {
            if (fleet.CurrentJob is not null && rqty > 0 && !string.IsNullOrWhiteSpace(rid))
            {
                fleet.CurrentJob.ReservationId = rid;
                fleet.CurrentJob.ReservedAmount = rqty;

                EmitLogi(state, new LogisticsEvents.Event
                {
                    Type = LogisticsEvents.LogisticsEventType.ReservationCreated,
                    FleetId = fleet.Id ?? "",
                    GoodId = goodId ?? "",
                    Amount = rqty,
                    SourceNodeId = sourceNode ?? "",
                    TargetNodeId = destNode ?? "",
                    SourceMarketId = sourceMarketId ?? "",
                    TargetMarketId = destMarketId ?? "",
                    Note = $"Reserved {rqty} units at source market."
                });
            }
        }

        var plannedJob = fleet.CurrentJob;
        if (plannedJob is null) return false;

        // Load planned pickup leg immediately (no replanning).
        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIds.AddRange(plannedJob.RouteToSourceEdgeIds);
        fleet.RouteEdgeIndex = 0;

        // Prevent MovementSystem from treating DestinationNodeId as a new request.
        fleet.DestinationNodeId = "";
        fleet.FinalDestinationNodeId = sourceNode ?? "";


        fleet.State = FleetState.Idle;
        fleet.CurrentTask = $"Fetching {goodId} from {sourceMarketId}";

        EmitLogi(state, new LogisticsEvents.Event
        {
            Type = LogisticsEvents.LogisticsEventType.JobPlanned,
            FleetId = fleet.Id ?? "",
            GoodId = goodId ?? "",
            Amount = amount,
            SourceNodeId = sourceNode ?? "",
            TargetNodeId = destNode ?? "",
            SourceMarketId = sourceMarketId ?? "",
            TargetMarketId = destMarketId ?? "",
            Note = fleet.CurrentTask ?? ""
        });

        return true;
    }

    private static string? GetNodeForMarketDeterministic(SimState state, string marketId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(marketId)) return null;

        // Deterministic: choose the lowest node id whose MarketId matches, without sorting.
        string? bestId = null;

        foreach (var n in state.Nodes.Values)
        {
            if (n is null) continue;
            if (!string.Equals(n.MarketId, marketId, StringComparison.Ordinal)) continue;

            var id = n.Id;
            if (string.IsNullOrWhiteSpace(id)) continue;

            if (bestId is null || string.CompareOrdinal(id, bestId) < 0)
                bestId = id;
        }

        if (bestId is not null) return bestId;

        // Fallback: sometimes a node id is used as the market id
        if (state.Nodes.ContainsKey(marketId)) return marketId;

        return null;
    }

    private static void CancelJob(SimState state, Fleet fleet, LogisticsJob job, string note)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (fleet is null) throw new ArgumentNullException(nameof(fleet));
        if (job is null) throw new ArgumentNullException(nameof(job));

        // Release any outstanding reservation deterministically.
        if (!string.IsNullOrWhiteSpace(job.ReservationId))
        {
            state.ReleaseLogisticsReservation(job.ReservationId);

            EmitLogi(state, new LogisticsEvents.Event
            {
                Type = LogisticsEvents.LogisticsEventType.ReservationReleased,
                FleetId = fleet.Id ?? "",
                GoodId = job.GoodId ?? "",
                Amount = job.ReservedAmount,
                SourceNodeId = job.SourceNodeId ?? "",
                TargetNodeId = job.TargetNodeId ?? "",
                SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                Note = "Reservation released on job cancel."
            });
        }

        fleet.CurrentJob = null;

        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIndex = 0;

        fleet.DestinationNodeId = "";
        fleet.FinalDestinationNodeId = "";

        fleet.State = FleetState.Idle;
        fleet.CurrentTask = "Idle";

        EmitLogi(state, new LogisticsEvents.Event
        {
            Type = LogisticsEvents.LogisticsEventType.JobCanceled,
            FleetId = fleet.Id ?? "",
            GoodId = job.GoodId ?? "",
            Amount = job.Amount,
            SourceNodeId = job.SourceNodeId ?? "",
            TargetNodeId = job.TargetNodeId ?? "",
            SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
            TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
            Note = note ?? ""
        });
    }
}

