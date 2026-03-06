using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S5.SEC_LANES.SYSTEM.001: Security lane system contract tests.
[TestFixture]
[Category("SecurityLaneSystem")]
public sealed class SecurityLaneSystemTests
{
    private SimState CreateTestState()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel.State;
    }

    [Test]
    public void ProcessSecurityLanes_NullState_NoThrow()
    {
        Assert.DoesNotThrow(() => SecurityLaneSystem.ProcessSecurityLanes(null!));
    }

    [Test]
    public void ProcessSecurityLanes_EdgesStartAtDefault()
    {
        var state = CreateTestState();

        foreach (var edge in state.Edges.Values)
        {
            Assert.That(edge.SecurityLevelBps, Is.EqualTo(SecurityTweaksV0.DefaultSecurityBps),
                $"Edge {edge.Id} should start at default security");
        }
    }

    [Test]
    public void ProcessSecurityLanes_PatrolRaisesSecurity()
    {
        var state = CreateTestState();

        // Pick an edge and place a patrol fleet on one of its nodes
        var edge = state.Edges.Values.First();
        var patrolFleet = new Fleet
        {
            Id = "patrol_test",
            OwnerId = "npc",
            Role = FleetRole.Patrol,
            CurrentNodeId = edge.FromNodeId,
        };
        state.Fleets["patrol_test"] = patrolFleet;

        // Lower edge security first
        edge.SecurityLevelBps = SecurityTweaksV0.ModerateBps - 500;
        int before = edge.SecurityLevelBps;

        SecurityLaneSystem.ProcessSecurityLanes(state);

        // Patrol boost + drift toward default should increase security
        Assert.That(edge.SecurityLevelBps, Is.GreaterThan(before),
            "Patrol presence should raise security level");
    }

    [Test]
    public void ProcessSecurityLanes_HeatLowersSecurity()
    {
        var state = CreateTestState();

        var edge = state.Edges.Values.First();
        edge.Heat = 10f; // high economic heat

        int before = edge.SecurityLevelBps;
        SecurityLaneSystem.ProcessSecurityLanes(state);

        // Heat penalty should lower security
        Assert.That(edge.SecurityLevelBps, Is.LessThan(before),
            "High economic heat should lower security level");
    }

    [Test]
    public void ProcessSecurityLanes_ClampsToMinMax()
    {
        var state = CreateTestState();

        var edge = state.Edges.Values.First();
        edge.SecurityLevelBps = SecurityTweaksV0.MaxSecurityBps;
        edge.Heat = 0f;

        // Place multiple patrols to try to exceed max
        for (int i = 0; i < 100; i++)
        {
            state.Fleets[$"patrol_{i}"] = new Fleet
            {
                Id = $"patrol_{i}",
                OwnerId = "npc",
                Role = FleetRole.Patrol,
                CurrentNodeId = edge.FromNodeId,
            };
        }

        SecurityLaneSystem.ProcessSecurityLanes(state);

        Assert.That(edge.SecurityLevelBps, Is.LessThanOrEqualTo(SecurityTweaksV0.MaxSecurityBps));
        Assert.That(edge.SecurityLevelBps, Is.GreaterThanOrEqualTo(SecurityTweaksV0.MinSecurityBps));
    }

    [Test]
    public void GetSecurityBand_CorrectMapping()
    {
        Assert.That(SecurityLaneSystem.GetSecurityBand(SecurityTweaksV0.HostileBps - 1), Is.EqualTo("hostile"));
        Assert.That(SecurityLaneSystem.GetSecurityBand(SecurityTweaksV0.HostileBps), Is.EqualTo("dangerous"));
        Assert.That(SecurityLaneSystem.GetSecurityBand(SecurityTweaksV0.DangerousBps), Is.EqualTo("moderate"));
        Assert.That(SecurityLaneSystem.GetSecurityBand(SecurityTweaksV0.SafeBps), Is.EqualTo("safe"));
    }

    [Test]
    public void ProcessSecurityLanes_Deterministic()
    {
        var state1 = CreateTestState();
        var state2 = CreateTestState();

        // Same patrol setup
        state1.Fleets["p1"] = new Fleet { Id = "p1", OwnerId = "npc", Role = FleetRole.Patrol, CurrentNodeId = state1.Edges.Values.First().FromNodeId };
        state2.Fleets["p1"] = new Fleet { Id = "p1", OwnerId = "npc", Role = FleetRole.Patrol, CurrentNodeId = state2.Edges.Values.First().FromNodeId };

        SecurityLaneSystem.ProcessSecurityLanes(state1);
        SecurityLaneSystem.ProcessSecurityLanes(state2);

        var edges1 = state1.Edges.Values.OrderBy(e => e.Id).ToList();
        var edges2 = state2.Edges.Values.OrderBy(e => e.Id).ToList();

        for (int i = 0; i < edges1.Count; i++)
        {
            Assert.That(edges1[i].SecurityLevelBps, Is.EqualTo(edges2[i].SecurityLevelBps),
                $"Edge {edges1[i].Id} security should be deterministic");
        }
    }
}
