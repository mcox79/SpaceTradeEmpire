using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;

namespace SimCore.Tests.Commands;

// GATE.S6.REVEAL.SCAN_CMD.001
[TestFixture]
public sealed class ScanDiscoveryCommandTests
{
    private static SimState CreateStateWithDiscovery(string nodeId, string discoveryId, DiscoveryPhase phase)
    {
        var state = new SimState(42);
        var node = new Node { Id = nodeId, Name = nodeId };
        node.SeededDiscoveryIds.Add(discoveryId);
        state.Nodes[nodeId] = node;
        state.PlayerLocationNodeId = nodeId;

        // Player fleet must be at the node for ApplyAnalyze hub check.
        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            CurrentNodeId = nodeId
        };

        state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
        {
            DiscoveryId = discoveryId,
            Phase = phase
        };
        return state;
    }

    [Test]
    public void ScanDiscovery_AdvancesSeenToScanned()
    {
        var state = CreateStateWithDiscovery("star_0", "DISC_001", DiscoveryPhase.Seen);
        new ScanDiscoveryCommand("DISC_001").Execute(state);
        Assert.That(state.Intel.Discoveries["DISC_001"].Phase, Is.EqualTo(DiscoveryPhase.Scanned));
    }

    [Test]
    public void ScanDiscovery_AdvancesScannedToAnalyzed()
    {
        var state = CreateStateWithDiscovery("star_0", "DISC_001", DiscoveryPhase.Scanned);
        new ScanDiscoveryCommand("DISC_001").Execute(state);
        Assert.That(state.Intel.Discoveries["DISC_001"].Phase, Is.EqualTo(DiscoveryPhase.Analyzed));
    }

    [Test]
    public void ScanDiscovery_NoOpOnAnalyzed()
    {
        var state = CreateStateWithDiscovery("star_0", "DISC_001", DiscoveryPhase.Analyzed);
        new ScanDiscoveryCommand("DISC_001").Execute(state);
        Assert.That(state.Intel.Discoveries["DISC_001"].Phase, Is.EqualTo(DiscoveryPhase.Analyzed));
    }

    [Test]
    public void ScanDiscovery_NoOpOnUnknownDiscovery()
    {
        var state = CreateStateWithDiscovery("star_0", "DISC_001", DiscoveryPhase.Seen);
        new ScanDiscoveryCommand("DISC_UNKNOWN").Execute(state);
        Assert.That(state.Intel.Discoveries["DISC_001"].Phase, Is.EqualTo(DiscoveryPhase.Seen));
    }
}
