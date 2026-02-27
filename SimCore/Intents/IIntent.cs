namespace SimCore.Intents;

public interface IIntent
{
    string Kind { get; }
    void Apply(SimState state);
}

// GATE.S3_6.DISCOVERY_STATE.003
// Scan verb v0: intent-driven Seen->Scanned transition with deterministic rejection.
public sealed class DiscoveryScanIntentV0 : IIntent
{
    public const string KindToken = "DISCOVERY_SCAN_V0";

    public string Kind => KindToken;

    public string FleetId { get; }
    public string DiscoveryId { get; }

    public DiscoveryScanIntentV0(string fleetId, string discoveryId)
    {
        FleetId = fleetId ?? "";
        DiscoveryId = discoveryId ?? "";
    }

    public void Apply(SimState state)
    {
        // Deterministic: single dictionary lookup%update only.
        SimCore.Systems.IntelSystem.ApplyScan(state, FleetId, DiscoveryId);
    }
}
