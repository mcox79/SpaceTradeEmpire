namespace SimCore.Tweaks;

// GATE.T48.ANOMALY.CHAIN_SYSTEM.001: Anomaly chain system tuning constants.
public static class AnomalyChainTweaksV0
{
    // Minimum galaxy size (node count) to seed 2 chains instead of 1.
    public const int TwoChainMinNodes = 30;

    // Minimum instability phase for a deep-space node to be a valid chain starter.
    public const int MinStarterInstabilityPhase = 2;

    // Maximum BFS depth when searching for reachable deep-space nodes.
    public const int MaxBfsDepth = 8;

    // Chain count by galaxy size.
    public const int SmallGalaxyChainCount = 1;
    public const int LargeGalaxyChainCount = 2;
}
