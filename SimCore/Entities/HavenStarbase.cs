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

    // GATE.S8.HAVEN.KEEPER.001: Keeper ambient evolution tier (Dormant→Awakened).
    [JsonInclude] public KeeperTier KeeperLevel { get; set; } = KeeperTier.Dormant;

    // Cumulative exotic matter delivered to Haven (for Keeper progression).
    [JsonInclude] public int ExoticMatterDelivered { get; set; } = 0;

    // Cumulative data logs discovered by the player (for Keeper progression).
    [JsonInclude] public int DataLogsDiscovered { get; set; } = 0;

    // GATE.S8.HAVEN.RESONANCE.001: Activated resonance pair IDs.
    [JsonInclude] public List<string> ActivatedResonancePairs { get; set; } = new();

    // Resonance cooldown: tick when last resonance was activated (-1 = no cooldown).
    [JsonInclude] public int ResonanceCooldownUntilTick { get; set; } = -1;

    // GATE.S8.HAVEN.FABRICATOR.001: T3 module fabrication queue.
    [JsonInclude] public string? FabricatingModuleId { get; set; }
    [JsonInclude] public int FabricationTicksRemaining { get; set; } = 0;
    [JsonInclude] public List<string> CompletedFabricationIds { get; set; } = new();

    // GATE.S8.HAVEN.RESEARCH_LAB.001: Haven research lab — parallel research slots gated by tier.
    [JsonInclude] public List<HavenResearchSlot> ResearchLabSlots { get; set; } = new();

    // GATE.S8.HAVEN.ENDGAME_PATHS.001: Chosen endgame path (None until player chooses at Tier 4+).
    [JsonInclude] public EndgamePath ChosenEndgamePath { get; set; } = EndgamePath.None;
    [JsonInclude] public int EndgamePathChosenTick { get; set; } = -1;

    // GATE.S8.HAVEN.ACCOMMODATION.001: Per-thread accommodation progress (0-100 each).
    [JsonInclude] public Dictionary<string, int> AccommodationProgress { get; set; } = new();

    // GATE.S8.HAVEN.COMMUNION_REP.001: Communion Representative NPC state.
    [JsonInclude] public CommunionRepState CommunionRep { get; set; } = new();
}

// GATE.S8.HAVEN.KEEPER.001: 5-tier Keeper ambient evolution.
public enum KeeperTier
{
    Dormant = 0,        // Silent presence — acknowledges player with faint light
    Aware = 1,          // Responds to player proximity — ambient hints begin
    Guiding = 2,        // Offers cryptic directional hints — fragment locations
    Communicating = 3,  // Direct communication — reveals fragment purposes
    Awakened = 4        // Full communication — reveals true nature of Haven
}

// GATE.S8.WIN.GAME_RESULT.001: Game result state.
public enum GameResult
{
    InProgress = 0, // Game is still running
    Victory = 1,    // Player completed chosen endgame path
    Death = 2,      // Player fleet hull reached 0
    Bankruptcy = 3  // Credits below threshold with no recovery
}

// GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path choices.
public enum EndgamePath
{
    None = 0,       // Not yet chosen
    Reinforce = 1,  // Strengthen existing structures — keep the factions stable
    Naturalize = 2, // Accept fracture space as natural — coexist with geometry
    Renegotiate = 3 // Challenge the geometry itself — reshape the rules
}

// GATE.S8.HAVEN.ACCOMMODATION.001: Known accommodation thread IDs.
public static class AccommodationThreadIds
{
    public const string Discovery = "discovery";
    public const string Commerce = "commerce";
    public const string Conflict = "conflict";
    public const string Harmony = "harmony";
    public static readonly string[] All = { Discovery, Commerce, Conflict, Harmony };
}

// GATE.S8.HAVEN.COMMUNION_REP.001: Communion Representative NPC presence at Haven.
public class CommunionRepState
{
    // Whether the representative is currently present at Haven.
    [JsonInclude] public bool Present { get; set; } = false;
    // Dialogue progression tier (0=not met, 1-3=progressive trust).
    [JsonInclude] public int DialogueTier { get; set; } = 0;
    // Last tick the player interacted with the representative.
    [JsonInclude] public int LastInteractionTick { get; set; } = -1;
}

// GATE.S8.HAVEN.RESEARCH_LAB.001: A single Haven research slot with active research state.
public class HavenResearchSlot
{
    [JsonInclude] public int SlotIndex { get; set; } = 0;
    [JsonInclude] public string TechId { get; set; } = "";
    [JsonInclude] public int ProgressTicks { get; set; } = 0;
    [JsonInclude] public int TotalTicks { get; set; } = 0;
    [JsonInclude] public int StallTicks { get; set; } = 0;
    [JsonInclude] public string StallReason { get; set; } = "";

    [JsonIgnore]
    public bool IsActive => !string.IsNullOrEmpty(TechId);
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
