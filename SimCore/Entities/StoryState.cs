using System;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S8.STORY_STATE.ENTITY.001: Story progression state for the 5 Recontextualizations.

[Flags]
public enum RevelationFlags
{
    None = 0,
    R1_Module = 1 << 0,       // Fracture drive modules aren't human-made
    R2_Concord = 1 << 1,      // Concord knew about fracture space — containment not peacekeeping
    R3_Pentagon = 1 << 2,     // Pentagon Break — 5-faction dependency cascade
    R4_Communion = 1 << 3,    // Communion 'unity' masks species privilege
    R5_Instability = 1 << 4,  // Fracture space is alive — trade network is the wound
}

public enum StoryAct
{
    Act1_Innocent = 0,    // Player has no revelations — everything seems normal
    Act2_Questioning = 1, // 1-2 revelations — cracks appearing
    Act3_Revealed = 2,    // 3+ revelations — true nature exposed
}

public class StoryState
{
    // Bitmask of which revelations have fired.
    [JsonInclude] public RevelationFlags RevealedFlags { get; set; } = RevelationFlags.None;

    // Current narrative act (derived from revelation count but persisted for stability).
    [JsonInclude] public StoryAct CurrentAct { get; set; } = StoryAct.Act1_Innocent;

    // Pentagon trade tracking: which faction types the player has traded with.
    // Bit 0=Concord, 1=Naturalize, 2=Communion, 3=Reinforce, 4=Weaver
    [JsonInclude] public int PentagonTradeFlags { get; set; } = 0;

    // Cumulative fracture exposure count (jumps + lattice visits + void site visits).
    [JsonInclude] public int FractureExposureCount { get; set; } = 0;

    // Number of lattice sites visited by the player.
    [JsonInclude] public int LatticeVisitCount { get; set; } = 0;

    // Tick when each revelation was triggered (-1 = not yet).
    [JsonInclude] public int R1Tick { get; set; } = -1;
    [JsonInclude] public int R2Tick { get; set; } = -1;
    [JsonInclude] public int R3Tick { get; set; } = -1;
    [JsonInclude] public int R4Tick { get; set; } = -1;
    [JsonInclude] public int R5Tick { get; set; } = -1;

    // Whether the player has read a Communion data log (prerequisite for R4).
    [JsonInclude] public bool HasReadCommunionLog { get; set; } = false;

    // Count of collected adaptation fragments (prerequisite for R5).
    [JsonInclude] public int CollectedFragmentCount { get; set; } = 0;

    // GATE.S8.PENTAGON.DETECT.001: Pentagon cascade state.
    [JsonInclude] public bool PentagonCascadeActive { get; set; } = false;
    [JsonInclude] public int PentagonCascadeTick { get; set; } = -1;

    // Helper: count of revelations fired.
    [JsonIgnore]
    public int RevelationCount
    {
        get
        {
            int count = 0;
            var flags = (int)RevealedFlags;
            while (flags != 0) { count += flags & 1; flags >>= 1; }
            return count;
        }
    }

    // Helper: check if a specific revelation has fired.
    public bool HasRevelation(RevelationFlags flag) => (RevealedFlags & flag) != 0;

    // Helper: all 5 pentagon factions traded with.
    [JsonIgnore]
    public bool AllPentagonFactionsTraded => PentagonTradeFlags == 0x1F; // bits 0-4 all set
}
