namespace SimCore.Tweaks;

// 2.5D galaxy shape: vertical spread for disc-like appearance.
// Stars near center get more Y-spread; edge stars flatten toward the galactic plane.
// At radius=200, max Y offset ≈ ±10 sim units (±250u visual at 25x scale).
public static class GalaxyShapeTweaksV0
{
    // Y-spread as fraction of galaxy radius. 0.05 = ±5% of radius.
    public static float DiscThicknessFraction { get; } = 0.05f;

    // Radial falloff: 0.0 = uniform thickness, 1.0 = zero thickness at edge.
    // 0.7 = moderate thinning toward galaxy edge (realistic disc shape).
    public static float RadialFalloff { get; } = 0.7f;

    // Minimum distinct factions within 2 hops of player start.
    // EnsureFactionDiversityAtStartV0 relocates start if below this threshold.
    public const int MinStarterFactionDiversity = 3;
}
