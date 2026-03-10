using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Events;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S7.ENFORCEMENT.CONFISCATION.001: Contract tests for confiscation events.
[TestFixture]
public sealed class ConfiscationTests
{
    private static SimState MakeState(float heat)
    {
        var state = new SimState();
        state.Nodes["n1"] = new Node { Id = "n1", MarketId = "m1" };
        state.Nodes["n2"] = new Node { Id = "n2", MarketId = "m2" };
        state.Markets["m1"] = new Market { Id = "m1" };
        state.Markets["m2"] = new Market { Id = "m2" };
        state.Edges["e1"] = new Edge
        {
            Id = "e1", FromNodeId = "n1", ToNodeId = "n2",
            Distance = 1f, Heat = heat
        };
        state.PlayerCredits = 1000;

        var fleet = new Fleet
        {
            Id = "f1", OwnerId = "player",
            State = FleetState.Traveling,
            CurrentEdgeId = "e1"
        };
        fleet.Cargo["ore"] = 10;
        state.Fleets["f1"] = fleet;

        return state;
    }

    [Test]
    public void BelowThreshold_NoConfiscation()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold - 0.1f);
        SecurityLaneSystem.ProcessSecurityLanes(state);

        Assert.That(state.Fleets["f1"].Cargo["ore"], Is.EqualTo(10),
            "Cargo should be unchanged below heat threshold.");
        Assert.That(state.SecurityEventLog.Count(e => e.Type == SecurityEvents.SecurityEventType.Confiscation),
            Is.EqualTo(0));
    }

    [Test]
    public void AboveThreshold_ConfiscatesGoods()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f);
        SecurityLaneSystem.ProcessSecurityLanes(state);

        int remaining = state.Fleets["f1"].Cargo.TryGetValue("ore", out var v) ? v : 0;
        Assert.That(remaining, Is.LessThan(10), "Cargo should be reduced after confiscation.");

        int seized = 10 - remaining;
        Assert.That(seized, Is.LessThanOrEqualTo(SecurityTweaksV0.ConfiscationMaxUnits),
            "Seized units should not exceed max.");
    }

    [Test]
    public void Confiscation_EmitsEvent()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f);
        SecurityLaneSystem.ProcessSecurityLanes(state);

        var evt = state.SecurityEventLog.FirstOrDefault(
            e => e.Type == SecurityEvents.SecurityEventType.Confiscation);
        Assert.That(evt, Is.Not.Null, "Should emit a confiscation event.");
        Assert.That(evt!.ConfiscatedGoodId, Is.EqualTo("ore"));
        Assert.That(evt.ConfiscatedUnits, Is.GreaterThan(0));
        Assert.That(evt.FineCredits, Is.GreaterThan(0));
    }

    [Test]
    public void Confiscation_DeductsFine()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f);
        long creditsBefore = state.PlayerCredits;
        SecurityLaneSystem.ProcessSecurityLanes(state);

        Assert.That(state.PlayerCredits, Is.LessThan(creditsBefore),
            "Credits should decrease after fine.");
    }

    [Test]
    public void Cooldown_PreventsRepeatedConfiscation()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f);

        // First confiscation.
        SecurityLaneSystem.ProcessSecurityLanes(state);
        int cargoAfterFirst = state.Fleets["f1"].Cargo.TryGetValue("ore", out var v1) ? v1 : 0;

        // Reset heat (it decays during processing) and try again.
        state.Edges["e1"].Heat = SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f;
        SecurityLaneSystem.ProcessSecurityLanes(state);
        int cargoAfterSecond = state.Fleets["f1"].Cargo.TryGetValue("ore", out var v2) ? v2 : 0;

        Assert.That(cargoAfterSecond, Is.EqualTo(cargoAfterFirst),
            "Cooldown should prevent second confiscation.");
    }

    [Test]
    public void NonPlayerFleet_NotAffected()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f);
        state.Fleets["f1"].OwnerId = "npc_trader";

        SecurityLaneSystem.ProcessSecurityLanes(state);

        Assert.That(state.Fleets["f1"].Cargo["ore"], Is.EqualTo(10),
            "NPC fleet should not be confiscated.");
    }

    [Test]
    public void IdleFleet_NotAffected()
    {
        var state = MakeState(SecurityTweaksV0.ConfiscationHeatThreshold + 1.0f);
        state.Fleets["f1"].State = FleetState.Idle;

        SecurityLaneSystem.ProcessSecurityLanes(state);

        Assert.That(state.Fleets["f1"].Cargo["ore"], Is.EqualTo(10),
            "Idle fleet should not be confiscated.");
    }
}
