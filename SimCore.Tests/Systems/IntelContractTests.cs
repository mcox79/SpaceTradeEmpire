using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Programs;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class IntelContractTests
{
    [Test]
    public void LocalTruth_IsExact_AndAgeIsZero()
    {
        var state = new SimState(123);

        var mktA = new Market { Id = "mkt_a" };
        mktA.Inventory["ore"] = 42;
        state.Markets["mkt_a"] = mktA;

        // Player is local at mkt_a (for Slice 1 we treat node id == market id)
        state.PlayerLocationNodeId = "mkt_a";

        MarketSystem.Process(state);
        IntelSystem.Process(state);

        var view = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_a", goodId: "ore");

        Assert.That(view.Kind, Is.EqualTo(MarketGoodViewKind.LocalTruth));
        Assert.That(view.ExactInventoryQty, Is.EqualTo(42));
        Assert.That(view.AgeTicks, Is.EqualTo(0));
        Assert.That(view.InventoryBand, Is.EqualTo(InventoryBand.Unknown));
    }

    [Test]
    public void RemoteIntel_IsUnknown_WhenNeverObserved()
    {
        var state = new SimState(42);

        var mktA = new Market { Id = "mkt_a" };
        var mktB = new Market { Id = "mkt_b" };
        mktA.Inventory["ore"] = 10;
        mktB.Inventory["ore"] = 10;
        state.Markets["mkt_a"] = mktA;
        state.Markets["mkt_b"] = mktB;

        state.PlayerLocationNodeId = "mkt_a";

        MarketSystem.Process(state);
        IntelSystem.Process(state);

        var view = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

        Assert.That(view.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
        Assert.That(view.InventoryBand, Is.EqualTo(InventoryBand.Unknown));
        Assert.That(view.AgeTicks, Is.EqualTo(-1));
    }

    [Test]
    public void RemoteIntel_IsBanded_AndAgeIncrements_WhenNotReobserved()
    {
        var state = new SimState(777);

        var mktA = new Market { Id = "mkt_a" };
        var mktB = new Market { Id = "mkt_b" };
        mktA.Inventory["ore"] = 10;
        mktB.Inventory["ore"] = 10;
        state.Markets["mkt_a"] = mktA;
        state.Markets["mkt_b"] = mktB;

        state.PlayerLocationNodeId = "mkt_b";

        MarketSystem.Process(state);
        IntelSystem.Process(state);

        // Move away so mkt_b is no longer being refreshed by local observation
        state.PlayerLocationNodeId = "mkt_a";

        // At this point, intel for mkt_b should exist (observed when local), but now it should age.
        var v0 = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

        Assert.That(v0.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
        Assert.That(v0.InventoryBand, Is.Not.EqualTo(InventoryBand.Unknown));
        Assert.That(v0.AgeTicks, Is.EqualTo(0));

        for (int i = 0; i < 5; i++)
        {
            state.AdvanceTick();
            MarketSystem.Process(state);
            IntelSystem.Process(state);
        }

        var v5 = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

        Assert.That(v5.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
        Assert.That(v5.InventoryBand, Is.EqualTo(v0.InventoryBand));
        Assert.That(v5.AgeTicks, Is.EqualTo(5));
    }

    // GATE.S3_6.DISCOVERY_STATE.001: DiscoveryState contract tests

    [Test]
    public void DiscoveryPhase_HasRequiredValues()
    {
        Assert.That((int)DiscoveryPhase.Seen, Is.EqualTo(0));
        Assert.That((int)DiscoveryPhase.Scanned, Is.EqualTo(1));
        Assert.That((int)DiscoveryPhase.Analyzed, Is.EqualTo(2));
    }

    [Test]
    public void DiscoveryReasonCode_HasRequiredValues()
    {
        Assert.That((int)DiscoveryReasonCode.Ok, Is.EqualTo(0));
        Assert.That((int)DiscoveryReasonCode.NotSeen, Is.EqualTo(1));
        Assert.That((int)DiscoveryReasonCode.AlreadyAnalyzed, Is.EqualTo(2));
        Assert.That((int)DiscoveryReasonCode.OffHub, Is.EqualTo(3));
        Assert.That((int)DiscoveryReasonCode.NotScanned, Is.EqualTo(4));
    }

    [Test]
    public void DiscoveryScan_ReasonCode_NotSeen_WhenNotInBook()
    {
        var state = new SimState(1);
        Assert.That(IntelSystem.GetScanReasonCode(state, "disc_unknown"), Is.EqualTo(DiscoveryReasonCode.NotSeen));
        Assert.That(IntelSystem.GetAnalyzeReasonCode(state, "disc_unknown"), Is.EqualTo(DiscoveryReasonCode.NotSeen));
    }

    [Test]
    public void DiscoveryScan_ReasonCode_AlreadyAnalyzed_WhenAtMaxPhase()
    {
        var state = new SimState(2);
        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Analyzed };
        Assert.That(IntelSystem.GetScanReasonCode(state, "disc_a"), Is.EqualTo(DiscoveryReasonCode.AlreadyAnalyzed));
        Assert.That(IntelSystem.GetAnalyzeReasonCode(state, "disc_a"), Is.EqualTo(DiscoveryReasonCode.AlreadyAnalyzed));
    }

    [Test]
    public void DiscoveryListing_StableOrdering_ByDiscoveryIdAsc()
    {
        var state = new SimState(3);
        state.Intel.Discoveries["disc_003"] = new DiscoveryStateV0 { DiscoveryId = "disc_003", Phase = DiscoveryPhase.Seen };
        state.Intel.Discoveries["disc_001"] = new DiscoveryStateV0 { DiscoveryId = "disc_001", Phase = DiscoveryPhase.Analyzed };
        state.Intel.Discoveries["disc_002"] = new DiscoveryStateV0 { DiscoveryId = "disc_002", Phase = DiscoveryPhase.Scanned };

        var list = IntelSystem.GetDiscoveriesAscending(state);

        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0].DiscoveryId, Is.EqualTo("disc_001"));
        Assert.That(list[1].DiscoveryId, Is.EqualTo("disc_002"));
        Assert.That(list[2].DiscoveryId, Is.EqualTo("disc_003"));
    }

    // GATE.S3_6.DISCOVERY_STATE.002: entering node marks Seen and emits deterministic transition events.

    [Test]
    public void DiscoverySeen_OnArrival_IsIdempotent_AndEventsAreDiscoveryIdAsc()
    {
        var state = new SimState(4242);

        // Nodes
        state.Nodes["a"] = new Node { Id = "a", Name = "A" };
        state.Nodes["b"] = new Node
        {
            Id = "b",
            Name = "B",
            SeededDiscoveryIds = new List<string> { "disc_002", "disc_001", "disc_001" } // intentionally unsorted + duplicate
        };

        // Edge for arrival bookkeeping
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "a", ToNodeId = "b", Distance = 1f, TotalCapacity = 10, UsedCapacity = 1 };

        // Fleet already traveling and will arrive this tick
        state.Fleets["f1"] = new Fleet
        {
            Id = "f1",
            CurrentNodeId = "a",
            State = FleetState.Traveling,
            CurrentEdgeId = "e1",
            DestinationNodeId = "b",
            TravelProgress = 1.0f,
            Speed = 1f
        };

        MovementSystem.Process(state);

        Assert.That(state.Intel.Discoveries.ContainsKey("disc_001"), Is.True);
        Assert.That(state.Intel.Discoveries.ContainsKey("disc_002"), Is.True);
        Assert.That(state.Intel.Discoveries["disc_001"].Phase, Is.EqualTo(DiscoveryPhase.Seen));
        Assert.That(state.Intel.Discoveries["disc_002"].Phase, Is.EqualTo(DiscoveryPhase.Seen));

        // Two events emitted in DiscoveryId asc order.
        Assert.That(state.FleetEventLog.Count, Is.GreaterThanOrEqualTo(2));
        var evs = state.FleetEventLog.Where(e => e.Type == SimCore.Events.FleetEvents.FleetEventType.DiscoverySeen).ToList();
        Assert.That(evs.Count, Is.EqualTo(2));
        Assert.That(evs[0].DiscoveryId, Is.EqualTo("disc_001"));
        Assert.That(evs[1].DiscoveryId, Is.EqualTo("disc_002"));

        // Re-enter again: should not emit again (idempotent).
        state.Edges["e2"] = new Edge { Id = "e2", FromNodeId = "a", ToNodeId = "b", Distance = 1f, TotalCapacity = 10, UsedCapacity = 1 };
        var f = state.Fleets["f1"];
        f.CurrentNodeId = "a";
        f.State = FleetState.Traveling;
        f.CurrentEdgeId = "e2";
        f.DestinationNodeId = "b";
        f.TravelProgress = 1.0f;

        MovementSystem.Process(state);

        var evs2 = state.FleetEventLog.Where(e => e.Type == SimCore.Events.FleetEvents.FleetEventType.DiscoverySeen).ToList();
        Assert.That(evs2.Count, Is.EqualTo(2));
    }

    // GATE.S3_6.DISCOVERY_STATE.003: Scan intent core v0 (Seen->Scanned) with deterministic rejection.

    [Test]
    public void DiscoveryScan_ReasonCode_NotSeen_WhenNotInSeenPhase()
    {
        var state = new SimState(7);
        state.Intel.Discoveries["disc_s"] = new DiscoveryStateV0 { DiscoveryId = "disc_s", Phase = DiscoveryPhase.Scanned };

        Assert.That(IntelSystem.GetScanReasonCode(state, "disc_s"), Is.EqualTo(DiscoveryReasonCode.NotSeen));
    }

    [Test]
    public void DiscoveryScan_ApplyScan_TransitionsSeenToScanned_WhenOk()
    {
        var state = new SimState(8);
        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Seen };

        var rc = IntelSystem.ApplyScan(state, fleetId: "f1", discoveryId: "disc_a");

        Assert.That(rc, Is.EqualTo(DiscoveryReasonCode.Ok));
        Assert.That(state.Intel.Discoveries["disc_a"].Phase, Is.EqualTo(DiscoveryPhase.Scanned));
    }

    [Test]
    public void DiscoveryScan_ApplyScan_IsNoop_WhenRejected()
    {
        var state = new SimState(9);
        state.Intel.Discoveries["disc_x"] = new DiscoveryStateV0 { DiscoveryId = "disc_x", Phase = DiscoveryPhase.Scanned };

        var rc = IntelSystem.ApplyScan(state, fleetId: "f1", discoveryId: "disc_x");

        Assert.That(rc, Is.EqualTo(DiscoveryReasonCode.NotSeen));
        Assert.That(state.Intel.Discoveries["disc_x"].Phase, Is.EqualTo(DiscoveryPhase.Scanned));
    }

    // GATE.S3_6.DISCOVERY_STATE.004: Analyze intent core v0 (Scanned->Analyzed) hub-only with deterministic rejection and outcome event stub.

    [Test]
    public void DiscoveryAnalyze_ApplyAnalyze_TransitionsScannedToAnalyzed_WhenAtHub()
    {
        var state = new SimState(10);
        state.PlayerLocationNodeId = "hub";

        state.Nodes["hub"] = new Node { Id = "hub", Name = "Hub" };
        state.Nodes["x"] = new Node { Id = "x", Name = "X" };

        state.Fleets["f1"] = new Fleet { Id = "f1", CurrentNodeId = "hub", State = FleetState.Idle };

        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Scanned };

        var rc = IntelSystem.ApplyAnalyze(state, fleetId: "f1", discoveryId: "disc_a");

        Assert.That(rc, Is.EqualTo(DiscoveryReasonCode.Ok));
        Assert.That(state.Intel.Discoveries["disc_a"].Phase, Is.EqualTo(DiscoveryPhase.Analyzed));

        var ev = state.FleetEventLog.Last(e => e.Type == SimCore.Events.FleetEvents.FleetEventType.DiscoveryAnalysisOutcome);
        Assert.That(ev.FleetId, Is.EqualTo("f1"));
        Assert.That(ev.DiscoveryId, Is.EqualTo("disc_a"));
        Assert.That(ev.NodeId, Is.EqualTo("hub"));
        Assert.That(ev.ReasonCode, Is.EqualTo((int)DiscoveryReasonCode.Ok));
        Assert.That(ev.PhaseAfter, Is.EqualTo((int)DiscoveryPhase.Analyzed));
    }

    [Test]
    public void DiscoveryAnalyze_ApplyAnalyze_IsNoop_WhenOffHub()
    {
        var state = new SimState(11);
        state.PlayerLocationNodeId = "hub";

        state.Nodes["hub"] = new Node { Id = "hub", Name = "Hub" };
        state.Nodes["x"] = new Node { Id = "x", Name = "X" };

        state.Fleets["f1"] = new Fleet { Id = "f1", CurrentNodeId = "x", State = FleetState.Idle };

        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Scanned };

        var rc = IntelSystem.ApplyAnalyze(state, fleetId: "f1", discoveryId: "disc_a");

        Assert.That(rc, Is.EqualTo(DiscoveryReasonCode.OffHub));
        Assert.That(state.Intel.Discoveries["disc_a"].Phase, Is.EqualTo(DiscoveryPhase.Scanned));

        var ev = state.FleetEventLog.Last(e => e.Type == SimCore.Events.FleetEvents.FleetEventType.DiscoveryAnalysisOutcome);
        Assert.That(ev.FleetId, Is.EqualTo("f1"));
        Assert.That(ev.DiscoveryId, Is.EqualTo("disc_a"));
        Assert.That(ev.NodeId, Is.EqualTo("x"));
        Assert.That(ev.ReasonCode, Is.EqualTo((int)DiscoveryReasonCode.OffHub));
        Assert.That(ev.PhaseAfter, Is.EqualTo((int)DiscoveryPhase.Scanned));
    }

    [Test]
    public void DiscoveryAnalyze_ApplyAnalyze_IsNoop_WhenNotScanned()
    {
        var state = new SimState(12);
        state.PlayerLocationNodeId = "hub";

        state.Nodes["hub"] = new Node { Id = "hub", Name = "Hub" };
        state.Fleets["f1"] = new Fleet { Id = "f1", CurrentNodeId = "hub", State = FleetState.Idle };

        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Seen };

        var rc = IntelSystem.ApplyAnalyze(state, fleetId: "f1", discoveryId: "disc_a");

        Assert.That(rc, Is.EqualTo(DiscoveryReasonCode.NotScanned));
        Assert.That(state.Intel.Discoveries["disc_a"].Phase, Is.EqualTo(DiscoveryPhase.Seen));

        var ev = state.FleetEventLog.Last(e => e.Type == SimCore.Events.FleetEvents.FleetEventType.DiscoveryAnalysisOutcome);
        Assert.That(ev.FleetId, Is.EqualTo("f1"));
        Assert.That(ev.DiscoveryId, Is.EqualTo("disc_a"));
        Assert.That(ev.NodeId, Is.EqualTo(""));
        Assert.That(ev.ReasonCode, Is.EqualTo((int)DiscoveryReasonCode.NotScanned));
        Assert.That(ev.PhaseAfter, Is.EqualTo((int)DiscoveryPhase.Seen));
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001: Unlock contract tests

    [Test]
    public void UnlockKind_HasRequiredValues()
    {
        Assert.That((int)UnlockKind.Permit, Is.EqualTo(0));
        Assert.That((int)UnlockKind.Broker, Is.EqualTo(1));
        Assert.That((int)UnlockKind.Recipe, Is.EqualTo(2));
        Assert.That((int)UnlockKind.SiteBlueprint, Is.EqualTo(3));
        Assert.That((int)UnlockKind.CorridorAccess, Is.EqualTo(4));
        Assert.That((int)UnlockKind.SensorLayer, Is.EqualTo(5));
    }

    [Test]
    public void UnlockReasonCode_HasRequiredValues()
    {
        Assert.That((int)UnlockReasonCode.Ok, Is.EqualTo(0));
        Assert.That((int)UnlockReasonCode.NotKnown, Is.EqualTo(1));
        Assert.That((int)UnlockReasonCode.AlreadyAcquired, Is.EqualTo(2));
        Assert.That((int)UnlockReasonCode.Blocked, Is.EqualTo(3));
    }

    [Test]
    public void UnlockAcquire_ReasonCode_NotKnown_WhenNotInBook()
    {
        var state = new SimState(11);
        Assert.That(IntelSystem.GetAcquireUnlockReasonCode(state, "unlock_unknown"), Is.EqualTo(UnlockReasonCode.NotKnown));
    }

    [Test]
    public void UnlockAcquire_ReasonCode_AlreadyAcquired_WhenAcquired()
    {
        var state = new SimState(12);
        state.Intel.Unlocks["u1"] = new UnlockContractV0 { UnlockId = "u1", Kind = UnlockKind.Permit, IsAcquired = true, IsBlocked = false };

        Assert.That(IntelSystem.GetAcquireUnlockReasonCode(state, "u1"), Is.EqualTo(UnlockReasonCode.AlreadyAcquired));
    }

    [Test]
    public void UnlockAcquire_ReasonCode_Blocked_WhenBlockedAndNotAcquired()
    {
        var state = new SimState(13);
        state.Intel.Unlocks["u2"] = new UnlockContractV0 { UnlockId = "u2", Kind = UnlockKind.Recipe, IsAcquired = false, IsBlocked = true };

        Assert.That(IntelSystem.GetAcquireUnlockReasonCode(state, "u2"), Is.EqualTo(UnlockReasonCode.Blocked));
    }

    [Test]
    public void UnlockListing_StableOrdering_ByUnlockIdAsc()
    {
        var state = new SimState(14);
        state.Intel.Unlocks["unlock_003"] = new UnlockContractV0 { UnlockId = "unlock_003", Kind = UnlockKind.SensorLayer, IsAcquired = false, IsBlocked = false };
        state.Intel.Unlocks["unlock_001"] = new UnlockContractV0 { UnlockId = "unlock_001", Kind = UnlockKind.Permit, IsAcquired = true, IsBlocked = false };
        state.Intel.Unlocks["unlock_002"] = new UnlockContractV0 { UnlockId = "unlock_002", Kind = UnlockKind.CorridorAccess, IsAcquired = false, IsBlocked = true };

        var list = IntelSystem.GetUnlocksAscending(state);

        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0].UnlockId, Is.EqualTo("unlock_001"));
        Assert.That(list[1].UnlockId, Is.EqualTo("unlock_002"));
        Assert.That(list[2].UnlockId, Is.EqualTo("unlock_003"));
    }

    [Test]
    public void UnlockExplainability_Payload_IsSchemaBound_AndUnlocksAreUnlockIdAsc()
    {
        var state = new SimState(1515);

        state.Intel.Unlocks["unlock_003"] = new UnlockContractV0 { UnlockId = "unlock_003", Kind = UnlockKind.SensorLayer, IsAcquired = false, IsBlocked = false };
        state.Intel.Unlocks["unlock_001"] = new UnlockContractV0 { UnlockId = "unlock_001", Kind = UnlockKind.Permit, IsAcquired = true, IsBlocked = false };
        state.Intel.Unlocks["unlock_002"] = new UnlockContractV0 { UnlockId = "unlock_002", Kind = UnlockKind.CorridorAccess, IsAcquired = false, IsBlocked = true };

        var payload = IntelSystem.BuildUnlockExplainPayload(state);
        var json = ProgramExplain.ToDeterministicJson(payload);

        ProgramExplain.ValidateUnlockJsonIsSchemaBound(json);

        Assert.That(payload.Unlocks.Count, Is.EqualTo(3));
        Assert.That(payload.Unlocks[0].UnlockId, Is.EqualTo("unlock_001"));
        Assert.That(payload.Unlocks[1].UnlockId, Is.EqualTo("unlock_002"));
        Assert.That(payload.Unlocks[2].UnlockId, Is.EqualTo("unlock_003"));
    }

    [Test]
    public void UnlockExplainability_ReasonTokens_AndActions_AreDeterministic_AndBounded()
    {
        var state = new SimState(1616);

        state.Intel.Unlocks["u_ok"] = new UnlockContractV0 { UnlockId = "u_ok", Kind = UnlockKind.Permit, IsAcquired = false, IsBlocked = false };
        state.Intel.Unlocks["u_blocked"] = new UnlockContractV0 { UnlockId = "u_blocked", Kind = UnlockKind.Recipe, IsAcquired = false, IsBlocked = true };
        state.Intel.Unlocks["u_acquired"] = new UnlockContractV0 { UnlockId = "u_acquired", Kind = UnlockKind.Broker, IsAcquired = true, IsBlocked = false };

        var payload = IntelSystem.BuildUnlockExplainPayload(state);

        var ok = payload.Unlocks.Single(e => e.UnlockId == "u_ok");
        Assert.That(ok.AcquireReasonCode, Is.EqualTo(IntelSystem.UnlockReasonToken_Ok));
        Assert.That(ok.Actions.Count, Is.InRange(1, 3));
        Assert.That(ok.Actions[0], Is.EqualTo(IntelSystem.UnlockActionToken_Acquire));

        var blocked = payload.Unlocks.Single(e => e.UnlockId == "u_blocked");
        Assert.That(blocked.AcquireReasonCode, Is.EqualTo(IntelSystem.UnlockReasonToken_Blocked));
        Assert.That(blocked.Actions.Count, Is.InRange(1, 3));
        Assert.That(blocked.Actions[0], Is.EqualTo(IntelSystem.UnlockActionToken_SatisfyPrereqs));
        Assert.That(blocked.Actions[1], Is.EqualTo(IntelSystem.UnlockActionToken_CheckIntel));

        var acquired = payload.Unlocks.Single(e => e.UnlockId == "u_acquired");
        Assert.That(acquired.AcquireReasonCode, Is.EqualTo(IntelSystem.UnlockReasonToken_AlreadyAcquired));
        Assert.That(acquired.Actions.Count, Is.InRange(1, 3));
        Assert.That(acquired.Actions[0], Is.EqualTo(IntelSystem.UnlockActionToken_Use));

        // ExplainChain is tokens only and stable ordered (root then reason then optional flag).
        Assert.That(blocked.ExplainChain.Count, Is.InRange(2, 3));
        Assert.That(blocked.ExplainChain[0], Is.EqualTo(IntelSystem.UnlockChainToken_ExplainRoot));
        Assert.That(blocked.ExplainChain[1], Is.EqualTo(IntelSystem.UnlockReasonToken_Blocked));
    }

    [Test]
    public void RemoteIntel_RemainsStale_WhenTruthChanges_UntilReobserved()
    {
        var state = new SimState(9001);

        var mktA = new Market { Id = "mkt_a" };
        var mktB = new Market { Id = "mkt_b" };
        mktA.Inventory["ore"] = 10;
        mktB.Inventory["ore"] = 10;
        state.Markets["mkt_a"] = mktA;
        state.Markets["mkt_b"] = mktB;

        // Observe B by being local there
        state.PlayerLocationNodeId = "mkt_b";
        MarketSystem.Process(state);
        IntelSystem.Process(state);

        // Move away
        state.PlayerLocationNodeId = "mkt_a";

        var vObs = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");
        Assert.That(vObs.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
        Assert.That(vObs.InventoryBand, Is.Not.EqualTo(InventoryBand.Unknown));
        Assert.That(vObs.AgeTicks, Is.EqualTo(0));

        // Mutate truth at B while away
        mktB.Inventory["ore"] = 999;

        // Tick forward without re-observing B
        for (int i = 0; i < 3; i++)
        {
            state.AdvanceTick();
            MarketSystem.Process(state);
            IntelSystem.Process(state);
        }

        var vStale = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

        Assert.That(vStale.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
        Assert.That(vStale.InventoryBand, Is.EqualTo(vObs.InventoryBand));
        Assert.That(vStale.AgeTicks, Is.EqualTo(3));

        // Re-observe by being local again
        state.PlayerLocationNodeId = "mkt_b";
        state.AdvanceTick();
        MarketSystem.Process(state);
        IntelSystem.Process(state);

        var vFresh = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

        Assert.That(vFresh.Kind, Is.EqualTo(MarketGoodViewKind.LocalTruth));
        Assert.That(vFresh.ExactInventoryQty, Is.EqualTo(999));
        Assert.That(vFresh.AgeTicks, Is.EqualTo(0));
    }
}
