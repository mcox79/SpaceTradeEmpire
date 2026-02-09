using System.Text.Json.Serialization;
using SimCore.Entities;

namespace SimCore.Entities;

public enum FleetState
{
    Idle = 0,
    Traveling = 1,
    Docked = 2,
    // SLICE 3: Fracture Travel (Off-Lane)
    FractureTraveling = 3
}

public class Fleet
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";

    // LOCATION STATE
    public string CurrentNodeId { get; set; } = "";
    public string DestinationNodeId { get; set; } = "";
    public string CurrentEdgeId { get; set; } = "";

    // ROUTE STATE (Slice 3 / GATE.FLEET.ROUTE.001)
    // DestinationNodeId is the next immediate hop while traveling.
    // FinalDestinationNodeId is the requested end node for the whole route.
    public string FinalDestinationNodeId { get; set; } = "";

    // Planned lane sequence to reach FinalDestinationNodeId.
    // RouteEdgeIndex points to the next edge to traverse.
    public List<string> RouteEdgeIds { get; set; } = new();
    public int RouteEdgeIndex { get; set; } = 0;

    // TRAVEL STATE
    public FleetState State { get; set; } = FleetState.Idle;
    public float TravelProgress { get; set; } = 0f; // 0.0 to 1.0
    public float Speed { get; set; } = 0.5f; // AU per tick

    // LOGIC STATE
    public string CurrentTask { get; set; } = "Idle"; // Explanation for UI
    public LogisticsJob? CurrentJob { get; set; } // The Active Order

    // CARGO
    // Gate target: dict keyed by GoodId, quantities are non-negative ints.
    // Determinism note: when iterating/serializing, sort keys with Ordinal.
    public Dictionary<string, int> Cargo { get; set; } = new();


    // Legacy/simple resource (kept until we explicitly replace it with Goods-based supplies).
    public int Supplies { get; set; } = 100;

    [JsonIgnore]
    public bool IsMoving => State == FleetState.Traveling || State == FleetState.FractureTraveling;

    public int GetCargoUnits(string goodId)
    {
        if (string.IsNullOrWhiteSpace(goodId)) return 0;
        return Cargo.TryGetValue(goodId, out var v) ? v : 0;
    }

}
