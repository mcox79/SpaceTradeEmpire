namespace SimCore.Tweaks;

// GATE.S15.FEEL.JUMP_EVENT_SYS.001: Jump event probabilities and loot ranges.
public static class JumpEventTweaksV0
{
    // Probability (0-100) of any jump event per lane transit completion.
    public static int EventChancePct { get; } = 25;

    // Weighted distribution: salvage 50%, signal 25%, turbulence 25%.
    public static int SalvageWeight { get; } = 50;
    public static int SignalWeight { get; } = 25;
    public static int TurbulenceWeight { get; } = 25;

    // Salvage loot range.
    public static int SalvageMinQty { get; } = 1;
    public static int SalvageMaxQty { get; } = 5;

    // Turbulence hull damage range.
    public static int TurbulenceMinDamage { get; } = 2;
    public static int TurbulenceMaxDamage { get; } = 8;

    // Denominator for event chance roll (rng.Next(ProbabilityRange)).
    public static int ProbabilityRange { get; } = 100;

    // Max jump events kept in history.
    public static int MaxJumpEventHistory { get; } = 20;
}
