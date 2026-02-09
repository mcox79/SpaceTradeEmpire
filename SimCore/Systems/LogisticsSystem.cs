using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Intents;
using SimCore.Events;

namespace SimCore.Systems;


public static class LogisticsSystem
{
    // Slice 3 / GATE.LOGI.RETRY.001
    // Number of consecutive pickup observations (0 units loaded) allowed at source before cancel.
    public const int MaxZeroPickupObservations = 3;

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        // 0) Advance in-progress jobs deterministically (phase transitions).
        foreach (var fleet in state.Fleets.Values.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            AdvanceJobState(state, fleet);
        }

        // 1) Identify shortages deterministically (industry sites + inputs sorted).
        var shortages = new List<(string MarketId, string GoodId, int Amount)>();

        foreach (var site in state.IndustrySites.Values.OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            if (!site.Active) continue;
            if (string.IsNullOrWhiteSpace(site.NodeId)) continue;

            var marketId = ResolveMarketForSiteNode(state, site.NodeId);
            if (string.IsNullOrWhiteSpace(marketId)) continue;
            if (!state.Markets.TryGetValue(marketId, out var market)) continue;

            foreach (var input in site.Inputs.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var goodId = input.Key;
                var perTick = input.Value;
                if (string.IsNullOrWhiteSpace(goodId)) continue;
                if (perTick <= 0) continue;

                var target = IndustrySystem.ComputeBufferTargetUnits(site, goodId);
                var current = market.Inventory.GetValueOrDefault(goodId, 0);

                if (current < target)
                {
                    shortages.Add((market.Id, goodId, target - current));
                }
            }
        }

        // Deterministic task order: by destination market, then good.
        shortages = shortages
            .OrderBy(x => x.MarketId, StringComparer.Ordinal)
            .ThenBy(x => x.GoodId, StringComparer.Ordinal)
            .ToList();

        if (shortages.Count == 0) return;

        // 2) Assign idle fleets deterministically.
        foreach (var task in shortages)
        {
            var fleet = state.Fleets.Values
                .OrderBy(f => f.Id, StringComparer.Ordinal)
                .FirstOrDefault(f =>
                    (f.State == FleetState.Idle || f.State == FleetState.Docked) &&
                    f.CurrentJob == null);
            if (fleet is null) break;

            var supplier = FindSupplierDeterministic(state, task.GoodId, excludeMarketId: task.MarketId);
            if (supplier is null) continue;

            // Plan: source market -> dest market
            PlanLogistics(state, fleet, supplier.Id, task.MarketId, task.GoodId, task.Amount);
        }
    }

    private static void AdvanceJobState(SimState state, Fleet fleet)
    {
        if (fleet.CurrentJob is null) return;

        var job = fleet.CurrentJob;

        // Only transition phases / issue transfers when the fleet is idle (arrived at a node and not mid-edge).
        if (fleet.State != FleetState.Idle) return;

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

                        state.EmitLogisticsEvent(new LogisticsEvents.Event
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
                            $"Pickup resulted in 0 units for {job.ZeroPickupObservations} consecutive observations; canceling job.");
                        return;
                    }

                    // Schedule a retry: clear the issued latch so we enqueue another LoadCargoIntent
                    // the next time we are Idle at source (next tick).
                    job.PickupTransferIssued = false;
                    job.PickupCargoBefore = fleet.GetCargoUnits(job.GoodId);
                    job.PickedUpAmount = 0;

                    state.EmitLogisticsEvent(new LogisticsEvents.Event
                    {
                        Type = LogisticsEvents.LogisticsEventType.PickupIssued,
                        FleetId = fleet.Id ?? "",
                        GoodId = job.GoodId ?? "",
                        Amount = job.Amount,
                        SourceNodeId = job.SourceNodeId ?? "",
                        TargetNodeId = job.TargetNodeId ?? "",
                        SourceMarketId = ResolveMarketForSiteNode(state, job.SourceNodeId) ?? "",
                        TargetMarketId = ResolveMarketForSiteNode(state, job.TargetNodeId) ?? "",
                        Note = $"Pickup observed 0 units (obs {job.ZeroPickupObservations}/{MaxZeroPickupObservations}); will retry."
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

                    state.EmitLogisticsEvent(new LogisticsEvents.Event
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

                state.EmitLogisticsEvent(new LogisticsEvents.Event
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

                        state.EmitLogisticsEvent(new LogisticsEvents.Event
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

                state.EmitLogisticsEvent(new LogisticsEvents.Event
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
                var prevId = plannedEdgeIds[i - 1] ?? "";
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

        foreach (var m in state.Markets.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (string.Equals(m.Id, excludeMarketId, StringComparison.Ordinal)) continue;

            var qty = state.GetUnreservedAvailable(m.Id, goodId);
            if (qty <= 10) continue;

            if (best is null || qty > bestQty)
            {
                best = m;
                bestQty = qty;
            }
        }

        return best;
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
        if (!RoutePlanner.TryPlan(state, fleet.CurrentNodeId, sourceNode, fleet.Speed, out var toSourcePlan))
            return false;

        if (!RoutePlanner.TryPlan(state, sourceNode, destNode, fleet.Speed, out var toTargetPlan))
            return false;

        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = goodId,
            SourceNodeId = sourceNode,
            TargetNodeId = destNode,
            Amount = amount,
            Phase = LogisticsJobPhase.Pickup,
            RouteToSourceEdgeIds = toSourcePlan.EdgeIds ?? new List<string>(),
            RouteToTargetEdgeIds = toTargetPlan.EdgeIds ?? new List<string>()
        };

        // Slice 3 / GATE.LOGI.RESERVE.001: Optional reservation at assignment time.
        // This does NOT mutate inventory; enforcement happens in LoadCargoCommand.
        if (state.TryCreateLogisticsReservation(sourceMarketId, goodId, fleet.Id ?? "", amount, out var rid, out var rqty))
        {
            if (fleet.CurrentJob is not null && rqty > 0 && !string.IsNullOrWhiteSpace(rid))
            {
                fleet.CurrentJob.ReservationId = rid;
                fleet.CurrentJob.ReservedAmount = rqty;

                state.EmitLogisticsEvent(new LogisticsEvents.Event
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

        state.EmitLogisticsEvent(new LogisticsEvents.Event
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
        // Deterministic: choose the lowest node id whose MarketId matches.
        foreach (var n in state.Nodes.Values.OrderBy(n => n.Id, StringComparer.Ordinal))
        {
            if (string.Equals(n.MarketId, marketId, StringComparison.Ordinal))
                return n.Id;
        }

        // Fallback: sometimes a node id is used as the market id
        if (state.Nodes.ContainsKey(marketId)) return marketId;

        return null;
    }

    public static void CancelJob(SimState state, Fleet fleet, LogisticsJob job, string note)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (fleet is null) throw new ArgumentNullException(nameof(fleet));
        if (job is null) throw new ArgumentNullException(nameof(job));

        // Release any outstanding reservation deterministically.
        if (!string.IsNullOrWhiteSpace(job.ReservationId))
        {
            state.ReleaseLogisticsReservation(job.ReservationId);

            state.EmitLogisticsEvent(new LogisticsEvents.Event
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

        state.EmitLogisticsEvent(new LogisticsEvents.Event
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

