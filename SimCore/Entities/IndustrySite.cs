using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

public class IndustrySite
{
    public string Id { get; set; } = "";
    public string NodeId { get; set; } = "";

    // INPUTS/OUTPUTS/BYPRODUCTS:
    // Inputs represent upkeep consumption in units per tick.
    // Outputs represent primary production in units per tick (scaled by efficiency).
    // Byproducts represent secondary production in units per tick (scaled by efficiency).
    public Dictionary<string, int> Inputs { get; set; } = new();
    public Dictionary<string, int> Outputs { get; set; } = new();
    public Dictionary<string, int> Byproducts { get; set; } = new();

    // BUFFERING:
    // Target buffers are expressed in days of game time. One day = 1440 ticks (1 tick = 1 minute).
    // Logistics uses this to decide when a market is short and by how much.
    public int BufferDays { get; set; } = 1;

    // DEGRADATION:
    // HealthBps is 0..10000. DegradePerDayBps is basis points of health lost per day at full undersupply.
    // DegradeRemainder accumulates fractional degradation deterministically.
    public int HealthBps { get; set; } = 10000;
    public int DegradePerDayBps { get; set; } = 0;
    public long DegradeRemainder { get; set; } = 0;

    // STATE (derived each tick by IndustrySystem)
    public float Efficiency { get; set; } = 1.0f;

    // GATE.S4.INDU.MIN_LOOP.001
    // Opt-in construction pipeline v0. Default false to preserve baseline worlds and goldens.
    public bool ConstructionEnabled { get; set; } = false;

    public bool Active { get; set; } = true;
}
