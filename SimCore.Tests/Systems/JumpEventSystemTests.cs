using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S15.FEEL.JUMP_EVENT_SYS.001
[TestFixture]
public sealed class JumpEventSystemTests
{
    private static SimState CreateStateWithArrival(string fleetId, string edgeId, string nodeId)
    {
        var state = new SimState(42);
        state.Fleets[fleetId] = new Fleet
        {
            Id = fleetId,
            CurrentNodeId = nodeId,
            HullHp = 100,
            HullHpMax = 100
        };
        state.ArrivalsThisTick.Add((fleetId, edgeId, nodeId));
        return state;
    }

    [Test]
    public void JumpEventSystem_ProcessesWithoutErrors()
    {
        var state = CreateStateWithArrival("fleet_trader_1", "edge_0", "star_1");
        JumpEventSystem.Process(state);
        // No exceptions thrown — events may or may not fire based on deterministic RNG.
    }

    [Test]
    public void JumpEventSystem_NoArrivals_NoEvents()
    {
        var state = new SimState(42);
        JumpEventSystem.Process(state);
        Assert.That(state.JumpEvents, Is.Empty);
    }

    [Test]
    public void JumpEventSystem_DeterministicAcrossCalls()
    {
        var state1 = CreateStateWithArrival("fleet_trader_1", "edge_0", "star_1");
        var state2 = CreateStateWithArrival("fleet_trader_1", "edge_0", "star_1");

        JumpEventSystem.Process(state1);
        JumpEventSystem.Process(state2);

        Assert.That(state2.JumpEvents.Count, Is.EqualTo(state1.JumpEvents.Count));
        if (state1.JumpEvents.Count > 0)
        {
            Assert.That(state2.JumpEvents[0].Kind, Is.EqualTo(state1.JumpEvents[0].Kind));
        }
    }

    [Test]
    public void JumpEventSystem_TurbulenceNeverKills()
    {
        var state = new SimState(42);
        state.Fleets["f1"] = new Fleet { Id = "f1", HullHp = 5, HullHpMax = 100 };

        for (int i = 0; i < 100; i++)
        {
            state.ArrivalsThisTick.Clear();
            state.ArrivalsThisTick.Add(("f1", $"e{i}", $"n{i}"));
            JumpEventSystem.Process(state);
        }

        Assert.That(state.Fleets["f1"].HullHp, Is.GreaterThanOrEqualTo(1), "Turbulence should never reduce HP below 1");
    }
}
