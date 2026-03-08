namespace SimCore.Tweaks;

// GATE.S7.TERRITORY.EMBARGO_MODEL.001: Embargo tuning constants.
// Factions at war embargo goods from the pentagon ring dependency.
public static class EmbargoTweaksV0
{
    // Minimum warfront intensity required to trigger embargoes.
    public const int MinEmbargoIntensity = 2; // Skirmish+

    // Embargo blocks: the good that faction A needs from faction B
    // is embargoed when they are at war. Uses PentagonRing from FactionTweaksV0.
    // Additional embargo: munitions are always embargoed during wartime.
    public const string AlwaysEmbargoedWarGood = "munitions";
}
