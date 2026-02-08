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
}
