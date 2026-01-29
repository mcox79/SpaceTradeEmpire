using System.Text.Json.Serialization;

namespace SimCore.Entities;

public enum FleetState
{
    Idle = 0,
    Traveling = 1
}

public class Fleet
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    
    // LOCATION STATE
    public string CurrentNodeId { get; set; } = "";
    public string DestinationNodeId { get; set; } = "";
    public string CurrentEdgeId { get; set; } = "";
    
    // TRAVEL STATE
    public FleetState State { get; set; } = FleetState.Idle;
    public float TravelProgress { get; set; } = 0f; // 0.0 to 1.0
    public float Speed { get; set; } = 0.5f; // AU per tick (Base speed)

    // CARGO (Supplies are needed to move)
    public int Supplies { get; set; } = 100;

    [JsonIgnore]
    public bool IsMoving => State == FleetState.Traveling;
}