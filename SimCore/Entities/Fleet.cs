namespace SimCore.Entities;

public enum FleetState { Idle, Travel, Docked }

public class Fleet
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    
    // LOCATION STATE
    public string CurrentNodeId { get; set; } = ""; // If docked/idle
    public string DestinationNodeId { get; set; } = ""; // If traveling
    public string CurrentEdgeId { get; set; } = "";     // If traveling
    
    // TRAVEL PROGRESS
    public float TravelProgress { get; set; } = 0f; // 0.0 to 1.0
    public float Speed { get; set; } = 0.2f;        // Segments per tick
    
    public FleetState State { get; set; } = FleetState.Idle;
    
    // CARGO (Simple Dictionary for now)
    public Dictionary<string, int> Cargo { get; set; } = new();
}