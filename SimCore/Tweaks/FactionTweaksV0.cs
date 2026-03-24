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

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirate faction — always hostile to player.
    public const string PirateId = "pirate";

    // Sorted ordinal for deterministic iteration (pirates excluded — they are not a territorial faction).
    public static readonly string[] AllFactionIds = { ChitinId, CommunionId, ConcordId, ValorinId, WeaversId };

    // All faction IDs including pirate (for reputation lookups).
    public static readonly string[] AllFactionIdsIncludingPirate = { ChitinId, CommunionId, ConcordId, PirateId, ValorinId, WeaversId };

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

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirate aggro threshold — 999 means always hostile (even rep 0).
    public const int PirateAggroReputationThreshold = 999;

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirate fleet tuning.
    public const int PirateHullHp = 60;
    public const int PirateShieldHp = 30;
    public const float PirateSpeed = 0.9f;

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirate loot table — enhanced drops.
    public const int PirateLootSalvagedTechMin = 2;
    public const int PirateLootSalvagedTechMax = 3;
    public const int PirateLootRareMetalsMin = 3;
    public const int PirateLootRareMetalsMax = 6;
    public const int PirateLootCreditsMin = 50;
    public const int PirateLootCreditsMax = 100;

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Min/max pirate patrol fleets seeded at FRONTIER/RIM nodes.
    public const int PirateFleetCountMin = 3;
    public const int PirateFleetCountMax = 5;

    // GATE.T55.COMBAT.TERRITORY_ENFORCE.001: Territory enforcement threshold.
    // At Closed-regime nodes, faction patrols become hostile when player rep is below this.
    public const int TerritoryHostileThreshold = 25;

    // Reputation bounds.
    public static int ReputationDefault { get; } = 0;
    public static int ReputationMin { get; } = -100;
    public static int ReputationMax { get; } = 100;

    // Reputation change amounts.
    public static int TradeRepGain { get; } = 1;         // per successful trade at faction station
    public static int AttackRepLoss { get; } = -25;      // per attack on faction ship
    // GATE.T55.REP.MISSION_WIRE.001: Rep gained on mission completion for the offering faction.
    public const int MissionRepGain = 5;

    // GATE.S7.REPUTATION.TRADE_DRIFT.001: Natural decay toward neutral.
    // Rep decays by 1 point per DecayIntervalTicks toward 0.
    public const int RepDecayIntervalTicks = 1440;  // ~1 game day
    public const int RepDecayAmount = 1;             // points per interval

    // GATE.S7.REPUTATION.WAR_PROFITEER.001: War profiteering rep changes.
    public const int WarProfiteerBuyerGain = 2;   // +rep with buyer faction
    public const int WarProfiteerEnemyLoss = -1;  // -rep with enemy faction

    // War-critical goods that trigger profiteering rep effects.
    public static readonly string[] WarCriticalGoods = { "munitions", "composites", "fuel" };

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

    // GATE.S7.TERRITORY.HYSTERESIS.001: Regime hysteresis — improvement toward Open requires
    // sustained stability for this many ticks. Worsening (toward Hostile) is instant.
    public const int RegimeHysteresisMinTicks = 30;

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

    // ── GATE.S7.FACTION_VIS.COLOR_PALETTE.001: Faction visual color palettes ──
    // RGB float triplets (0-1). Primary = hull/territory fill, Secondary = accent/trim, Accent = UI highlight.
    // Concord = blue (orderly federation)
    public static readonly (float R, float G, float B) ConcordPrimary   = (0.2f, 0.4f, 0.9f);
    public static readonly (float R, float G, float B) ConcordSecondary = (0.5f, 0.6f, 0.95f);
    public static readonly (float R, float G, float B) ConcordAccent    = (0.3f, 0.7f, 1.0f);

    // Chitin = amber (insectoid hive)
    public static readonly (float R, float G, float B) ChitinPrimary    = (0.85f, 0.65f, 0.1f);
    public static readonly (float R, float G, float B) ChitinSecondary  = (0.7f, 0.5f, 0.15f);
    public static readonly (float R, float G, float B) ChitinAccent     = (1.0f, 0.8f, 0.2f);

    // Weavers = green (silicon constructors)
    public static readonly (float R, float G, float B) WeaversPrimary   = (0.15f, 0.75f, 0.3f);
    public static readonly (float R, float G, float B) WeaversSecondary = (0.2f, 0.6f, 0.35f);
    public static readonly (float R, float G, float B) WeaversAccent    = (0.3f, 0.9f, 0.4f);

    // Valorin = red (aggressive expansionists)
    public static readonly (float R, float G, float B) ValorinPrimary   = (0.85f, 0.15f, 0.15f);
    public static readonly (float R, float G, float B) ValorinSecondary = (0.7f, 0.2f, 0.2f);
    public static readonly (float R, float G, float B) ValorinAccent    = (1.0f, 0.3f, 0.2f);

    // Communion = purple (ethereal mystics)
    public static readonly (float R, float G, float B) CommunionPrimary   = (0.6f, 0.2f, 0.8f);
    public static readonly (float R, float G, float B) CommunionSecondary = (0.5f, 0.3f, 0.7f);
    public static readonly (float R, float G, float B) CommunionAccent    = (0.7f, 0.4f, 1.0f);

    // GATE.T55.COMBAT.PIRATE_FACTION.001: Pirate = dark crimson (outlaws)
    public static readonly (float R, float G, float B) PiratePrimary   = (0.5f, 0.1f, 0.1f);
    public static readonly (float R, float G, float B) PirateSecondary = (0.4f, 0.15f, 0.15f);
    public static readonly (float R, float G, float B) PirateAccent    = (0.8f, 0.2f, 0.1f);

    /// <summary>
    /// Returns (Primary, Secondary, Accent) color tuples for a given faction ID.
    /// Returns neutral gray if faction is unknown.
    /// </summary>
    public static ((float R, float G, float B) Primary, (float R, float G, float B) Secondary, (float R, float G, float B) Accent) GetFactionColors(string factionId)
    {
        return factionId switch
        {
            ConcordId   => (ConcordPrimary, ConcordSecondary, ConcordAccent),
            ChitinId    => (ChitinPrimary, ChitinSecondary, ChitinAccent),
            WeaversId   => (WeaversPrimary, WeaversSecondary, WeaversAccent),
            ValorinId   => (ValorinPrimary, ValorinSecondary, ValorinAccent),
            CommunionId => (CommunionPrimary, CommunionSecondary, CommunionAccent),
            PirateId    => (PiratePrimary, PirateSecondary, PirateAccent),
            _           => ((0.5f, 0.5f, 0.5f), (0.6f, 0.6f, 0.6f), (0.7f, 0.7f, 0.7f)),
        };
    }
}
