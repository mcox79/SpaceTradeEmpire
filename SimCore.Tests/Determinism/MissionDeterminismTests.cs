using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Determinism;

// GATE.S1.MISSION.DETERMINISM.001: Mission determinism and save/load tests.
[TestFixture]
[Category("MissionDeterminism")]
public sealed class MissionDeterminismTests
{
    [Test]
    public void MissionCompletion_HashStable_AcrossTwoRuns()
    {
        var hash1 = RunMissionAndGetHash(seed: 42);
        var hash2 = RunMissionAndGetHash(seed: 42);

        Assert.That(hash1, Is.EqualTo(hash2), "World hash must be stable across identical runs.");
        Assert.That(hash1, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void MissionMidway_SaveLoad_PreservesState()
    {
        var state = MakeWorld(seed: 42);
        MissionSystem.AcceptMission(state, "mission_matched_luggage");

        var goodId = state.Missions.ActiveSteps[1].TargetGoodId;

        // Complete step 0 (arrive at start).
        MissionSystem.Process(state);
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(1));

        // Complete step 1 (have cargo).
        state.PlayerCargo[goodId] = 1;
        MissionSystem.Process(state);
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(2));

        // Save mid-mission (at step 2).
        var kernel = new SimKernel(seed: 42);
        // We need to set up the kernel's state to match. Use serialize/deserialize on the state.
        var json = SerializationSystem.Serialize(state);

        // Load into fresh kernel.
        var kernel2 = new SimKernel(seed: 42);
        kernel2.LoadFromString(json);
        var loaded = kernel2.State;

        // Verify mission state preserved.
        Assert.That(loaded.Missions.ActiveMissionId, Is.EqualTo("mission_matched_luggage"));
        Assert.That(loaded.Missions.CurrentStepIndex, Is.EqualTo(2));
        Assert.That(loaded.Missions.ActiveSteps, Has.Count.EqualTo(4));
        Assert.That(loaded.Missions.ActiveSteps[0].Completed, Is.True);
        Assert.That(loaded.Missions.ActiveSteps[1].Completed, Is.True);
        Assert.That(loaded.Missions.ActiveSteps[2].Completed, Is.False);

        // Verify resolved targets survived.
        Assert.That(loaded.Missions.ActiveSteps[1].TargetGoodId, Is.EqualTo(goodId));

        // Verify event log survived.
        Assert.That(loaded.Missions.EventLog, Has.Count.EqualTo(3)); // Accepted + 2 StepCompleted

        // Continue mission and complete.
        var destNode = loaded.Missions.ActiveSteps[2].TargetNodeId;
        loaded.PlayerLocationNodeId = destNode;
        MissionSystem.Process(loaded);

        loaded.PlayerCargo.Remove(goodId);
        MissionSystem.Process(loaded);

        Assert.That(loaded.Missions.ActiveMissionId, Is.EqualTo(""));
        Assert.That(loaded.Missions.CompletedMissionIds, Contains.Item("mission_matched_luggage"));
    }

    [Test]
    public void MissionCompletion_HashChanges_WithMission()
    {
        var stateNoMission = MakeWorld(seed: 42);
        var hashNoMission = stateNoMission.GetSignature();

        var stateWithMission = MakeWorld(seed: 42);
        MissionSystem.AcceptMission(stateWithMission, "mission_matched_luggage");
        var hashWithMission = stateWithMission.GetSignature();

        Assert.That(hashWithMission, Is.Not.EqualTo(hashNoMission),
            "Signature must differ when mission is active.");
    }

    [Test]
    public void MissionEventOrdering_Deterministic()
    {
        var state1 = RunMissionAndGetState(seed: 42);
        var state2 = RunMissionAndGetState(seed: 42);

        Assert.That(state1.Missions.EventLog, Has.Count.EqualTo(state2.Missions.EventLog.Count));

        for (int i = 0; i < state1.Missions.EventLog.Count; i++)
        {
            var e1 = state1.Missions.EventLog[i];
            var e2 = state2.Missions.EventLog[i];
            Assert.That(e1.Seq, Is.EqualTo(e2.Seq));
            Assert.That(e1.Tick, Is.EqualTo(e2.Tick));
            Assert.That(e1.EventType, Is.EqualTo(e2.EventType));
            Assert.That(e1.MissionId, Is.EqualTo(e2.MissionId));
            Assert.That(e1.StepIndex, Is.EqualTo(e2.StepIndex));
        }
    }

    // --- helpers ---

    private static string RunMissionAndGetHash(int seed)
    {
        var state = RunMissionAndGetState(seed);
        return state.GetSignature();
    }

    private static SimState RunMissionAndGetState(int seed)
    {
        var state = MakeWorld(seed);

        MissionSystem.AcceptMission(state, "mission_matched_luggage");
        var goodId = state.Missions.ActiveSteps[1].TargetGoodId;
        var destNode = state.Missions.ActiveSteps[2].TargetNodeId;

        // Step 0: arrive at start (already there).
        MissionSystem.Process(state);

        // Step 1: have cargo.
        state.PlayerCargo[goodId] = 1;
        MissionSystem.Process(state);

        // Step 2: travel to destination.
        state.PlayerLocationNodeId = destNode;
        MissionSystem.Process(state);

        // Step 3: sell cargo.
        state.PlayerCargo.Remove(goodId);
        MissionSystem.Process(state);

        return state;
    }

    private static SimState MakeWorld(int seed)
    {
        var state = new SimState(seed: seed);
        state.PlayerCredits = 1000;
        state.PlayerLocationNodeId = "stn_a";

        state.Nodes["stn_a"] = new Node
        {
            Id = "stn_a", Name = "Station A", Kind = NodeKind.Station, MarketId = "mkt_a"
        };
        state.Nodes["stn_b"] = new Node
        {
            Id = "stn_b", Name = "Station B", Kind = NodeKind.Station, MarketId = "mkt_b"
        };

        var mktA = new Market { Id = "mkt_a" };
        mktA.Inventory["fuel"] = 100;
        mktA.Inventory["ore"] = 50;
        state.Markets["mkt_a"] = mktA;

        var mktB = new Market { Id = "mkt_b" };
        mktB.Inventory["fuel"] = 20;
        state.Markets["mkt_b"] = mktB;

        state.Edges["lane_a_b"] = new Edge
        {
            Id = "lane_a_b", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f
        };

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1", OwnerId = "player",
            CurrentNodeId = "stn_a", State = FleetState.Docked,
        };

        return state;
    }
}
