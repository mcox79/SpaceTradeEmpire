namespace SimCore.Tweaks;

/// <summary>
/// Lane gate transit cost constants.
/// Transit costs credits only (no fuel). Price scales with lane congestion.
/// </summary>
public static class TransitTweaksV0
{
    // Base credit cost per lane jump (flat fee).
    public const int BaseCreditCost = 25;

    // Maximum congestion surcharge added on top of base cost.
    // At 100% lane utilization the surcharge equals this value.
    public const int MaxCongestionSurcharge = 75;
}
