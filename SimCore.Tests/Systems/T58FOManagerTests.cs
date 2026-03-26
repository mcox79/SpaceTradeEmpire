using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.T58.FO.EMPIRE_HEALTH.001 + DOCK_RECAP.001 + LOA_MODEL.001 +
/// SERVICE_RECORD.001 + FLIP_MOMENT.001 + KG.MILESTONE_ENTITY.001:
/// Contract tests for Tier 1 FO Trade Manager + KG Milestone systems.
/// </summary>
[TestFixture]
[Category("FOManager")]
public sealed class T58FOManagerTests
{
    // ── Empire Health ──

    [Test]
    public void EmpireHealth_NoRoutes_StatusIsNone()
    {
        var state = CreateBaseState();
        EmpireHealthSystem.Process(state);
        Assert.That(state.FirstOfficer!.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.None));
    }

    [Test]
    public void EmpireHealth_AllRoutesHealthy_StatusIsHealthy()
    {
        var state = CreateBaseState();
        AddActiveRoute(state, "r1", TradeRouteStatus.Active, confidenceScore: 50);
        AddActiveRoute(state, "r2", TradeRouteStatus.Active, confidenceScore: 80);

        EmpireHealthSystem.Process(state);

        Assert.That(state.FirstOfficer!.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.Healthy));
        Assert.That(state.FirstOfficer.EmpireHealth.TotalManagedRoutes, Is.EqualTo(2));
        Assert.That(state.FirstOfficer.EmpireHealth.HealthyRoutes, Is.EqualTo(2));
    }

    [Test]
    public void EmpireHealth_LowMarginRoute_StatusIsDegraded()
    {
        var state = CreateBaseState();
        AddActiveRoute(state, "r1", TradeRouteStatus.Active, confidenceScore: 50);
        AddActiveRoute(state, "r2", TradeRouteStatus.Active, confidenceScore: 2); // Below DegradedMarginPct

        EmpireHealthSystem.Process(state);

        Assert.That(state.FirstOfficer!.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.Degraded));
        Assert.That(state.FirstOfficer.EmpireHealth.DegradedRoutes, Is.EqualTo(1));
    }

    [Test]
    public void EmpireHealth_DeadRoute_StatusIsCritical()
    {
        var state = CreateBaseState();
        AddActiveRoute(state, "r1", TradeRouteStatus.Active, confidenceScore: 50);
        AddActiveRoute(state, "r2", TradeRouteStatus.Unprofitable, confidenceScore: 0);

        EmpireHealthSystem.Process(state);

        Assert.That(state.FirstOfficer!.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.Critical));
        Assert.That(state.FirstOfficer.EmpireHealth.DeadRoutes, Is.EqualTo(1));
    }

    [Test]
    public void EmpireHealth_DestroyedShip_StatusIsCritical()
    {
        var state = CreateBaseState();
        AddActiveRoute(state, "r1", TradeRouteStatus.Active, confidenceScore: 50);

        // Add a destroyed player fleet.
        state.Fleets["fleet_dead"] = new Fleet
        {
            Id = "fleet_dead",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            HullHp = 0,
            HullHpMax = 100
        };

        EmpireHealthSystem.Process(state);

        Assert.That(state.FirstOfficer!.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.Critical));
        Assert.That(state.FirstOfficer.EmpireHealth.ShipLost, Is.True);
    }

    [Test]
    public void EmpireHealth_DegradationTracksTransition()
    {
        var state = CreateBaseState();
        AddActiveRoute(state, "r1", TradeRouteStatus.Active, confidenceScore: 50);

        // First tick: establish Healthy.
        EmpireHealthSystem.Process(state);
        Assert.That(state.FirstOfficer!.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.Healthy));

        // Degrade a route.
        state.Intel.TradeRoutes["r1"].ConfidenceScore = 2;

        // Advance to next cadence tick.
        for (int i = 0; i < FOManagerTweaksV0.HealthEvalCadenceTicks; i++)
            state.AdvanceTick();

        EmpireHealthSystem.Process(state);

        Assert.That(state.FirstOfficer.EmpireHealth.Status, Is.EqualTo(EmpireHealthStatus.Degraded));
        Assert.That(state.FirstOfficer.EmpireHealth.PreviousStatus, Is.EqualTo(EmpireHealthStatus.Healthy),
            "Previous status should record the transition from Healthy");
        Assert.That(state.FirstOfficer.EmpireHealth.LastTransitionTick, Is.EqualTo(state.Tick),
            "Transition tick should record when degradation occurred");
    }

    // ── Dock Recap ──

    [Test]
    public void DockRecap_NoDocking_NoPendingRecap()
    {
        var state = CreateBaseState();
        DockRecapSystem.Process(state);
        Assert.That(state.FirstOfficer!.DockRecap.PendingRecap, Is.False);
    }

    [Test]
    public void DockRecap_DockAfterThreshold_GeneratesRecap()
    {
        var state = CreateBaseState();
        var recap = state.FirstOfficer!.DockRecap;
        recap.LastDockTick = 1;

        // Add some trades to accumulate.
        state.TransactionLog.Add(new TransactionRecord
        {
            Tick = 50, Source = "Sell", ProfitDelta = 200, CashDelta = 200
        });
        state.TransactionLog.Add(new TransactionRecord
        {
            Tick = 80, Source = "Sell", ProfitDelta = 300, CashDelta = 300
        });

        // Advance past threshold.
        while (state.Tick < FOManagerTweaksV0.RecapMinTicksSinceLastDock + 5)
            state.AdvanceTick();

        // Accumulate metrics while not docked.
        DockRecapSystem.Process(state);

        // Now dock the player fleet.
        state.Fleets["fleet_trader_1"].State = FleetState.Docked;
        DockRecapSystem.Process(state);

        Assert.That(recap.PendingRecap, Is.True);
        Assert.That(recap.RecapLines.Count, Is.GreaterThan(0));
        Assert.That(recap.RecapLines.Count, Is.LessThanOrEqualTo(FOManagerTweaksV0.RecapMaxLines));
    }

    // ── LOA Model ──

    [Test]
    public void LOA_DefaultLevels_MatchTweaks()
    {
        var table = new LOATable();

        Assert.That(table.GetLevel(LOADomain.RouteCreation), Is.EqualTo(FOManagerTweaksV0.LOARouteCreation));
        Assert.That(table.GetLevel(LOADomain.RouteOptimization), Is.EqualTo(FOManagerTweaksV0.LOARouteOptimization));
        Assert.That(table.GetLevel(LOADomain.SustainLogistics), Is.EqualTo(FOManagerTweaksV0.LOASustainLogistics));
        Assert.That(table.GetLevel(LOADomain.ShipPurchase), Is.EqualTo(FOManagerTweaksV0.LOAShipPurchase));
        Assert.That(table.GetLevel(LOADomain.WarfrontResponse), Is.EqualTo(FOManagerTweaksV0.LOAWarfrontResponse));
        Assert.That(table.GetLevel(LOADomain.Construction), Is.EqualTo(FOManagerTweaksV0.LOAConstruction));
    }

    [Test]
    public void LOA_SetLevel_ClampedTo4To7()
    {
        var table = new LOATable();

        table.SetLevel(LOADomain.RouteCreation, 3); // Below min
        Assert.That(table.GetLevel(LOADomain.RouteCreation), Is.EqualTo(4));

        table.SetLevel(LOADomain.RouteCreation, 8); // Above max
        Assert.That(table.GetLevel(LOADomain.RouteCreation), Is.EqualTo(7));

        table.SetLevel(LOADomain.RouteCreation, 6); // In range
        Assert.That(table.GetLevel(LOADomain.RouteCreation), Is.EqualTo(6));
    }

    [Test]
    public void LOA_RevertEntries_CleanedUpAfterWindow()
    {
        var state = CreateBaseState();
        var loa = state.FirstOfficer!.LOA;

        loa.RevertEntries.Add(new RouteRevertEntry
        {
            RouteId = "r1",
            ActionTick = 0,
            PreviousSourceNodeId = "n1",
            PreviousDestNodeId = "n2",
            PreviousGoodId = "fuel"
        });

        // Advance past revert window.
        while (state.Tick < FOManagerTweaksV0.RouteRevertWindowTicks + 10)
            state.AdvanceTick();

        FirstOfficerSystem.Process(state);

        Assert.That(loa.RevertEntries, Is.Empty, "Expired revert entries should be cleaned up");
    }

    // ── Service Record ──

    [Test]
    public void ServiceRecord_InitializesEmpty()
    {
        var fo = new FirstOfficer { CandidateType = FirstOfficerCandidate.Analyst, IsPromoted = true };
        Assert.That(fo.ServiceRecord.RoutesManaged, Is.EqualTo(0));
        Assert.That(fo.ServiceRecord.RecommendationsTaken, Is.EqualTo(0));
        Assert.That(fo.ServiceRecord.History, Is.Empty);
        Assert.That(fo.ServiceRecord.WorstCallDescription, Is.EqualTo(""));
    }

    [Test]
    public void ServiceRecord_TracksCrisis()
    {
        var fo = new FirstOfficer { CandidateType = FirstOfficerCandidate.Veteran, IsPromoted = true };
        var record = fo.ServiceRecord;

        record.CrisesHandled++;
        record.History.Add(new ServiceRecordEntry
        {
            Tick = 500,
            EventType = "crisis",
            Description = "Valorin embargo response",
            CreditImpact = -120,
            WasSuccessful = true
        });

        Assert.That(record.CrisesHandled, Is.EqualTo(1));
        Assert.That(record.History.Count, Is.EqualTo(1));
        Assert.That(record.History[0].EventType, Is.EqualTo("crisis"));
    }

    [Test]
    public void ServiceRecord_WorstCallTracksMaxCost()
    {
        var record = new FOServiceRecord();

        // Simulate tracking worst call.
        long cost = 250;
        if (cost > record.WorstCallCost)
        {
            record.WorstCallCost = cost;
            record.WorstCallDescription = "Route Beta reroute cost 250cr";
        }

        Assert.That(record.WorstCallCost, Is.EqualTo(250));
        Assert.That(record.WorstCallDescription, Does.Contain("Route Beta"));
    }

    // ── Flip Moment ──

    [Test]
    public void FlipMoment_NotEnoughRoutes_DoesNotTrigger()
    {
        var state = CreateBaseState();
        state.FirstOfficer!.EmpireHealth.TotalManagedRoutes = 1; // Below FlipMinRoutes

        FlipMomentSystem.Process(state);

        Assert.That(state.FirstOfficer.FlipMoment.HasFlipped, Is.False);
    }

    [Test]
    public void FlipMoment_SustainedPositive_Triggers()
    {
        var state = CreateBaseState();
        state.FirstOfficer!.EmpireHealth.TotalManagedRoutes = FOManagerTweaksV0.FlipMinRoutes;

        // Add positive transactions each tick.
        for (int i = 0; i < FOManagerTweaksV0.FlipSustainedTicks + 5; i++)
        {
            state.TransactionLog.Add(new TransactionRecord
            {
                Tick = state.Tick,
                Source = "Sell",
                ProfitDelta = 100,
                CashDelta = 100
            });

            FlipMomentSystem.Process(state);
            state.AdvanceTick();
        }

        Assert.That(state.FirstOfficer.FlipMoment.HasFlipped, Is.True);
        Assert.That(state.FirstOfficer.FlipMoment.FlipEventPending, Is.True);
        Assert.That(state.FirstOfficer.FlipMoment.FlipTick, Is.GreaterThan(0));
    }

    [Test]
    public void FlipMoment_OnlyFiresOnce()
    {
        var state = CreateBaseState();
        state.FirstOfficer!.FlipMoment.HasFlipped = true;
        state.FirstOfficer.EmpireHealth.TotalManagedRoutes = FOManagerTweaksV0.FlipMinRoutes;

        // Even with positive revenue, should not re-trigger.
        state.TransactionLog.Add(new TransactionRecord
        {
            Tick = state.Tick, Source = "Sell", ProfitDelta = 1000, CashDelta = 1000
        });

        FlipMomentSystem.Process(state);

        // FlipEventPending should still be false (wasn't set to true again).
        Assert.That(state.FirstOfficer.FlipMoment.FlipEventPending, Is.False);
    }

    // ── Decision Dialogue ──

    [Test]
    public void DecisionDialogue_QueueAndDequeue_OneAtATime()
    {
        var state = CreateBaseState();
        var decision1 = CreateTestDecision("d1", DecisionType.Crisis, severity: 5);
        var decision2 = CreateTestDecision("d2", DecisionType.RouteProposal, severity: 2);

        DecisionDialogueSystem.QueueDecision(state, decision1);
        DecisionDialogueSystem.QueueDecision(state, decision2);

        Assert.That(state.FirstOfficer!.DecisionQueue.Count, Is.EqualTo(2));

        // Process: highest severity first (Rule 5).
        DecisionDialogueSystem.Process(state);

        Assert.That(state.FirstOfficer.ActiveDecision, Is.Not.Null);
        Assert.That(state.FirstOfficer.ActiveDecision!.DecisionId, Is.EqualTo("d1"));
        Assert.That(state.FirstOfficer.ActiveDecision.Status, Is.EqualTo(DecisionStatus.AwaitingPlayer));
        Assert.That(state.FirstOfficer.DecisionQueue.Count, Is.EqualTo(1));
    }

    [Test]
    public void DecisionDialogue_NoDequeueWhileActive()
    {
        var state = CreateBaseState();
        var decision1 = CreateTestDecision("d1", DecisionType.Crisis, severity: 5);
        var decision2 = CreateTestDecision("d2", DecisionType.RouteProposal, severity: 2);

        DecisionDialogueSystem.QueueDecision(state, decision1);
        DecisionDialogueSystem.Process(state);

        DecisionDialogueSystem.QueueDecision(state, decision2);
        DecisionDialogueSystem.Process(state);

        // Rule 5: should NOT dequeue d2 while d1 is active.
        Assert.That(state.FirstOfficer!.ActiveDecision!.DecisionId, Is.EqualTo("d1"));
    }

    [Test]
    public void DecisionDialogue_ResolveDecision_TracksServiceRecord()
    {
        var state = CreateBaseState();
        var decision = CreateTestDecision("d1", DecisionType.RouteProposal, severity: 3);

        DecisionDialogueSystem.QueueDecision(state, decision);
        DecisionDialogueSystem.Process(state);

        // Player selects the FO recommendation.
        int recommended = state.FirstOfficer!.ActiveDecision!.RecommendedOptionIndex;
        bool resolved = DecisionDialogueSystem.ResolveDecision(state, recommended);

        Assert.That(resolved, Is.True);
        Assert.That(state.FirstOfficer.ActiveDecision, Is.Null);
        Assert.That(state.FirstOfficer.ResolvedDecisions.Count, Is.EqualTo(1));
        Assert.That(state.FirstOfficer.ServiceRecord.RecommendationsOffered, Is.EqualTo(1));
        Assert.That(state.FirstOfficer.ServiceRecord.RecommendationsTaken, Is.EqualTo(1));
    }

    [Test]
    public void DecisionDialogue_PersonalityRecommendation_Analyst_PicksProfit()
    {
        var state = CreateBaseState();
        state.FirstOfficer!.CandidateType = FirstOfficerCandidate.Analyst;

        var decision = new FODecision
        {
            DecisionId = "test_personality",
            Type = DecisionType.RouteProposal,
            Severity = 3,
            Situation = "New route opportunity.",
            Stakes = "Revenue impact.",
            Options = new()
            {
                new DecisionOption { Label = "Safe", CreditImpact = 50, RiskLevel = 0, ExplorationValue = 0 },
                new DecisionOption { Label = "Profit", CreditImpact = 200, RiskLevel = 1, ExplorationValue = 0 },
                new DecisionOption { Label = "Explore", CreditImpact = 10, RiskLevel = 2, ExplorationValue = 5 }
            }
        };

        DecisionDialogueSystem.QueueDecision(state, decision);
        DecisionDialogueSystem.Process(state);

        Assert.That(state.FirstOfficer.ActiveDecision!.RecommendedOptionIndex, Is.EqualTo(1),
            "Analyst (Maren) should recommend highest CreditImpact option");
    }

    [Test]
    public void DecisionDialogue_PersonalityRecommendation_Veteran_PicksSafe()
    {
        var state = CreateBaseState();
        state.FirstOfficer!.CandidateType = FirstOfficerCandidate.Veteran;

        var decision = new FODecision
        {
            DecisionId = "test_vet",
            Type = DecisionType.Warfront,
            Severity = 5,
            Situation = "Warfront escalation.",
            Stakes = "Route safety.",
            Options = new()
            {
                new DecisionOption { Label = "Engage", CreditImpact = 100, RiskLevel = 2, ExplorationValue = 0 },
                new DecisionOption { Label = "Defend", CreditImpact = 50, RiskLevel = 0, ExplorationValue = 0 },
                new DecisionOption { Label = "Withdraw", CreditImpact = -20, RiskLevel = 0, ExplorationValue = 0 }
            }
        };

        DecisionDialogueSystem.QueueDecision(state, decision);
        DecisionDialogueSystem.Process(state);

        // Veteran picks lowest risk, then highest credit among tied risk.
        Assert.That(state.FirstOfficer.ActiveDecision!.RecommendedOptionIndex, Is.EqualTo(1),
            "Veteran (Dask) should recommend safest option with best credit impact");
    }

    // ── KG Milestones ──

    [Test]
    public void KGMilestone_DefaultIsGeographic()
    {
        var state = CreateBaseState();
        Assert.That(state.Intel.KGMilestones.HighestMilestone, Is.EqualTo(KGMilestone.Geographic));
    }

    [Test]
    public void KGMilestone_3DiscoveriesSeen_UnlocksPin()
    {
        var state = CreateBaseState();
        AddDiscovery(state, "d1", DiscoveryPhase.Seen);
        AddDiscovery(state, "d2", DiscoveryPhase.Seen);
        AddDiscovery(state, "d3", DiscoveryPhase.Seen);

        KnowledgeGraphSystem.Process(state);

        Assert.That(state.Intel.KGMilestones.HighestMilestone, Is.EqualTo(KGMilestone.Pin));
        Assert.That(state.Intel.KGMilestones.MilestoneTicks.ContainsKey((int)KGMilestone.Pin), Is.True);
    }

    [Test]
    public void KGMilestone_ConnectionRevealed_UnlocksRelational()
    {
        var state = CreateBaseState();
        AddDiscovery(state, "d1", DiscoveryPhase.Seen);
        AddDiscovery(state, "d2", DiscoveryPhase.Seen);
        AddDiscovery(state, "d3", DiscoveryPhase.Seen);

        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "kc1",
            SourceDiscoveryId = "d1",
            TargetDiscoveryId = "d2",
            IsRevealed = true,
            RevealedTick = 10
        });

        KnowledgeGraphSystem.Process(state);

        Assert.That(state.Intel.KGMilestones.HighestMilestone,
            Is.GreaterThanOrEqualTo(KGMilestone.Relational));
    }

    [Test]
    public void KGMilestone_5SeenPlus1Analyzed_UnlocksAnnotate()
    {
        var state = CreateBaseState();
        for (int i = 0; i < 5; i++)
            AddDiscovery(state, $"d{i}", DiscoveryPhase.Seen);
        AddDiscovery(state, "d_analyzed", DiscoveryPhase.Analyzed);

        // Need a connection for Relational milestone first.
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "kc1",
            SourceDiscoveryId = "d0",
            TargetDiscoveryId = "d1",
            IsRevealed = true,
            RevealedTick = 10
        });

        KnowledgeGraphSystem.Process(state);

        Assert.That(state.Intel.KGMilestones.HighestMilestone,
            Is.GreaterThanOrEqualTo(KGMilestone.Annotate));
    }

    [Test]
    public void KGMilestone_FOPromotedPlus3Analyzed_UnlocksFlag()
    {
        var state = CreateBaseState();
        for (int i = 0; i < 5; i++)
            AddDiscovery(state, $"d{i}", DiscoveryPhase.Seen);
        AddDiscovery(state, "da1", DiscoveryPhase.Analyzed);
        AddDiscovery(state, "da2", DiscoveryPhase.Analyzed);
        AddDiscovery(state, "da3", DiscoveryPhase.Analyzed);

        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "kc1", SourceDiscoveryId = "d0", TargetDiscoveryId = "d1",
            IsRevealed = true, RevealedTick = 10
        });
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "kc2", SourceDiscoveryId = "d2", TargetDiscoveryId = "d3",
            IsRevealed = true, RevealedTick = 10
        });

        KnowledgeGraphSystem.Process(state);

        Assert.That(state.Intel.KGMilestones.HighestMilestone,
            Is.GreaterThanOrEqualTo(KGMilestone.Flag));
    }

    [Test]
    public void KGMilestone_FullProgression_ReachesCompare()
    {
        var state = CreateBaseState();
        for (int i = 0; i < 8; i++)
            AddDiscovery(state, $"d{i}", DiscoveryPhase.Seen);
        AddDiscovery(state, "da1", DiscoveryPhase.Analyzed);
        AddDiscovery(state, "da2", DiscoveryPhase.Analyzed);
        AddDiscovery(state, "da3", DiscoveryPhase.Analyzed);

        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "kc1", SourceDiscoveryId = "d0", TargetDiscoveryId = "d1",
            IsRevealed = true, RevealedTick = 10
        });
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = "kc2", SourceDiscoveryId = "d2", TargetDiscoveryId = "d3",
            IsRevealed = true, RevealedTick = 10
        });

        KnowledgeGraphSystem.Process(state);

        Assert.That(state.Intel.KGMilestones.HighestMilestone, Is.EqualTo(KGMilestone.Compare));
        Assert.That(state.Intel.KGMilestones.PendingMilestoneNotification, Is.EqualTo((int)KGMilestone.Compare));
    }

    // ── Helpers ──

    private static SimState CreateBaseState()
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { Id = "node_a", Name = "Alpha", Kind = NodeKind.Station };
        state.Nodes["node_b"] = new Node { Id = "node_b", Name = "Beta", Kind = NodeKind.Station };
        state.PlayerLocationNodeId = "node_a";
        state.PlayerCredits = 1000;

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            State = FleetState.Idle,
            HullHp = 100,
            HullHpMax = 100
        };

        state.FirstOfficer = new FirstOfficer
        {
            CandidateType = FirstOfficerCandidate.Analyst,
            IsPromoted = true,
            Tier = DialogueTier.Mid
        };

        return state;
    }

    private static void AddActiveRoute(SimState state, string routeId,
        TradeRouteStatus status, int confidenceScore)
    {
        state.Intel.TradeRoutes[routeId] = new TradeRouteIntel
        {
            RouteId = routeId,
            SourceNodeId = "node_a",
            DestNodeId = "node_b",
            GoodId = "fuel",
            Status = status,
            ConfidenceScore = confidenceScore,
            DiscoveredTick = 0
        };
    }

    private static FODecision CreateTestDecision(string id, DecisionType type, int severity)
    {
        return new FODecision
        {
            DecisionId = id,
            Type = type,
            Severity = severity,
            Situation = $"Test situation for {id}.",
            Stakes = $"Test stakes for {id}.",
            Options = new()
            {
                new DecisionOption { Label = "Option A", CreditImpact = 100, RiskLevel = 0, ExplorationValue = 0, ConsequenceColor = "green" },
                new DecisionOption { Label = "Option B", CreditImpact = 50, RiskLevel = 1, ExplorationValue = 3, ConsequenceColor = "amber" },
                new DecisionOption { Label = "Option C", CreditImpact = -20, RiskLevel = 2, ExplorationValue = 5, ConsequenceColor = "red" }
            }
        };
    }

    private static void AddDiscovery(SimState state, string id, DiscoveryPhase phase)
    {
        state.Intel.Discoveries[id] = new DiscoveryStateV0
        {
            DiscoveryId = id,
            Phase = phase
        };
    }
}
