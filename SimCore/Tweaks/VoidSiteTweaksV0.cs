namespace SimCore.Tweaks;

// GATE.S6.FRACTURE.VOID_SITES.001: Void site generation tuning.
public static class VoidSiteTweaksV0
{
    // Minimum star count required for void site generation.
    public static int MinStarCount { get; } = 2;

    // Maximum void sites per edge (1 + hash % this).
    public static int MaxSitesPerEdge { get; } = 2;

    // Position interpolation range along edge: [MinT, MinT + 1000/RangeDiv].
    public static float MinT { get; } = 0.3f;
    public static int THashMod { get; } = 1000;
    public static float TRangeDiv { get; } = 2500f;

    // Perpendicular offset parameters.
    public static float MinLengthEpsilon { get; } = 0.001f;
    public static int OffsetHashMod { get; } = 100;
    public static float OffsetScale { get; } = 10f;
    public static float OffsetHalfRange { get; } = 5f;
    public static int OffsetHashShift { get; } = 16;
}
