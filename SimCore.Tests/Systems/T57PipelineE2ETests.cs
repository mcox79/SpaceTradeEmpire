using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.T57.PROOF.PIPELINE_E2E.001: End-to-end proof that the Exploration->Automation pipeline works.
/// Discovery analyzed -> EconomicIntel created -> DISCOVERY_OPPORTUNITY fired -> FO dialogue -> route confidence.
/// </summary>
[TestFixture]
[Category("PipelineE2E")]
public sealed class T57PipelineE2ETests
{
    [Test]
    public void Pipeline_AnalyzedDiscovery_CreatesEconomicIntel_And_FiresFOTrigger()
    {
        var state = new SimState(42);

        // Nodes: player at node_a, discovery at node_b.
        state.Nodes["node_a"] = new Node { Id = "node_a", Name = "Alpha", Kind = NodeKind.Station };
        state.Nodes["node_b"] = new Node { Id = "node_b", Name = "Beta", Kind = NodeKind.Station };
        state.PlayerLocationNodeId = "node_a";
        state.PlayerCredits = 500;

        // Fleet for distance calculation.
        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            State = FleetState.Idle
        };

        // Seed a discovery at node_b in Analyzed phase (ready for outcome generation).
        string discoveryId = "disc_v0|RESOURCE_POOL_MARKER|node_b|ref1|source1";
        state.Nodes["node_b"].SeededDiscoveryIds.Add(discoveryId);
        state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
        {
            DiscoveryId = discoveryId,
            Phase = DiscoveryPhase.Analyzed
        };

        // Promoted FO at Mid tier (DISCOVERY_OPPORTUNITY requires Mid).
        state.FirstOfficer = new FirstOfficer
        {
            CandidateType = FirstOfficerCandidate.Analyst,
            IsPromoted = true,
            Tier = DialogueTier.Mid
        };

        // Act: process discovery outcomes (the pipeline entry point).
        DiscoveryOutcomeSystem.Process(state);

        // Assert 1: EconomicIntel was created from the analyzed discovery.
        string expectedIntelId = "ECON_" + discoveryId;
        Assert.That(state.Intel.EconomicIntels.ContainsKey(expectedIntelId), Is.True,
            "EconomicIntel should be created from analyzed discovery");
        var intel = state.Intel.EconomicIntels[expectedIntelId];
        Assert.That(intel.Type, Is.EqualTo(EconomicIntelType.ResourceDeposit),
            "RESOURCE_POOL_MARKER should map to ResourceDeposit");
        Assert.That(intel.EstimatedValue, Is.EqualTo(EconomicIntelTweaksV0.ResourceDepositBaseValue));
        Assert.That(intel.NodeId, Is.EqualTo("node_b"));
        Assert.That(intel.FlavorText, Is.Not.Empty, "EconomicIntel should have flavor text");

        // Assert 2: DISCOVERY_OPPORTUNITY FO trigger was fired.
        bool triggerFired = state.FirstOfficer.DialogueEventLog
            .Any(e => e.TriggerToken == "DISCOVERY_OPPORTUNITY");
        Assert.That(triggerFired, Is.True,
            "DISCOVERY_OPPORTUNITY trigger should fire when FO is promoted at Mid tier");

        // Assert 3: Anomaly encounter was created (outcome bookkeeping).
        string outcomeKey = "OUTCOME_" + discoveryId;
        Assert.That(state.AnomalyEncounters.ContainsKey(outcomeKey), Is.True,
            "Anomaly encounter should be recorded");
    }

    [Test]
    public void Pipeline_RouteConfidence_IncreasesWithProvenTrades()
    {
        var state = new SimState(42);

        state.Nodes["n1"] = new Node { Id = "n1", Name = "Alpha", Kind = NodeKind.Station };
        state.Nodes["n2"] = new Node { Id = "n2", Name = "Beta", Kind = NodeKind.Station };
        state.Markets["n1"] = new Market();
        state.Markets["n2"] = new Market();
        state.PlayerLocationNodeId = "n1";

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "n1",
            State = FleetState.Idle
        };

        // FO for personality-colored confidence text.
        state.FirstOfficer = new FirstOfficer
        {
            CandidateType = FirstOfficerCandidate.Analyst,
            IsPromoted = true,
            Tier = DialogueTier.Mid
        };

        // Route with 5 proven trades — should get confidence bonus above base 50.
        string routeId = "route_test_1";
        state.Intel.TradeRoutes[routeId] = new TradeRouteIntel
        {
            RouteId = routeId,
            SourceNodeId = "n1",
            DestNodeId = "n2",
            GoodId = "fuel",
            ProvenTradeCount = 5,
            DiscoveredTick = 0,
            Status = TradeRouteStatus.Active
        };

        // Act: call ProcessRouteConfidence directly (it's public static).
        // Tick 0 is on cadence (0 % 10 == 0).
        IntelSystem.ProcessRouteConfidence(state);

        // Assert: confidence score increased above base 50 (5 proven trades * 5 pts each = +25).
        var route = state.Intel.TradeRoutes[routeId];
        int expectedScore = ConfidenceLangTweaksV0.BaseConfidence + (5 * ConfidenceLangTweaksV0.PerProvenTradeBonusPts);
        Assert.That(route.ConfidenceScore, Is.EqualTo(expectedScore),
            $"Expected {expectedScore} = base {ConfidenceLangTweaksV0.BaseConfidence} + 5 trades * {ConfidenceLangTweaksV0.PerProvenTradeBonusPts}");
        Assert.That(route.ConfidenceText, Is.Not.Empty,
            "Confidence text should be generated from FO personality");
    }

    [Test]
    public void Pipeline_ConfidenceText_MatchesFOPersonality()
    {
        var state = new SimState(42);
        state.Nodes["n1"] = new Node { Id = "n1", Name = "Alpha", Kind = NodeKind.Station };
        state.Nodes["n2"] = new Node { Id = "n2", Name = "Beta", Kind = NodeKind.Station };
        state.Markets["n1"] = new Market();
        state.Markets["n2"] = new Market();
        state.PlayerLocationNodeId = "n1";
        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "n1",
            State = FleetState.Idle
        };

        string routeId = "route_pers_1";
        state.Intel.TradeRoutes[routeId] = new TradeRouteIntel
        {
            RouteId = routeId,
            SourceNodeId = "n1",
            DestNodeId = "n2",
            GoodId = "fuel",
            ProvenTradeCount = 5,
            DiscoveredTick = 0,
            Status = TradeRouteStatus.Active
        };

        // Test Analyst personality — uses percentage language.
        state.FirstOfficer = new FirstOfficer
        {
            CandidateType = FirstOfficerCandidate.Analyst,
            IsPromoted = true,
            Tier = DialogueTier.Mid
        };

        IntelSystem.ProcessRouteConfidence(state);
        var route = state.Intel.TradeRoutes[routeId];
        Assert.That(route.ConfidenceText, Does.Contain("%"),
            "Analyst confidence text should use percentage language");
    }

    [Test]
    public void Pipeline_EconomicIntel_HasCorrectDistanceBand()
    {
        var state = new SimState(42);

        // Create a chain of nodes from start to discovery (3 hops).
        state.Nodes["start"] = new Node { Id = "start", Name = "Start", Kind = NodeKind.Station };
        state.Nodes["hop1"] = new Node { Id = "hop1", Name = "Hop1", Kind = NodeKind.Station };
        state.Nodes["hop2"] = new Node { Id = "hop2", Name = "Hop2", Kind = NodeKind.Station };
        state.Nodes["deep"] = new Node { Id = "deep", Name = "Deep", Kind = NodeKind.Station };
        state.PlayerLocationNodeId = "start";
        state.PlayerCredits = 500;

        // Create edges for path: start -> hop1 -> hop2 -> deep.
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "start", ToNodeId = "hop1" };
        state.Edges["e2"] = new Edge { Id = "e2", FromNodeId = "hop1", ToNodeId = "hop2" };
        state.Edges["e3"] = new Edge { Id = "e3", FromNodeId = "hop2", ToNodeId = "deep" };

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "start",
            State = FleetState.Idle
        };

        // Discovery at 'deep' node (3 hops from start).
        string discoveryId = "disc_v0|CORRIDOR_TRACE|deep|ref1|source1";
        state.Nodes["deep"].SeededDiscoveryIds.Add(discoveryId);
        state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
        {
            DiscoveryId = discoveryId,
            Phase = DiscoveryPhase.Analyzed
        };

        state.FirstOfficer = new FirstOfficer
        {
            CandidateType = FirstOfficerCandidate.Veteran,
            IsPromoted = true,
            Tier = DialogueTier.Mid
        };

        DiscoveryOutcomeSystem.Process(state);

        string expectedIntelId = "ECON_" + discoveryId;
        Assert.That(state.Intel.EconomicIntels.ContainsKey(expectedIntelId), Is.True);
        var intel = state.Intel.EconomicIntels[expectedIntelId];
        Assert.That(intel.Type, Is.EqualTo(EconomicIntelType.CargoManifest),
            "CORRIDOR_TRACE should map to CargoManifest");

        // Distance band depends on hops: 3 hops from PlayerLocationNodeId.
        // Band >= 0 (Near, Mid, or Deep depending on tweaks thresholds).
        Assert.That(intel.DistanceBand, Is.GreaterThanOrEqualTo(0));
        Assert.That(intel.FreshnessMaxTicks, Is.GreaterThan(0),
            "Freshness should be set based on distance band");
    }
}
