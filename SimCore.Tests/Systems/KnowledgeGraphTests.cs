using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001
[TestFixture]
public sealed class KnowledgeGraphTests
{
    private SimState MakeStateWithConnections()
    {
        var state = new SimState(42);

        // Add two discoveries
        state.Intel.Discoveries["disc_A"] = new DiscoveryStateV0
        {
            DiscoveryId = "disc_A",
            Phase = DiscoveryPhase.Seen
        };
        state.Intel.Discoveries["disc_B"] = new DiscoveryStateV0
        {
            DiscoveryId = "disc_B",
            Phase = DiscoveryPhase.Seen
        };

        // Add a connection between them
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "conn_1",
            SourceDiscoveryId = "disc_A",
            TargetDiscoveryId = "disc_B",
            ConnectionType = KnowledgeConnectionType.SameOrigin,
            Description = "Both originated from the same ancient facility.",
            IsRevealed = false
        });

        return state;
    }

    [Test]
    public void Connection_VisibleWhenBothSeen()
    {
        var state = MakeStateWithConnections();
        var conn = state.Intel.KnowledgeConnections[0];

        Assert.That(KnowledgeGraphSystem.IsConnectionVisible(state, conn), Is.True);
    }

    [Test]
    public void Connection_InvisibleWhenOneEndpointMissing()
    {
        var state = MakeStateWithConnections();

        // Remove one discovery
        state.Intel.Discoveries.Remove("disc_B");

        var conn = state.Intel.KnowledgeConnections[0];
        Assert.That(KnowledgeGraphSystem.IsConnectionVisible(state, conn), Is.False);
    }

    [Test]
    public void Connection_NotRevealedUntilBothAnalyzed()
    {
        var state = MakeStateWithConnections();

        // Both Seen but not Analyzed → visible but not revealed
        KnowledgeGraphSystem.Process(state);
        var conn = state.Intel.KnowledgeConnections[0];
        Assert.That(conn.IsRevealed, Is.False);
        Assert.That(KnowledgeGraphSystem.IsConnectionVisible(state, conn), Is.True);
    }

    [Test]
    public void Connection_RevealedWhenBothAnalyzed()
    {
        var state = MakeStateWithConnections();
        while (state.Tick < 50) state.AdvanceTick();

        // Upgrade both to Analyzed
        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Analyzed;
        state.Intel.Discoveries["disc_B"].Phase = DiscoveryPhase.Analyzed;

        KnowledgeGraphSystem.Process(state);
        var conn = state.Intel.KnowledgeConnections[0];
        Assert.That(conn.IsRevealed, Is.True);
        Assert.That(conn.RevealedTick, Is.EqualTo(50));
    }

    [Test]
    public void QuestionMarkCount_TracksUnrevealed()
    {
        var state = MakeStateWithConnections();

        // Both Seen → 1 question mark
        Assert.That(KnowledgeGraphSystem.GetQuestionMarkCount(state), Is.EqualTo(1));

        // Analyze both → revealed → 0 question marks
        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Analyzed;
        state.Intel.Discoveries["disc_B"].Phase = DiscoveryPhase.Analyzed;
        KnowledgeGraphSystem.Process(state);

        Assert.That(KnowledgeGraphSystem.GetQuestionMarkCount(state), Is.EqualTo(0));
    }

    [Test]
    public void Process_SkipsAlreadyRevealed()
    {
        var state = MakeStateWithConnections();
        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Analyzed;
        state.Intel.Discoveries["disc_B"].Phase = DiscoveryPhase.Analyzed;

        while (state.Tick < 50) state.AdvanceTick();
        KnowledgeGraphSystem.Process(state);
        var conn = state.Intel.KnowledgeConnections[0];
        Assert.That(conn.RevealedTick, Is.EqualTo(50));

        // Process again at different tick — should not overwrite
        while (state.Tick < 100) state.AdvanceTick();
        KnowledgeGraphSystem.Process(state);
        Assert.That(conn.RevealedTick, Is.EqualTo(50));
    }

    [Test]
    public void Process_EmptyConnections_NoException()
    {
        var state = new SimState(42);
        KnowledgeGraphSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void MultipleConnections_IndependentReveal()
    {
        var state = MakeStateWithConnections();

        // Add a third discovery and second connection
        state.Intel.Discoveries["disc_C"] = new DiscoveryStateV0
        {
            DiscoveryId = "disc_C",
            Phase = DiscoveryPhase.Analyzed
        };
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "conn_2",
            SourceDiscoveryId = "disc_A",
            TargetDiscoveryId = "disc_C",
            ConnectionType = KnowledgeConnectionType.Lead,
            Description = "A leads to C.",
            IsRevealed = false
        });

        // Only disc_A is Seen, disc_C is Analyzed → conn_2 not revealed yet (A not Analyzed)
        KnowledgeGraphSystem.Process(state);
        Assert.That(state.Intel.KnowledgeConnections[0].IsRevealed, Is.False); // conn_1
        Assert.That(state.Intel.KnowledgeConnections[1].IsRevealed, Is.False); // conn_2

        // Upgrade disc_A to Analyzed → conn_2 reveals (both Analyzed), conn_1 still needs disc_B
        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Analyzed;
        KnowledgeGraphSystem.Process(state);
        Assert.That(state.Intel.KnowledgeConnections[0].IsRevealed, Is.False);
        Assert.That(state.Intel.KnowledgeConnections[1].IsRevealed, Is.True);
    }

    [Test]
    public void Connection_ScannedPhaseIsVisible()
    {
        var state = MakeStateWithConnections();
        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Scanned;
        state.Intel.Discoveries["disc_B"].Phase = DiscoveryPhase.Scanned;

        var conn = state.Intel.KnowledgeConnections[0];
        Assert.That(KnowledgeGraphSystem.IsConnectionVisible(state, conn), Is.True);
        KnowledgeGraphSystem.Process(state);
        Assert.That(conn.IsRevealed, Is.False);
    }

    [Test]
    public void Connection_MixedScannedAnalyzed_NotRevealed()
    {
        var state = MakeStateWithConnections();
        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Scanned;
        state.Intel.Discoveries["disc_B"].Phase = DiscoveryPhase.Analyzed;

        KnowledgeGraphSystem.Process(state);
        Assert.That(state.Intel.KnowledgeConnections[0].IsRevealed, Is.False);
        Assert.That(KnowledgeGraphSystem.IsConnectionVisible(state,
            state.Intel.KnowledgeConnections[0]), Is.True);
    }

    [Test]
    public void QuestionMarkCount_MultipleConnections()
    {
        var state = MakeStateWithConnections();
        state.Intel.Discoveries["disc_C"] = new DiscoveryStateV0
        {
            DiscoveryId = "disc_C", Phase = DiscoveryPhase.Seen
        };
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "conn_2",
            SourceDiscoveryId = "disc_A",
            TargetDiscoveryId = "disc_C",
            ConnectionType = KnowledgeConnectionType.Lead,
            Description = "A leads to C.",
            IsRevealed = false
        });

        Assert.That(KnowledgeGraphSystem.GetQuestionMarkCount(state), Is.EqualTo(2));

        state.Intel.Discoveries["disc_A"].Phase = DiscoveryPhase.Analyzed;
        state.Intel.Discoveries["disc_C"].Phase = DiscoveryPhase.Analyzed;
        KnowledgeGraphSystem.Process(state);
        Assert.That(KnowledgeGraphSystem.GetQuestionMarkCount(state), Is.EqualTo(1));
    }
}
