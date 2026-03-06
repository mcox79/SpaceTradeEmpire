using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S6.REVEAL.SCAN_CMD.001: Player action to advance discovery phase.
// Seen -> Scanned (scan), Scanned -> Analyzed (analyze). Delegates to IntelSystem.
public sealed class ScanDiscoveryCommand : ICommand
{
    public string DiscoveryId { get; }

    public ScanDiscoveryCommand(string discoveryId)
    {
        DiscoveryId = discoveryId ?? "";
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrEmpty(DiscoveryId)) return;
        if (state.Intel?.Discoveries is null) return;
        if (!state.Intel.Discoveries.TryGetValue(DiscoveryId, out var disc)) return;

        const string playerFleetId = "fleet_trader_1";

        if (disc.Phase == DiscoveryPhase.Seen)
        {
            IntelSystem.ApplyScan(state, playerFleetId, DiscoveryId);
        }
        else if (disc.Phase == DiscoveryPhase.Scanned)
        {
            IntelSystem.ApplyAnalyze(state, playerFleetId, DiscoveryId);
        }
        // Analyzed: no-op
    }
}
