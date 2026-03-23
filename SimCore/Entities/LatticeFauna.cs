using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T45.DEEP_DREAD.LATTICE_FAUNA.001: Emergent computational process on degrading Thread Lattice.
// NOT biological — interference patterns that behave like predators.
// Attracted by fracture drive signature, avoidable by going dark.
public enum LatticeFaunaState
{
    Approaching = 0,  // Detected player signature, en route
    Present = 1,      // At player's node, interfering
    Departing = 2     // Player went dark or fauna timed out
}

public class LatticeFauna
{
    [JsonInclude] public string Id { get; set; } = "";
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public LatticeFaunaState State { get; set; } = LatticeFaunaState.Approaching;
    [JsonInclude] public int SpawnTick { get; set; }
    [JsonInclude] public int ArrivalTick { get; set; }     // Tick when fauna arrives at node
    [JsonInclude] public int DarkTicksAccumulated { get; set; } // Ticks player has stayed dark
}
