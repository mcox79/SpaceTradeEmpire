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

    // GATE.S6.FRACTURE.TRAVEL_CMD.001: Off-lane fracture travel constants.
    public const int FractureSpeedDivisor = 10;       // 10x slower than lane transit
    public const int MinFractureTravelTechLevel = 1;  // Minimum fleet.TechLevel to initiate
    public const float MinFractureTravelDistance = 1f; // Floor to avoid division by zero

    // GATE.S6.FRACTURE.COST_MODEL.001: Fracture travel costs.
    public const int FractureFuelPerJump = 20;         // FuelCurrent consumed on departure
    public const int FractureHullStressPerJump = 10;   // HullHp damage on arrival
    public const float FractureTracePerArrival = 0.5f; // Trace left at destination node

    // GATE.S6.FRACTURE.DETECTION_REP.001: Fracture use detection by factions.
    public const float TraceDetectionThreshold = 1.0f; // Trace level that triggers faction detection
    public const int FractureDetectionRepPenalty = -10; // Rep penalty when detected
    public const float TraceDecayPerTick = 0.01f;      // Trace decays naturally per tick

    // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Minimum tick before fracture can be discovered.
    public const int FractureDiscoveryMinTick = 200;

    // GATE.S7.FRACTURE.OFFLANE_ROUTES.001: Offlane jump constants (non-adjacent node-to-node).
    public const int OfflaneFuelCostPerUnit = 5;        // Fuel per unit of Euclidean distance
    public const int OfflaneHullStressPerUnit = 2;      // Hull stress per unit of distance
    public const float OfflaneMinDistance = 0.5f;       // Distance floor to avoid zero cost
    public const int OfflaneMinTechLevel = 2;           // Minimum fleet TechLevel for offlane jumps
}
