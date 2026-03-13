using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S8.HAVEN.ENTITY.001: Haven starbase entity — player's hidden home base.
public enum HavenTier
{
    Undiscovered = 0,
    Powered = 1,       // Tier 1: automatic on first dock
    Inhabited = 2,     // Tier 2: crew quarters, research lab
    Operational = 3,   // Tier 3: drydock, bidirectional thread, hangar bay 2
    Expanded = 4,      // Tier 4: resonance chamber, fabricator
    Awakened = 5       // Tier 5: endgame — accommodation geometry alive
}

public class HavenStarbase
{
    // The node where Haven is located (empty if not yet seeded).
    [JsonInclude] public string NodeId { get; set; } = "";

    // Current tier (Undiscovered until player docks).
    [JsonInclude] public HavenTier Tier { get; set; } = HavenTier.Undiscovered;

    // True once the player has visited Haven at least once.
    [JsonInclude] public bool Discovered { get; set; } = false;

    // Tick when Haven was first discovered.
    [JsonInclude] public int DiscoveryTick { get; set; } = -1;

    // Upgrade progress: ticks remaining for current tier upgrade (0 = not upgrading).
    [JsonInclude] public int UpgradeTicksRemaining { get; set; } = 0;

    // Target tier being upgraded toward (same as Tier if not upgrading).
    [JsonInclude] public HavenTier UpgradeTargetTier { get; set; } = HavenTier.Undiscovered;

    // Hangar: IDs of stored fleet entities (max determined by tier).
    [JsonInclude] public List<string> StoredShipIds { get; set; } = new();

    // Installed Adaptation Fragment IDs (for tier prerequisites and visual effects).
    [JsonInclude] public List<string> InstalledFragmentIds { get; set; } = new();

    // Market ID for Haven's exotic-only market (set during world gen).
    [JsonInclude] public string MarketId { get; set; } = "";

    // Sustain tracking: ticks since last sustain deduction.
    [JsonInclude] public int SustainTickCounter { get; set; } = 0;

    // Whether the bidirectional thread has been unlocked (Tier 3+).
    [JsonInclude] public bool BidirectionalThread { get; set; } = false;

    // Whether Haven's thread has been revealed to a faction ally (Tier 4 choice).
    [JsonInclude] public string RevealedToFactionId { get; set; } = "";

    // GATE.S8.HAVEN.RESIDENTS.001: Named NPCs that appear at Haven (The Keeper, FO candidates).
    [JsonInclude] public List<HavenResident> Residents { get; set; } = new();

    // GATE.S8.HAVEN.TROPHY_WALL.001: Deposited adaptation fragment IDs → deposit tick.
    [JsonInclude] public Dictionary<string, int> TrophyWall { get; set; } = new();
}

// GATE.S8.HAVEN.RESIDENTS.001: A named NPC residing at Haven.
public class HavenResident
{
    [JsonInclude] public string ResidentId { get; set; } = "";
    [JsonInclude] public string Name { get; set; } = "";
    [JsonInclude] public string Role { get; set; } = ""; // "keeper", "fo_candidate", "scholar"
    [JsonInclude] public int AppearedAtTier { get; set; }
    [JsonInclude] public int AppearedTick { get; set; } = -1;
}
