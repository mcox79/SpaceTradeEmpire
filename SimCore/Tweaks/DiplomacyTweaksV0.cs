namespace SimCore.Tweaks;

// GATE.S7.DIPLOMACY.FRAMEWORK.001: Diplomacy system tuning constants.
public static class DiplomacyTweaksV0
{
    // ── Treaty constants ──
    public const int TreatyTariffReductionBps = 500;   // STRUCTURAL: 5% tariff reduction
    public const int TreatyDurationTicks = 2880;       // ~2 game days
    public const int TreatyCooldownTicks = 720;        // Cooldown between proposals

    // ── Bounty constants ──
    public const int BountyRewardCreditsMin = 200;
    public const int BountyRewardCreditsRange = 300;   // 200-500 credits
    public const int BountyRewardRep = 5;
    public const int BountyDurationTicks = 1440;       // ~1 game day expiry
    public const int BountyGenerationIntervalTicks = 360; // New bounty every ~6 game hours

    // ── Sanction constants ──
    public const int SanctionTariffIncreaseBps = 2000; // 20% tariff increase
    public const int SanctionDurationTicks = 1440;     // ~1 game day
    public const int SanctionRepPenalty = 15;          // Rep hit for treaty violation

    // ── Proposal acceptance thresholds ──
    // Rep tier determines auto-accept: Allied/Friendly = auto-accept,
    // Neutral = hash-based 50%, Hostile/Enemy = auto-reject.
    public const int ProposalAutoAcceptRepMin = 25;    // Friendly threshold
    public const int ProposalAutoRejectRepMax = -25;   // Hostile threshold

    // ── Faction AI proposal generation ──
    public const int FactionProposalIntervalTicks = 480; // Factions propose every ~8 game hours

    // ── Per-faction diplomatic personalities ──
    // Weights for Treaty vs Bounty proposal (out of 100).
    // Concord: treaty-seeking (80/20), Chitin: bounty-heavy (30/70),
    // Weavers: trade-focused (90/10), Valorin: aggressive (20/80),
    // Communion: peace (95/5).
    public const int ConcordTreatyWeight = 80;
    public const int ChitinTreatyWeight = 30;
    public const int WeaversTreatyWeight = 90;
    public const int ValorinTreatyWeight = 20;
    public const int CommunionTreatyWeight = 95;

    // Maximum active treaties per faction.
    public const int MaxActiveTreatiesPerFaction = 1;
    // Maximum active bounties total.
    public const int MaxActiveBounties = 5;
}
