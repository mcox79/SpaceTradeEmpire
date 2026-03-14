namespace SimCore.Tweaks;

// GATE.S8.PENTAGON.DETECT.001: Pentagon Break cascade tuning constants.
public static class PentagonBreakTweaksV0
{
    // Communion food self-production rate (units per CascadeFoodIntervalTicks) once cascade active.
    public const int CommunionFoodSelfProductionQty = 5;

    // Interval (ticks) between Communion food injections during cascade.
    public const int CascadeFoodIntervalTicks = 30;

    // GDP impact on downstream faction markets (basis points, 1000 = 10%).
    public const int CascadeGdpImpactBps = 1000;
}
