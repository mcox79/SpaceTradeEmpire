using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Tests.Commands;

// GATE.S16.NPC_ALIVE.DAMAGE_CMD.001
[TestFixture]
public sealed class NpcFleetDamageCommandTests
{
    private static SimState CreateStateWithNpcFleet(string fleetId = "npc_trader_1")
    {
        var state = new SimState(42);
        var fleet = new Fleet
        {
            Id = fleetId,
            OwnerId = "faction_a",
            Role = FleetRole.Trader,
            HullHpMax = 50,
            HullHp = 50,
            ShieldHpMax = 20,
            ShieldHp = 20,
            Speed = 0.8f,
            CurrentNodeId = "star_0"
        };
        state.Fleets[fleetId] = fleet;
        return state;
    }

    [Test]
    public void Damage_ShieldAbsorbsFirst()
    {
        var state = CreateStateWithNpcFleet();
        new NpcFleetDamageCommand("npc_trader_1", 15).Execute(state);

        var fleet = state.Fleets["npc_trader_1"];
        Assert.That(fleet.ShieldHp, Is.EqualTo(5));  // 20 - 15
        Assert.That(fleet.HullHp, Is.EqualTo(50));    // untouched
    }

    [Test]
    public void Damage_OverflowsToHull()
    {
        var state = CreateStateWithNpcFleet();
        new NpcFleetDamageCommand("npc_trader_1", 30).Execute(state);

        var fleet = state.Fleets["npc_trader_1"];
        Assert.That(fleet.ShieldHp, Is.EqualTo(0));
        Assert.That(fleet.HullHp, Is.EqualTo(40));    // 50 - (30 - 20)
    }

    [Test]
    public void Damage_HullClampsAtZero()
    {
        var state = CreateStateWithNpcFleet();
        new NpcFleetDamageCommand("npc_trader_1", 200).Execute(state);

        var fleet = state.Fleets["npc_trader_1"];
        Assert.That(fleet.ShieldHp, Is.EqualTo(0));
        Assert.That(fleet.HullHp, Is.EqualTo(0));
    }

    [Test]
    public void Damage_AppliesDelayTicks()
    {
        var state = CreateStateWithNpcFleet();
        Assert.That(state.Fleets["npc_trader_1"].DelayTicksRemaining, Is.EqualTo(0));

        new NpcFleetDamageCommand("npc_trader_1", 10, delayTicks: 3).Execute(state);
        Assert.That(state.Fleets["npc_trader_1"].DelayTicksRemaining, Is.EqualTo(3));

        // Stacks additively.
        new NpcFleetDamageCommand("npc_trader_1", 5, delayTicks: 2).Execute(state);
        Assert.That(state.Fleets["npc_trader_1"].DelayTicksRemaining, Is.EqualTo(5));
    }

    [Test]
    public void Damage_InitializesHpIfUnset()
    {
        var state = new SimState(42);
        state.Fleets["npc_1"] = new Fleet
        {
            Id = "npc_1",
            OwnerId = "faction_a",
            Role = FleetRole.Patrol,
            // HullHpMax defaults to -1 (uninitialized)
        };

        new NpcFleetDamageCommand("npc_1", 10).Execute(state);

        var fleet = state.Fleets["npc_1"];
        Assert.That(fleet.HullHpMax, Is.GreaterThan(0));
        Assert.That(fleet.ShieldHpMax, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Damage_ZeroOrNegative_NoOp()
    {
        var state = CreateStateWithNpcFleet();
        new NpcFleetDamageCommand("npc_trader_1", 0).Execute(state);
        Assert.That(state.Fleets["npc_trader_1"].ShieldHp, Is.EqualTo(20));

        new NpcFleetDamageCommand("npc_trader_1", -5).Execute(state);
        Assert.That(state.Fleets["npc_trader_1"].ShieldHp, Is.EqualTo(20));
    }

    [Test]
    public void Damage_MissingFleet_NoOp()
    {
        var state = new SimState(42);
        // Should not throw.
        new NpcFleetDamageCommand("nonexistent", 10).Execute(state);
    }
}
