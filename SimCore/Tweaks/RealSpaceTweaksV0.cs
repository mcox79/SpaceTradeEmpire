namespace SimCore.Tweaks;

// GATE.S17.REAL_SPACE.STAR_COORDS.001: Real-space galactic scale tuning.
// Sim positions stay at original scale for determinism. GalaxyView multiplies
// by GalacticScaleFactor when rendering to produce galactic-scale 3D layout.
public static class RealSpaceTweaksV0
{
    // Rendering multiplier applied to sim positions by GalaxyView.
    // Sim radius 200 * 25 = 5000u visual radius. ~2000-3000u between neighbors.
    public static float GalacticScaleFactor { get; } = 25f;

    // Approximate radius within which a system's local detail is rendered (in scaled units).
    public static float LocalDetailRadius { get; } = 200f;
}
