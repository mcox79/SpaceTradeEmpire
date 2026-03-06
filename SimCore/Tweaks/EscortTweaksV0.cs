namespace SimCore.Tweaks;

/// <summary>
/// GATE.S5.ESCORT_PROG.MODEL.001
/// Tweak-routed constants for EscortV0 and PatrolV0 program executors.
/// </summary>
public static class EscortTweaksV0
{
    public static readonly int EscortSpeedBps = 8000;      // 80% of fleet base speed
    public static readonly int PatrolCycleBaseTicks = 30;  // ticks per patrol leg
}
