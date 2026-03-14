namespace SimCore.Tweaks;

// GATE.S8.MEGAPROJECT.ENTITY.001: Megaproject tuning constants.
public static class MegaprojectTweaksV0
{
    // Minimum faction rep at the target node to start a megaproject.
    public const int MinFactionRepToStart = 25;

    // --- Fracture Anchor ---
    public const int AnchorTicksPerStage = 100;
    public const int AnchorCreditCost = 5000;
    public const int AnchorExoticMatterPerStage = 30;
    public const int AnchorCompositesPerStage = 15;

    // --- Trade Corridor ---
    public const int CorridorTicksPerStage = 80;
    public const int CorridorCreditCost = 8000;
    public const int CorridorRareMetalsPerStage = 20;
    public const int CorridorElectronicsPerStage = 15;

    // --- Sensor Pylon Network ---
    public const int PylonTicksPerStage = 60;
    public const int PylonCreditCost = 4000;
    public const int PylonElectronicsPerStage = 20;
    public const int PylonExoticCrystalsPerStage = 10;

    // Trade Corridor transit time reduction (percentage).
    public const int CorridorTransitSpeedBoostPct = 30;

    // Sensor Pylon scan range extension (hop radius).
    public const int PylonScanRangeHops = 3;
}
