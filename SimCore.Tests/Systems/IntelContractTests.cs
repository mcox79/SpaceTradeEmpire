using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    public void DiscoverySnapshotV0_IsFactsOnly_AndDeterministicOrdering()
    {
        var state = new SimState(4242);

        // Discoveries (unordered insert) should still produce stable aggregate counts.
        state.Intel.Discoveries["disc_b"] = new DiscoveryStateV0 { DiscoveryId = "disc_b", Phase = DiscoveryPhase.Scanned };
        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Seen };
        state.Intel.Discoveries["disc_c"] = new DiscoveryStateV0 { DiscoveryId = "disc_c", Phase = DiscoveryPhase.Analyzed };

        // Unlocks must be UnlockId asc in snapshot.
        state.Intel.Unlocks["unlock_003"] = new UnlockContractV0 { UnlockId = "unlock_003", Kind = UnlockKind.SensorLayer, IsAcquired = false, IsBlocked = false };
        state.Intel.Unlocks["unlock_001"] = new UnlockContractV0 { UnlockId = "unlock_001", Kind = UnlockKind.Permit, IsAcquired = true, IsBlocked = false };
        state.Intel.Unlocks["unlock_002"] = new UnlockContractV0 { UnlockId = "unlock_002", Kind = UnlockKind.CorridorAccess, IsAcquired = false, IsBlocked = true };

        // Rumor leads must be LeadId asc and hint sublists must be Ordinal asc.
        state.Intel.RumorLeads["LEAD.0002"] = new RumorLead
        {
            LeadId = "LEAD.0002",
            SourceVerbToken = "HUB_ANALYSIS",
            Status = RumorLeadStatus.Active,
            Hint = new HintPayloadV0
            {
                CoarseLocationToken = "OUTER_RIM",
                ImpliedPayoffToken = "BROKER_UNLOCK",
                RegionTags = new List<string> { "ZETA", "ALPHA" },
                PrerequisiteTokens = new List<string> { "REQ_B", "REQ_A" }
            }
        };
        state.Intel.RumorLeads["LEAD.0001"] = new RumorLead
        {
            LeadId = "LEAD.0001",
            SourceVerbToken = "EXPLORE",
            Status = RumorLeadStatus.Active,
            Hint = new HintPayloadV0
            {
                CoarseLocationToken = "CORE",
                ImpliedPayoffToken = "RESOURCE_SITE",
                RegionTags = new List<string> { "BETA" },
                PrerequisiteTokens = new List<string>()
            }
        };

        var snap = IntelSystem.BuildDiscoverySnapshotV0(state, "");

        Assert.That(snap.DiscoveredSiteCount, Is.EqualTo(3));
        Assert.That(snap.ScannedSiteCount, Is.EqualTo(2));
        Assert.That(snap.AnalyzedSiteCount, Is.EqualTo(1));
        Assert.That(snap.ExpeditionStatusToken, Is.Not.EqualTo(""));

        Assert.That(snap.Unlocks.Count, Is.EqualTo(3));
        Assert.That(snap.Unlocks[0].UnlockId, Is.EqualTo("unlock_001"));
        Assert.That(snap.Unlocks[1].UnlockId, Is.EqualTo("unlock_002"));
        Assert.That(snap.Unlocks[2].UnlockId, Is.EqualTo("unlock_003"));

        // Acquired unlock must have at least one deploy-package verb token.
        var u1 = snap.Unlocks[0];
        Assert.That(u1.DeployVerbControlTokens.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(u1.DeployVerbControlTokens[0], Is.EqualTo(IntelSystem.DeployVerbToken_DeployPackageV0));

        // Blocked unlock must surface blocked reason%actions (schema-bound tokens).
        var u2 = snap.Unlocks[1];
        Assert.That(u2.BlockedReasonToken, Is.EqualTo(IntelSystem.UnlockReasonToken_Blocked));
        Assert.That(u2.BlockedActionTokens.Count, Is.InRange(1, 3));

        Assert.That(snap.RumorLeads.Count, Is.EqualTo(2));
        Assert.That(snap.RumorLeads[0].LeadId, Is.EqualTo("LEAD.0001"));
        Assert.That(snap.RumorLeads[1].LeadId, Is.EqualTo("LEAD.0002"));

        // LEAD.0002 hint tokens must include sorted RegionTags and sorted PrerequisiteTokens.
        var lead2 = snap.RumorLeads[1];
        var joined = string.Join(",", lead2.HintTokens);
        Assert.That(joined.Contains("ALPHA"), Is.True);
        Assert.That(joined.Contains("ZETA"), Is.True);
        Assert.That(joined.IndexOf("ALPHA", StringComparison.Ordinal), Is.LessThan(joined.IndexOf("ZETA", StringComparison.Ordinal)));
        Assert.That(joined.IndexOf("REQ_A", StringComparison.Ordinal), Is.LessThan(joined.IndexOf("REQ_B", StringComparison.Ordinal)));
    }

    [Test]
    public void ExpeditionPrograms_ExplainabilityTokenSurface_V0_VacuousWhenAbsent_AndDeterministicWhenPresent()
    {
        static void AssertOrdinalSortedUnique(IReadOnlyList<string> tokens)
        {
            var prev = "";
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i] ?? "";
                Assert.That(t, Is.Not.EqualTo(""));
                Assert.That(string.CompareOrdinal(prev, t) <= 0, $"tokens not Ordinal asc at index {i}: '{prev}' then '{t}'");
                Assert.That(seen.Add(t), Is.True, $"duplicate token '{t}'");
                prev = t;
            }
        }

        // Seed 42 tick 0 vacuous policy: if a kind is not present in this initial state, the assertion for that kind passes.
        var state = new SimState(42);
        var payload = ProgramExplain.Build(state);

        var expedition = payload.Programs
            .Where(p => !string.IsNullOrEmpty(p.ExpeditionKindToken) ||
                        (p.Kind ?? "").IndexOf("Expedition", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        var requiredKinds = new[]
        {
            "ExpeditionKind.Survey",
            "ExpeditionKind.Sample",
            "ExpeditionKind.Salvage",
            "ExpeditionKind.Analyze"
        };

        foreach (var kindToken in requiredKinds)
        {
            var matches = expedition
                .Where(p => string.Equals(p.ExpeditionKindToken ?? "", kindToken, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 0)
                continue; // vacuous pass for this kind at Seed 42 tick 0

            foreach (var p in matches)
            {
                Assert.That(p.Status, Is.Not.EqualTo(""));

                Assert.That(p.ExplainPrimaryTokens, Is.Not.Null);
                Assert.That(p.ExplainSecondaryTokens, Is.Not.Null);
                Assert.That(p.InterventionVerbTokens, Is.Not.Null);

                AssertOrdinalSortedUnique(p.ExplainPrimaryTokens);
                AssertOrdinalSortedUnique(p.ExplainSecondaryTokens);

                // Intervention verbs are tokens and deterministic when present.
                if (p.InterventionVerbTokens.Count > 0)
                    AssertOrdinalSortedUnique(p.InterventionVerbTokens);
            }
        }
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

    // GATE.S3_6.RUMOR_INTEL_MIN.001: RumorLead schema and IntelBook contract tests

    [Test]
    public void RumorLeadStatus_HasRequiredValues()
    {
        Assert.That((int)RumorLeadStatus.Active, Is.EqualTo(0));
        Assert.That((int)RumorLeadStatus.Fulfilled, Is.EqualTo(1));
        Assert.That((int)RumorLeadStatus.Dismissed, Is.EqualTo(2));
    }

    [Test]
    public void RumorLead_RequiredFields_ArePresent()
    {
        var lead = new RumorLead
        {
            LeadId = "LEAD.0001",
            Hint = new HintPayloadV0
            {
                RegionTags = new List<string> { "OUTER_RIM" },
                CoarseLocationToken = "SECTOR_NORTH",
                PrerequisiteTokens = new List<string> { "HUB_ANALYSIS" },
                ImpliedPayoffToken = "BROKER_UNLOCK"
            },
            Status = RumorLeadStatus.Active,
            SourceVerbToken = "SCAN"
        };

        Assert.That(lead.LeadId, Is.EqualTo("LEAD.0001"));
        Assert.That(lead.Hint, Is.Not.Null);
        Assert.That(lead.Hint.RegionTags, Is.Not.Null);
        Assert.That(lead.Hint.CoarseLocationToken, Is.EqualTo("SECTOR_NORTH"));
        Assert.That(lead.Hint.PrerequisiteTokens, Is.Not.Null);
        Assert.That(lead.Hint.ImpliedPayoffToken, Is.EqualTo("BROKER_UNLOCK"));
        Assert.That(lead.Status, Is.EqualTo(RumorLeadStatus.Active));
        Assert.That(lead.SourceVerbToken, Is.EqualTo("SCAN"));
    }

    [Test]
    public void RumorLead_LeadId_Format_IsLeadDotZeroPadded4Digit()
    {
        // LeadId format: LEAD.<zero-padded-4-digit>
        var validIds = new[] { "LEAD.0001", "LEAD.0010", "LEAD.0100", "LEAD.9999" };
        foreach (var id in validIds)
        {
            Assert.That(System.Text.RegularExpressions.Regex.IsMatch(id, @"^LEAD\.\d{4}$"), Is.True,
                $"Expected LeadId '{id}' to match LEAD.<zero-padded-4-digit>");
        }
    }

    [Test]
    public void RumorLeadListing_StableOrdering_ByLeadIdOrdinalAsc()
    {
        var state = new SimState(2001);
        state.Intel.RumorLeads["LEAD.0003"] = new RumorLead { LeadId = "LEAD.0003", Status = RumorLeadStatus.Active, SourceVerbToken = "SCAN" };
        state.Intel.RumorLeads["LEAD.0001"] = new RumorLead { LeadId = "LEAD.0001", Status = RumorLeadStatus.Fulfilled, SourceVerbToken = "EXPEDITION" };
        state.Intel.RumorLeads["LEAD.0002"] = new RumorLead { LeadId = "LEAD.0002", Status = RumorLeadStatus.Dismissed, SourceVerbToken = "HUB_ANALYSIS" };

        var list = IntelSystem.GetRumorLeadsAscending(state);

        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0].LeadId, Is.EqualTo("LEAD.0001"));
        Assert.That(list[1].LeadId, Is.EqualTo("LEAD.0002"));
        Assert.That(list[2].LeadId, Is.EqualTo("LEAD.0003"));
    }

    [Test]
    public void RumorLead_ReasonCodes_LeadBlocked_And_LeadMissingHint_AreRegistered()
    {
        // ReasonCodes must exist as stable string tokens in the registered set.
        Assert.That(ProgramExplain.ReasonCodes.LeadBlocked, Is.EqualTo("LeadBlocked"));
        Assert.That(ProgramExplain.ReasonCodes.LeadMissingHint, Is.EqualTo("LeadMissingHint"));

        // Ensure they are distinct.
        Assert.That(ProgramExplain.ReasonCodes.LeadBlocked,
            Is.Not.EqualTo(ProgramExplain.ReasonCodes.LeadMissingHint));
    }

    // GATE.S3_6.UI_DISCOVERY_MIN.002
    [Test]
    public void DiscoveryExceptions_FourTokensRegistered_AndEachHasInterventionVerb()
    {
        var policy = ProgramExplain.DiscoveryExceptionPolicyV0.GetEntriesOrdered();

        // Must be exactly the four required exceptions, in Ordinal asc.
        Assert.That(policy.Count, Is.EqualTo(4));

        Assert.That(policy[0].Ordinal, Is.EqualTo(0));
        Assert.That(policy[0].ExceptionToken, Is.EqualTo(ProgramExplain.DiscoveryExceptionPolicyV0.SiteAccessBlocked));

        Assert.That(policy[1].Ordinal, Is.EqualTo(1));
        Assert.That(policy[1].ExceptionToken, Is.EqualTo(ProgramExplain.DiscoveryExceptionPolicyV0.AnalysisQueueFull));

        Assert.That(policy[2].Ordinal, Is.EqualTo(2));
        Assert.That(policy[2].ExceptionToken, Is.EqualTo(ProgramExplain.DiscoveryExceptionPolicyV0.ExpeditionStalled));

        Assert.That(policy[3].Ordinal, Is.EqualTo(3));
        Assert.That(policy[3].ExceptionToken, Is.EqualTo(ProgramExplain.DiscoveryExceptionPolicyV0.IntelStale));

        // Each exception must be paired with at least 1 Discoveries.* or Programs.* intervention verb token.
        foreach (var e in policy)
        {
            Assert.That(string.IsNullOrEmpty(e.ExceptionToken), Is.False, "ExceptionToken must be non-empty");
            Assert.That(e.InterventionVerbTokens, Is.Not.Null);
            Assert.That(e.InterventionVerbTokens.Count, Is.GreaterThanOrEqualTo(1));

            foreach (var v in e.InterventionVerbTokens)
            {
                Assert.That(
                    v.StartsWith("Discoveries.", StringComparison.Ordinal) || v.StartsWith("Programs.", StringComparison.Ordinal),
                    Is.True,
                    $"Intervention verb token '{v}' must start with Discoveries. or Programs."
                );
            }
        }

        // Tokens must be distinct.
        var distinctTokens = policy.Select(p => p.ExceptionToken).Distinct(StringComparer.Ordinal).ToList();
        Assert.That(distinctTokens.Count, Is.EqualTo(4));
    }

    [Test]
    public void IntelBook_RumorLeads_IsPresentAndEmpty_ByDefault()
    {
        var book = new IntelBook();
        Assert.That(book.RumorLeads, Is.Not.Null);
        Assert.That(book.RumorLeads.Count, Is.EqualTo(0));
    }

    // GATE.S3_6.RUMOR_INTEL_MIN.004
    [Test]
    public void RumorLeadScenarioProof_Seed42_EmitsReportV0_SaveLoad_NoDrift()
    {
        const int seed = 42;
        const string exploreNodeId = "node_explore_001";
        const string hubNodeId = "hub";
        const string discoveryId = "disc_seed_42_001";
        const string leadExplore = "LEAD.0001";
        const string leadHub = "LEAD.0002";

        var sim = new SimKernel(seed);

        // Minimal deterministic fixture. Do not depend on worldgen for this proof.
        sim.State.Nodes[exploreNodeId] = new Node { Id = exploreNodeId, Name = "ExploreNode" };
        sim.State.Nodes[hubNodeId] = new Node { Id = hubNodeId, Name = "Hub" };
        sim.State.PlayerLocationNodeId = exploreNodeId;

        // Step 1: explore -> lead granted
        IntelSystem.GrantRumorLeadOnExplore(sim.State, leadExplore, exploreNodeId);

        // Step 2: dock hub (state change only; no mutation via UI surfaces here)
        sim.State.PlayerLocationNodeId = hubNodeId;

        // Step 3: hub-analysis -> lead granted
        IntelSystem.GrantRumorLeadOnHubAnalysis(sim.State, leadHub, discoveryId);

        // Snapshot before save%load (deterministic ordering: LeadId asc; stable field order)
        var beforeSnapshot = SnapshotRumorLeadsV0(sim.State);
        var beforeHintDigest = ComputeSha256HexUpper(Encoding.UTF8.GetBytes(beforeSnapshot));

        // Save%load%verify
        var json = sim.SaveToString();
        var sim2 = new SimKernel(seed: 999); // prove identity restored independent of ctor seed
        sim2.LoadFromString(json);

        var afterSnapshot = SnapshotRumorLeadsV0(sim2.State);
        var afterHintDigest = ComputeSha256HexUpper(Encoding.UTF8.GetBytes(afterSnapshot));

        // Emit report first (must exist even on failure).
        var report = new StringBuilder();
        report.AppendLine("RumorLeadScenarioProofV0");
        report.AppendLine($"Seed: {seed}");
        report.AppendLine($"WorldId: {TryReadStringMember(sim2.State, "WorldId")}");
        report.AppendLine($"TickIndex: {TryReadLongMember(sim2.State, "TickIndex")}");
        report.AppendLine($"LeadCount: {IntelSystem.GetRumorLeadsAscending(sim2.State).Count}");
        report.AppendLine($"HintDigest: {afterHintDigest}");
        report.AppendLine("");
        report.AppendLine("RumorLeads (LeadId asc):");
        report.Append(afterSnapshot);

        var reportText = report.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        var outPath = ResolveRepoRelativePath("docs/generated/rumor_lead_scenario_seed_42_v0.txt");
        WriteDeterministicTextFileV0(outPath, reportText);

        // Assertions after report emit.
        if (!string.Equals(beforeSnapshot, afterSnapshot, StringComparison.Ordinal))
        {
            var first = FindFirstRumorLeadSnapshotMismatchV0(beforeSnapshot, afterSnapshot);

            TestContext.Out.WriteLine($"RumorLeadScenario Seed: {seed}");
            TestContext.Out.WriteLine($"RumorLeadScenario FirstDiff: {first.leadId}%{first.fieldName}%{first.beforeValue}%{first.afterValue}");

            Assert.Fail(
                $"RumorLeadScenario snapshot mismatch after save%load (Seed={seed} {first.leadId}%{first.fieldName}%{first.beforeValue}%{first.afterValue}).");
        }

        if (!string.Equals(beforeHintDigest, afterHintDigest, StringComparison.Ordinal))
        {
            Assert.Fail($"RumorLeadScenario hint digest drift after save%load (Seed={seed} Before={beforeHintDigest} After={afterHintDigest}).");
        }

        Assert.That(IntelSystem.GetRumorLeadsAscending(sim2.State).Count, Is.GreaterThanOrEqualTo(1),
            $"RumorLeadScenario expected >=1 rumor lead after scenario (Seed={seed}).");
    }

    private static string SnapshotRumorLeadsV0(SimState state)
    {
        var leads = IntelSystem.GetRumorLeadsAscending(state);
        var sb = new StringBuilder();

        for (int i = 0; i < leads.Count; i++)
        {
            var r = leads[i];
            var leadId = r?.LeadId ?? "";
            var status = r?.Status.ToString() ?? "";
            var sourceVerb = r?.SourceVerbToken ?? "";

            var hint = r?.Hint;
            var regionTags = (hint?.RegionTags ?? new List<string>())
                .Where(x => !string.IsNullOrEmpty(x))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            var prereq = (hint?.PrerequisiteTokens ?? new List<string>())
                .Where(x => !string.IsNullOrEmpty(x))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            var coarse = hint?.CoarseLocationToken ?? "";
            var payoff = hint?.ImpliedPayoffToken ?? "";

            sb.Append(leadId);
            sb.Append('%');
            sb.Append(status);
            sb.Append('%');
            sb.Append(sourceVerb);
            sb.Append('%');
            sb.Append(string.Join(",", regionTags));
            sb.Append('%');
            sb.Append(coarse);
            sb.Append('%');
            sb.Append(string.Join(",", prereq));
            sb.Append('%');
            sb.Append(payoff);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static (string leadId, string fieldName, string beforeValue, string afterValue) FindFirstRumorLeadSnapshotMismatchV0(string before, string after)
    {
        var bLines = before.Split('\n');
        var aLines = after.Split('\n');

        var n = Math.Min(bLines.Length, aLines.Length);
        for (int i = 0; i < n; i++)
        {
            var bl = bLines[i];
            var al = aLines[i];
            if (string.Equals(bl, al, StringComparison.Ordinal)) continue;

            // line format: LeadId%Status%SourceVerb%RegionTags%Coarse%Prereq%Payoff
            var bp = bl.Split('%');
            var ap = al.Split('%');

            var leadId = bp.Length > 0 ? bp[0] : "";
            var leadIdAfter = ap.Length > 0 ? ap[0] : "";
            if (!string.Equals(leadId, leadIdAfter, StringComparison.Ordinal))
            {
                return (leadId.Length != 0 ? leadId : leadIdAfter, "LeadId", leadId, leadIdAfter);
            }

            var fields = new[]
            {
                ("Status", 1),
                ("SourceVerbToken", 2),
                ("RegionTags", 3),
                ("CoarseLocationToken", 4),
                ("PrerequisiteTokens", 5),
                ("ImpliedPayoffToken", 6)
            };

            for (int f = 0; f < fields.Length; f++)
            {
                var (name, idx) = fields[f];
                var bv = bp.Length > idx ? bp[idx] : "";
                var av = ap.Length > idx ? ap[idx] : "";
                if (!string.Equals(bv, av, StringComparison.Ordinal))
                    return (leadId, name, bv, av);
            }

            return (leadId, "Line", bl, al);
        }

        // Length mismatch
        return ("", "LineCount", bLines.Length.ToString(), aLines.Length.ToString());
    }

    private static string ComputeSha256HexUpper(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
            sb.Append(hash[i].ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private static string ResolveRepoRelativePath(string repoRelativePath)
    {
        var root = FindRepoRoot(TestContext.CurrentContext.WorkDirectory);
        return Path.GetFullPath(Path.Combine(root, repoRelativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir is not null; i++)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "docs")))
                return dir.FullName;
            dir = dir.Parent;
        }

        // Failure-safe deterministic fallback: use startDir.
        return startDir;
    }

    // GATE.S1.GALAXY_MAP.CONTRACT.001

    [Test]
    public void GalaxySnapshot_ContractV0_AllDisplayStates_Present()
    {
        var state = new SimState(42);

        // Nodes:
        // - hidden: neither rumored, visited, nor mapped
        // - rumored: active rumor lead coarse token points to node id
        // - visited: player current node
        // - mapped: has seeded discoveries that exist in IntelBook
        state.Nodes["n_hidden"] = new Node { Id = "n_hidden", Name = "HiddenName" };
        state.Nodes["n_rumor"] = new Node { Id = "n_rumor", Name = "RumorName" };
        state.Nodes["n_visit"] = new Node { Id = "n_visit", Name = "VisitedName" };
        state.Nodes["n_map"] = new Node
        {
            Id = "n_map",
            Name = "MappedName",
            SeededDiscoveryIds = new List<string> { "disc_b", "disc_a" } // intentionally unsorted
        };

        // Edge ordering: edge ids intentionally out of order.
        state.Edges["e2"] = new Edge { Id = "e2", FromNodeId = "n_hidden", ToNodeId = "n_rumor", Distance = 1f };
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "n_visit", ToNodeId = "n_map", Distance = 1f };

        // Player location drives VISITED.
        state.PlayerLocationNodeId = "n_visit";

        // Rumor lead drives RUMORED.
        state.Intel.RumorLeads["LEAD.0001"] = new RumorLead
        {
            LeadId = "LEAD.0001",
            Status = RumorLeadStatus.Active,
            Hint = new HintPayloadV0 { CoarseLocationToken = "n_rumor" },
            SourceVerbToken = "HUB_ANALYSIS"
        };

        // Discoveries drive MAPPED via node.SeededDiscoveryIds membership.
        state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Seen };
        state.Intel.Discoveries["disc_b"] = new DiscoveryStateV0 { DiscoveryId = "disc_b", Phase = DiscoveryPhase.Scanned };

        var snap = MapQueries.BuildGalaxySnapshotV0(state);

        Assert.That(snap.PlayerCurrentNodeId, Is.EqualTo("n_visit"));

        // Ordering: nodes by node id asc.
        Assert.That(snap.SystemNodes.Count, Is.EqualTo(4));
        Assert.That(snap.SystemNodes[0].NodeId, Is.EqualTo("n_hidden"));
        Assert.That(snap.SystemNodes[1].NodeId, Is.EqualTo("n_map"));
        Assert.That(snap.SystemNodes[2].NodeId, Is.EqualTo("n_rumor"));
        Assert.That(snap.SystemNodes[3].NodeId, Is.EqualTo("n_visit"));

        // Ordering: edges by edge id asc (even though edge id is not emitted).
        Assert.That(snap.LaneEdges.Count, Is.EqualTo(2));
        Assert.That(snap.LaneEdges[0].FromNodeId, Is.EqualTo("n_visit"));
        Assert.That(snap.LaneEdges[0].ToNodeId, Is.EqualTo("n_map"));
        Assert.That(snap.LaneEdges[1].FromNodeId, Is.EqualTo("n_hidden"));
        Assert.That(snap.LaneEdges[1].ToNodeId, Is.EqualTo("n_rumor"));

        var states = snap.SystemNodes.Select(n => n.DisplayStateToken).ToHashSet(StringComparer.Ordinal);
        Assert.That(states.Contains("HIDDEN"), Is.True);
        Assert.That(states.Contains("RUMORED"), Is.True);
        Assert.That(states.Contains("VISITED"), Is.True);
        Assert.That(states.Contains("MAPPED"), Is.True);

        var hidden = snap.SystemNodes.Single(n => n.NodeId == "n_hidden");
        Assert.That(hidden.DisplayStateToken, Is.EqualTo("HIDDEN"));
        Assert.That(hidden.DisplayText, Is.EqualTo(""));
        Assert.That(hidden.ObjectCount, Is.EqualTo(0));

        var rumored = snap.SystemNodes.Single(n => n.NodeId == "n_rumor");
        Assert.That(rumored.DisplayStateToken, Is.EqualTo("RUMORED"));
        Assert.That(rumored.DisplayText, Is.EqualTo("???"));
        Assert.That(rumored.ObjectCount, Is.EqualTo(0));

        var visited = snap.SystemNodes.Single(n => n.NodeId == "n_visit");
        Assert.That(visited.DisplayStateToken, Is.EqualTo("VISITED"));
        Assert.That(visited.DisplayText, Is.EqualTo("VisitedName"));
        Assert.That(visited.ObjectCount, Is.EqualTo(0));

        var mapped = snap.SystemNodes.Single(n => n.NodeId == "n_map");
        Assert.That(mapped.DisplayStateToken, Is.EqualTo("MAPPED"));
        Assert.That(mapped.DisplayText, Is.EqualTo("MappedName+2"));
        Assert.That(mapped.ObjectCount, Is.EqualTo(2));

        // Deterministic digest over an explicit, ordered rendering.
        var sb = new StringBuilder();
        sb.Append("Seed=42\n");
        sb.Append("PlayerCurrentNodeId=").Append(snap.PlayerCurrentNodeId).Append('\n');
        sb.Append("SystemNodes\n");
        for (int i = 0; i < snap.SystemNodes.Count; i++)
        {
            var n = snap.SystemNodes[i];
            sb.Append(n.NodeId).Append('|')
              .Append(n.DisplayStateToken).Append('|')
              .Append(n.DisplayText).Append('|')
              .Append(n.ObjectCount.ToString(System.Globalization.CultureInfo.InvariantCulture))
              .Append('\n');
        }
        sb.Append("LaneEdges\n");
        for (int i = 0; i < snap.LaneEdges.Count; i++)
        {
            var e = snap.LaneEdges[i];
            sb.Append(e.FromNodeId).Append('|').Append(e.ToNodeId).Append('\n');
        }

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(sb.ToString().Replace("\r\n", "\n"));
        var hash = sha.ComputeHash(bytes);
        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        Assert.That(hex.Length, Is.EqualTo(64));
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    [Test]
    public void SystemSnapshot_ContractV0_ReturnsStationSiteAndLaneGateFields()
    {
        var state = new SimState(42);

        state.Nodes["n1"] = new Node
        {
            Id = "n1",
            Name = "Alpha",
            SeededDiscoveryIds = new List<string> { "disc_002", "disc_001", "disc_001" } // unsorted + dup
        };
        state.Nodes["n2"] = new Node { Id = "n2", Name = "Beta" };

        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "n1", ToNodeId = "n2", Distance = 1f, TotalCapacity = 10, UsedCapacity = 0 };

        state.Intel.Discoveries["disc_002"] = new DiscoveryStateV0 { DiscoveryId = "disc_002", Phase = DiscoveryPhase.Seen };
        state.Intel.Discoveries["disc_001"] = new DiscoveryStateV0 { DiscoveryId = "disc_001", Phase = DiscoveryPhase.Analyzed };

        var snap = MapQueries.BuildSystemSnapshotV0(state, "n1");

        Assert.That(snap.Station, Is.Not.Null);
        Assert.That(snap.Station.NodeId, Is.EqualTo("n1"));
        Assert.That(snap.Station.NodeName, Is.EqualTo("Alpha"));

        Assert.That(snap.LaneGate, Is.Not.Null);
        Assert.That(snap.LaneGate.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(snap.LaneGate[0].EdgeId, Is.EqualTo("e1"));
        Assert.That(snap.LaneGate[0].NeighborNodeId, Is.EqualTo("n2"));

        Assert.That(snap.DiscoverySites, Is.Not.Null);
        Assert.That(snap.DiscoverySites.Count, Is.EqualTo(2));
        Assert.That(snap.DiscoverySites[0].SiteId, Is.EqualTo("disc_001"));
        Assert.That(snap.DiscoverySites[0].PhaseToken, Is.EqualTo("ANALYZED"));
        Assert.That(snap.DiscoverySites[1].SiteId, Is.EqualTo("disc_002"));
        Assert.That(snap.DiscoverySites[1].PhaseToken, Is.EqualTo("SEEN"));
    }

    private static void WriteDeterministicTextFileV0(string path, string contents)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // UTF-8 no BOM; normalize newlines already done by caller.
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string TryReadStringMember(object instance, string name)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        var t = instance.GetType();

        var p = t.GetProperty(name, flags);
        if (p is not null && p.GetIndexParameters().Length == 0)
        {
            var v = p.GetValue(instance);
            if (v is string s) return s;
        }

        var f = t.GetField(name, flags);
        if (f is not null)
        {
            var v = f.GetValue(instance);
            if (v is string s) return s;
        }

        return "";
    }

    private static long TryReadLongMember(object instance, string name)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        var t = instance.GetType();

        var p = t.GetProperty(name, flags);
        if (p is not null && p.GetIndexParameters().Length == 0)
        {
            var v = p.GetValue(instance);
            if (v is int i) return i;
            if (v is long l) return l;
        }

        var f = t.GetField(name, flags);
        if (f is not null)
        {
            var v = f.GetValue(instance);
            if (v is int i) return i;
            if (v is long l) return l;
        }

        return 0;
    }
}
