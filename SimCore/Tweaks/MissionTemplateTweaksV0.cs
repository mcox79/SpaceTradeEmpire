namespace SimCore.Tweaks;

// GATE.T48.TEMPLATE.SCHEMA.001: Mission template system tuning constants.
public static class MissionTemplateTweaksV0
{
    // BPS divisor for reward multiplier math (10000 bps = 100%).
    public const int BpsDivisor = 10000;

    // Maximum active template missions at once.
    public const int MaxActiveTemplateMissions = 3;

    // Default twist probability weight sum (for normalization).
    public const int TwistWeightSumBps = 10000;

    // GATE.T48.TEMPLATE.TWIST_ENGINE.001: Twist injection constants.
    // Twist evaluation interval (every N ticks while template mission active).
    public const int TwistCheckIntervalTicks = 30;

    // Instability bonus weight (bps per instability phase at player node).
    public const int InstabilityTwistBonusBps = 500;

    // Warfront proximity bonus weight (bps if player within 2 hops of active warfront).
    public const int WarfrontProximityBonusBps = 1000;

    // Maximum twists that can fire during a single template mission.
    public const int MaxTwistsPerMission = 3;

    // Reward scaling per active twist in bps (base * (1 + twist_count * this / 10000)).
    public const int DefaultPerTwistBonusBps = 3000;

    // Faction rep multiplier bonus for matching faction template (bps added to reward).
    public const int FactionRepBonusBps = 1500;
}
