namespace SimCore.Tweaks;

/// <summary>
/// Fracture numeric constants v0 (GATE.S6.FRACTURE.*).
/// All fracture-related magic numbers routed through here for tweak guard compliance.
/// </summary>
public static class FractureTweaksV0
{
    // GATE.S6.FRACTURE.ACCESS_MODEL.001: Minimum hull_hp_max to enter fracture nodes.
    public const int MinHullHpMaxForFracture = 120;

    // GATE.S6.FRACTURE.ACCESS_MODEL.001: Fracture tier gating threshold.
    // Nodes with FractureTier > this value require a tech-level check.
    public const int MinFractureTierForGating = 0;

    // GATE.S6.FRACTURE.CONTENT.001: Fracture outpost fee multiplier (premium pricing).
    public const float FractureOutpostFeeMultiplier = 1.50f;

    // GATE.S6.FRACTURE.MARKET_MODEL.001: Volatility multiplier (int pct: 150 = 1.5x).
    public const int FractureVolatilityPct = 150;

    // GATE.S6.FRACTURE.MARKET_MODEL.001: Spread multiplier (int pct: 200 = 2x lane spread).
    public const int FractureSpreadPct = 200;

    // GATE.S6.FRACTURE.MARKET_MODEL.001: Volume cap (int pct: 50 = half of lane ideal stock).
    public const int FractureVolumeCapPct = 50;

    // GATE.S6.FRACTURE.MARKET_MODEL.001: Minimum fracture spread (units).
    public const int MinFractureSpread = 2;

    // GATE.S6.FRACTURE.TRAVEL.001: Fracture jump fuel cost multiplier (int pct: 300 = 3x lane cost).
    public const int FractureFuelCostMultiplierPct = 300;

    // GATE.S6.FRACTURE.TRAVEL.001: Fracture jump risk multiplier (int pct: 200 = 2x lane risk).
    public const int FractureRiskMultiplierPct = 200;

    // GATE.S6.FRACTURE.ECON_FEEDBACK.001: Fracture goods flow rate into lane hub (int pct: 10 = 10% per tick).
    public const int FractureGoodsFlowRatePct = 10;
}
