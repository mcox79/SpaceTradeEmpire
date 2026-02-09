using System.Collections.Generic;

namespace SimCore.Entities;

public enum LogisticsJobPhase
{
    Pickup = 0,
    Deliver = 1
}

public class LogisticsJob
{
    public string GoodId { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";

    // Quantity requested by the shortage planner
    public int Amount { get; set; } = 0;

    // Slice 3 / GATE.LOGI.JOB.001
    // Job is multi-hop and carries deterministic route legs.
    public LogisticsJobPhase Phase { get; set; } = LogisticsJobPhase.Pickup;

    // Route legs (lane ids) computed deterministically at plan time.
    public List<string> RouteToSourceEdgeIds { get; set; } = new();
    public List<string> RouteToTargetEdgeIds { get; set; } = new();

    // Slice 3 / GATE.LOGI.EXEC.001
    // Latches to ensure we enqueue each transfer intent at most once,
    // even if the fleet remains Idle at the node for multiple ticks.
    public bool PickupTransferIssued { get; set; } = false;
    public bool DeliveryTransferIssued { get; set; } = false;

    // Slice 3 / GATE.LOGI.FULFILL.001
    // Because intents resolve before LogisticsSystem each tick, a pickup intent executes on the next tick.
    // We record cargo-before when issuing pickup, then on later ticks observe the actual delta applied.
    public int PickupCargoBefore { get; set; } = 0;

    // Actual amount loaded (may be less than Amount due to clamping). This is what we deliver.
    public int PickedUpAmount { get; set; } = 0;

    // Slice 3 / GATE.LOGI.RETRY.001
    // Counts consecutive observations where a pickup attempt resulted in 0 units loaded at source.
    public int ZeroPickupObservations { get; set; } = 0;

    // Slice 3 / GATE.LOGI.RESERVE.001
    // Optional reservation created at plan/assignment time to protect supplier inventory from other loads.
    // Enforced in LoadCargoCommand (LogisticsSystem never mutates inventories directly).
    public string ReservationId { get; set; } = "";
    public int ReservedAmount { get; set; } = 0;
}


