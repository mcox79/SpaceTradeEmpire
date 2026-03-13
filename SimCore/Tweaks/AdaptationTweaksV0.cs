namespace SimCore.Tweaks;

// GATE.S8.ADAPTATION.COLLECTION.001: Adaptation fragment tuning constants.
public static class AdaptationTweaksV0
{
    // Resonance pair bonuses (basis points or flat values).
    public const int TradeMarginBonusBps = 500;       // +5% trade margin
    public const int ScanRangeBonusPct = 10;           // +10% scan range
    public const int HangarBayBonus = 1;               // +1 Haven hangar bay
    public const int FractureCostReductionPct = 10;    // -10% fracture travel cost
    public const int ShieldCapacityBonusPct = 15;      // +15% shield capacity
    public const int PowerBudgetBonusPct = 10;         // +10% module power budget
    public const int ResearchSpeedBonusPct = 5;        // +5% research speed

    // Fragment placement bias: prefer nodes with distance > this from player start.
    public const int PlacementMinDistanceFromStart = 3;

    // Ancient hull restoration.
    public const int HullRestoreCreditCost = 5000;
    public const int HullRestoreExoticMatterCost = 100;
    public const int HullRestoreDurationTicks = 200;
}
