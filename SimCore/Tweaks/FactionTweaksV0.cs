namespace SimCore.Tweaks;

// GATE.S7.FACTION.CONTENT_DATA.001: 5 named factions with lore-accurate content.
// Pentagon dependency ring: Concord→Weavers→Chitin→Valorin→Communion→Concord.
public static class FactionTweaksV0
{
    // ── Faction IDs ──
    public const string ConcordId = "concord";
    public const string ChitinId = "chitin";
    public const string WeaversId = "weavers";
    public const string ValorinId = "valorin";
    public const string CommunionId = "communion";

    // Sorted ordinal for deterministic iteration.
    public static readonly string[] AllFactionIds = { ChitinId, CommunionId, ConcordId, ValorinId, WeaversId };

    // ── Per-faction tariff rates ──
    public const float ConcordTariffRate = 0.05f;
    public const float ChitinTariffRate = 0.15f;
    public const float WeaversTariffRate = 0.08f;
    public const float ValorinTariffRate = 0.20f;
    public const float CommunionTariffRate = 0.03f;

    // ── Per-faction aggression levels: 0=peaceful, 1=defensive, 2=hostile ──
    public const int ConcordAggressionLevel = 0;
    public const int ChitinAggressionLevel = 1;
    public const int WeaversAggressionLevel = 0;
    public const int ValorinAggressionLevel = 2;
    public const int CommunionAggressionLevel = 0;

    // ── Per-faction species ──
    public const string ConcordSpecies = "Human";
    public const string ChitinSpecies = "Insectoid";
    public const string WeaversSpecies = "Silicon";
    public const string ValorinSpecies = "Mammalian";
    public const string CommunionSpecies = "Ethereal";

    // ── Per-faction philosophy (one-word identity) ──
    public const string ConcordPhilosophy = "Order";
    public const string ChitinPhilosophy = "Adaptation";
    public const string WeaversPhilosophy = "Structure";
    public const string ValorinPhilosophy = "Expansion";
    public const string CommunionPhilosophy = "Understanding";

    // ── Legacy role-based defaults (kept for tests using manual faction IDs) ──
    public static float TraderTariffRate { get; } = 0.05f;
    public static float MinerTariffRate { get; } = 0.15f;
    public static float PirateTariffRate { get; } = 0.30f;
    public static int TraderAggressionLevel { get; } = 0;
    public static int MinerAggressionLevel { get; } = 1;
    public static int PirateAggressionLevel { get; } = 2;

    // Aggression thresholds: reputation below this triggers hostile NPC behavior.
    public static int AggroReputationThreshold { get; } = -50;

    // Reputation bounds.
    public static int ReputationDefault { get; } = 0;
    public static int ReputationMin { get; } = -100;
    public static int ReputationMax { get; } = 100;

    // Reputation change amounts.
    public static int TradeRepGain { get; } = 1;         // per successful trade at faction station
    public static int AttackRepLoss { get; } = -25;      // per attack on faction ship

    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Reputation below this blocks trade entirely.
    public static int TradeBlockedRepThreshold { get; } = -50;
    // Tariff basis points multiplier: TariffRate 0.15 -> 1500 bps.
    public static int TariffBpsMultiplier { get; } = 10000;

    // GATE.S7.REPUTATION.ACCESS_TIERS.001: 5-tier reputation thresholds.
    // Allied >=75, Friendly >=25, Neutral >=-25, Hostile >=-75, Enemy <-75.
    public const int AlliedThreshold = 75;
    public const int FriendlyThreshold = 25;
    public const int NeutralThreshold = -25;
    public const int HostileThreshold = -75;

    // GATE.S7.REPUTATION.ACCESS_TIERS.001: Access gating by tier.
    public const int DockBlockedBelowTier = -75;
    public const int TradeBlockedBelowTier = -25;
    public const int TechRequiresMinTier = 25;

    // GATE.S7.REPUTATION.PRICING_CURVES.001: Tier-based price modifier in basis points.
    public const int AlliedPriceBps = -1500;
    public const int FriendlyPriceBps = -500;
    public const int NeutralPriceBps = 0;
    public const int HostilePriceBps = 2000;

    // GATE.S7.TERRITORY.PATROL_RESPONSE.001: Cargo threshold for Restricted regime pursuit.
    public const int CargoThresholdForPursuit = 5;

    // GATE.S7.FACTION.PENTAGON_RING.001: Pentagon dependency ring.
    // Ring: Concord needs Composites from Weavers → Weavers need Electronics from Chitin →
    //       Chitin need RareMetals from Valorin → Valorin need ExoticCrystals from Communion →
    //       Communion need Food from Concord → (cycle)
    public static readonly (string Consumer, string Supplier, string Good)[] PentagonRing =
    {
        (ConcordId,   WeaversId,   "composites"),
        (WeaversId,   ChitinId,    "electronics"),
        (ChitinId,    ValorinId,   "rare_metals"),
        (ValorinId,   CommunionId, "exotic_crystals"),
        (CommunionId, ConcordId,   "food"),
    };

    // Secondary cross-links (non-ring trade dependencies).
    public static readonly (string Consumer, string Supplier, string Good)[] SecondaryCrossLinks =
    {
        (CommunionId, ConcordId,   "fuel"),          // Communion also needs fuel from Concord
        (ConcordId,   ChitinId,    "electronics"),    // Concord buys electronics from Chitin
        (ValorinId,   WeaversId,   "composites"),     // Valorin buys composites for ship hulls
    };
}
