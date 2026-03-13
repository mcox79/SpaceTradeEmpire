using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S8.ADAPTATION.ENTITY.001: Adaptation fragment — collectible precursor artifact.
public enum AdaptationFragmentKind
{
    Biological = 0,
    Structural = 1,
    Energetic = 2,
    Cognitive = 3
}

public class AdaptationFragment
{
    [JsonInclude] public string FragmentId { get; set; } = "";
    [JsonInclude] public string Name { get; set; } = "";
    [JsonInclude] public string Description { get; set; } = "";
    [JsonInclude] public AdaptationFragmentKind Kind { get; set; }
    [JsonInclude] public string ResonancePairId { get; set; } = "";

    // -1 = not yet collected.
    [JsonInclude] public int CollectedTick { get; set; } = -1;

    // Node where this fragment is placed (worldgen).
    [JsonInclude] public string NodeId { get; set; } = "";

    public bool IsCollected => CollectedTick >= 0;
}
