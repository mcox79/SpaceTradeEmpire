namespace SimCore.Tweaks;

// GATE.T61.POSTMORTEM.CAUSE_CODES.001: Postmortem cause code analysis constants.
public static class PostmortemTweaksV0
{
    // Slippage detection: if actual price differs from published by more than this %, flag Slippage.
    public const int SlippageThresholdBps = 500;

    // Heat threshold: if edge heat at trade route exceeds this, flag Heat.
    public const int HeatCauseThreshold = 300;

    // Capital lockup: if fleet has cargo but can't sell within this many ticks, flag CapitalLockup.
    public const int CapitalLockupTickThreshold = 120;

    // Queueing: if lane queue position > this, flag Queueing.
    public const int QueuePositionThreshold = 3;

    // Service shortage: if destination market stock for target good is 0, flag ServiceShortage.
    public const int ShortageStockThreshold = 0;
}
