namespace SimCore.Tweaks;

// GATE.T18.NARRATIVE.INSTRUMENT_DISAGREEMENT.001: Instrument disagreement constants.
// Standard sensors accurate for pricing, fracture module accurate for navigation.
// Neither always right — creates "which instrument do I trust?" decision surface.
// All drift values in basis points (10000 = 100%).
public static class InstrumentDisagreementTweaksV0
{
    // Standard sensor price drift (BPS) by instability phase.
    // Standard stays closer to true price — it's the right tool for commerce.
    public const int StandardDriftBpsPhase1 = 100;  // 1%
    public const int StandardDriftBpsPhase2 = 400;  // 4%
    public const int StandardDriftBpsPhase3 = 800;  // 8%

    // Fracture module price drift (BPS) by instability phase.
    // Fracture module is worse for pricing — wrong tool for the job.
    public const int FractureDriftBpsPhase1 = 500;   // 5%
    public const int FractureDriftBpsPhase2 = 1500;  // 15%
    public const int FractureDriftBpsPhase3 = 2500;  // 25%

    // Standard sensor ETA overestimate percentage by instability phase.
    // Standard sensors can't read fracture topology, so they add safety margins.
    public const int StandardEtaOverestimatePctPhase1 = 10;
    public const int StandardEtaOverestimatePctPhase2 = 30;
    public const int StandardEtaOverestimatePctPhase3 = 60;

    // Hash salts for deterministic divergence (arbitrary stable values).
    public const uint STRUCT_SaltStandard = 0x57D0001;
    public const uint STRUCT_SaltFracture = 0xF4C0002;
    public const uint STRUCT_SaltNavigation = 0x4A70003;
}
