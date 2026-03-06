using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S16.NPC_ALIVE.FLEET_DESTROY.001
[TestFixture]
public sealed class NpcFleetCombatSystemTests
{
    private static SimState CreateStateWithDestroyedNpc()
    {
        var state = new SimState(42);

        var edge = new Edge { Id = "e1", FromNodeId = "a", ToNodeId = "b", Distance = 10, TotalCapacity = 5, UsedCapacity = 1 };
        state.Edges["e1"] = edge;

        state.Fleets["npc_1"] = new Fleet
        {
            Id = "npc_1",
            OwnerId = "faction_a",
            Role = FleetRole.Trader,
            HullHpMax = 50,
            HullHp = 0, // destroyed
            ShieldHpMax = 20,
            ShieldHp = 0,
            State = FleetState.Traveling,
            CurrentEdgeId = "e1",
            CurrentNodeId = "a"
        };

        state.Fleets["npc_2"] = new Fleet
        {
            Id = "npc_2",
            OwnerId = "faction_b",
            Role = FleetRole.Patrol,
            HullHpMax = 30,
            HullHp = 15, // alive
            ShieldHpMax = 10,
            ShieldHp = 5,
            CurrentNodeId = "a"
        };

        return state;
    }

    [Test]
    public void DestroyedNpc_RemovedFromState()
    {
        var state = CreateStateWithDestroyedNpc();
        Assert.That(state.Fleets.ContainsKey("npc_1"), Is.True);

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Fleets.ContainsKey("npc_1"), Is.False);
        Assert.That(state.Fleets.ContainsKey("npc_2"), Is.True); // alive, kept
    }

    [Test]
    public void DestroyedNpc_FreesEdgeCapacity()
    {
        var state = CreateStateWithDestroyedNpc();
        Assert.That(state.Edges["e1"].UsedCapacity, Is.EqualTo(1));

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Edges["e1"].UsedCapacity, Is.EqualTo(0));
    }

    [Test]
    public void DestroyedNpc_RecordedInTransientList()
    {
        var state = CreateStateWithDestroyedNpc();

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.NpcFleetsDestroyedThisTick, Contains.Item("npc_1"));
        Assert.That(state.NpcFleetsDestroyedThisTick.Count, Is.EqualTo(1));
    }

    [Test]
    public void PlayerFleet_NeverDestroyed()
    {
        var state = new SimState(42);
        state.Fleets["player_fleet"] = new Fleet
        {
            Id = "player_fleet",
            OwnerId = "player",
            HullHpMax = 100,
            HullHp = 0, // at zero but owned by player
            CurrentNodeId = "a"
        };

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Fleets.ContainsKey("player_fleet"), Is.True);
    }

    [Test]
    public void UninitializedHp_NotDestroyed()
    {
        var state = new SimState(42);
        state.Fleets["npc_new"] = new Fleet
        {
            Id = "npc_new",
            OwnerId = "faction_a",
            // HullHpMax = -1 (default, uninitialized)
            CurrentNodeId = "a"
        };

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Fleets.ContainsKey("npc_new"), Is.True);
    }

    // --- GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001 ---

    [Test]
    public void DestroyedNpc_QueuedForRespawn()
    {
        var state = CreateStateWithDestroyedNpc();
        NpcFleetCombatSystem.Process(state);

        Assert.That(state.NpcRespawnQueue.Count, Is.EqualTo(1));
        Assert.That(state.NpcRespawnQueue[0].FleetId, Is.EqualTo("npc_1"));
        Assert.That(state.NpcRespawnQueue[0].HomeNodeId, Is.EqualTo("a"));
    }

    [Test]
    public void Respawn_AfterCooldown()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0", Name = "Star 0" };

        // Simulate a fleet destroyed at tick 10.
        state.NpcRespawnQueue.Add(new NpcRespawnEntry
        {
            FleetId = "ai_fleet_star_0",
            HomeNodeId = "star_0",
            DestructionTick = 10
        });

        // Tick 50: not enough time elapsed (cooldown = 60).
        // Manually set tick via reflection or advance.
        // SimState.Tick is read-only via AdvanceTick, so let's advance to tick 70.
        for (int i = 0; i < 70; i++) state.AdvanceTick();

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Fleets.ContainsKey("ai_fleet_star_0"), Is.True);
        Assert.That(state.NpcRespawnQueue.Count, Is.EqualTo(0));
        Assert.That(state.Fleets["ai_fleet_star_0"].CurrentNodeId, Is.EqualTo("star_0"));
    }

    [Test]
    public void Respawn_NotBeforeCooldown()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0", Name = "Star 0" };

        state.NpcRespawnQueue.Add(new NpcRespawnEntry
        {
            FleetId = "ai_fleet_star_0",
            HomeNodeId = "star_0",
            DestructionTick = 0
        });

        // Only advance 30 ticks (< 60 cooldown).
        for (int i = 0; i < 30; i++) state.AdvanceTick();

        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Fleets.ContainsKey("ai_fleet_star_0"), Is.False);
        Assert.That(state.NpcRespawnQueue.Count, Is.EqualTo(1));
    }
}
