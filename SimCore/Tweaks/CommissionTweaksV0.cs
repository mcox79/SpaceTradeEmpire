namespace SimCore.Tweaks;

// GATE.S7.FACTION_COMMISSION.ENTITY.001: Commission system tuning constants.
public static class CommissionTweaksV0
{
    // Ticks between rep drift + stipend payments.
    public const int CommissionCycleTicks = 1440;

    // Rep change per cycle: +1 with employer faction.
    public const int EmployerRepGainPerCycle = 1;
    // Rep change per cycle: -1 with each rival faction.
    public const int RivalRepLossPerCycle = 1;

    // Default stipend per cycle (credits). Per-faction overrides possible.
    public const int DefaultStipendCredits = 50;

    // GATE.S7.FACTION_COMMISSION.INFAMY.001: Infamy thresholds.
    // Infamy >= InfamyCapFriendly → max rep tier is Friendly (can't reach Allied).
    // Infamy >= InfamyCapNeutral → max rep tier is Neutral.
    public const int InfamyCapFriendly = 50;
    public const int InfamyCapNeutral = 100;

    // Infamy accumulation amounts.
    public const int InfamyPerAttack = 10;       // attacking a faction's ship
    public const int InfamyPerWarProfiteer = 5;  // selling war goods to enemy of faction
}
