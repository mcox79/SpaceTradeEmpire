using System.Collections.Generic;
using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.S8.LATTICE_DRONES.ENTITY.001: Entity + fleet flag.
/// GATE.S8.LATTICE_DRONES.SPAWN.001: Instability-phase-linked spawning.
/// GATE.S8.LATTICE_DRONES.COMBAT.001: Drone combat behavior.
/// GATE.S7.COMBAT_DEPTH2.PROJECTION.001: Pre-combat projection.
/// </summary>
[TestFixture]
public class LatticeDroneTests
{
    private static SimState MakeState(int instabilityLevel)
    {
        var state = new SimState();
        state.Nodes["node_a"] = new Node { Id = "node_a", InstabilityLevel = instabilityLevel };
        return state;
    }

    /// <summary>Advance state tick to the next spawn check interval.</summary>
    private static void AdvanceToSpawnCheck(SimState state)
    {
        for (int i = 0; i < LatticeDroneTweaksV0.SpawnCheckIntervalTicks; i++)
            state.AdvanceTick();
    }

    // ── Entity tests ──

    [Test]
    public void Fleet_IsLatticeDrone_DefaultFalse()
    {
        var fleet = new Fleet { Id = "test" };
        Assert.That(fleet.IsLatticeDrone, Is.False);
    }

    [Test]
    public void Fleet_IsLatticeDrone_SetTrue()
    {
        var fleet = new Fleet { Id = "ld_test", IsLatticeDrone = true };
        Assert.That(fleet.IsLatticeDrone, Is.True);
    }

    // ── Spawn tests ──

    [Test]
    public void Spawn_Phase0_NoDrones()
    {
        var state = MakeState(0);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(0));
    }

    [Test]
    public void Spawn_Phase1_NoDrones()
    {
        // Shimmer phase (index 1) — below spawn threshold.
        var state = MakeState(InstabilityTweaksV0.ShimmerMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(0));
    }

    [Test]
    public void Spawn_Phase2_SpawnsDrone()
    {
        // Drift phase (index 2) — territorial drones.
        var state = MakeState(InstabilityTweaksV0.DriftMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(1));
    }

    [Test]
    public void Spawn_Phase3_SpawnsDrone()
    {
        // Fracture phase (index 3) — hostile drones.
        var state = MakeState(InstabilityTweaksV0.FractureMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(1));
    }

    [Test]
    public void Spawn_Phase4_NoDrones()
    {
        // Void phase (index 4) — too unstable.
        var state = MakeState(InstabilityTweaksV0.VoidMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(0));
    }

    [Test]
    public void Spawn_MaxPerNode_Respected()
    {
        var state = MakeState(InstabilityTweaksV0.DriftMin);
        // Spawn drones by ticking at check intervals.
        for (int i = 0; i < LatticeDroneTweaksV0.MaxDronesPerNode + 2; i++)
        {
            AdvanceToSpawnCheck(state);
            LatticeDroneSpawnSystem.Process(state);
        }

        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"),
            Is.LessThanOrEqualTo(LatticeDroneTweaksV0.MaxDronesPerNode));
    }

    [Test]
    public void Spawn_Despawn_WhenPhaseDrops()
    {
        var state = MakeState(InstabilityTweaksV0.DriftMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);
        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(1));

        // Drop instability to phase 0.
        state.Nodes["node_a"].InstabilityLevel = 0;
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);
        Assert.That(LatticeDroneSpawnSystem.CountDronesAtNode(state, "node_a"), Is.EqualTo(0));
    }

    [Test]
    public void Spawn_DroneHasCorrectStats()
    {
        var state = MakeState(InstabilityTweaksV0.DriftMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Fleet? drone = null;
        foreach (var f in state.Fleets.Values)
            if (f.IsLatticeDrone) { drone = f; break; }

        Assert.That(drone, Is.Not.Null);
        Assert.That(drone!.HullHp, Is.EqualTo(LatticeDroneTweaksV0.DroneHullHp));
        Assert.That(drone.ShieldHp, Is.EqualTo(LatticeDroneTweaksV0.DroneShieldHp));
        Assert.That(drone.ShipClassId, Is.EqualTo(LatticeDroneTweaksV0.DroneShipClassId));
        Assert.That(drone.BattleStations, Is.EqualTo(BattleStationsState.BattleReady));
    }

    [Test]
    public void Spawn_TerritorialDrone_HasGracePeriod()
    {
        var state = MakeState(InstabilityTweaksV0.DriftMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Fleet? drone = null;
        foreach (var f in state.Fleets.Values)
            if (f.IsLatticeDrone) { drone = f; break; }

        Assert.That(drone!.LatticeDroneGraceTicksRemaining, Is.EqualTo(LatticeDroneTweaksV0.WarningGraceTicks));
    }

    [Test]
    public void Spawn_HostileDrone_NoGracePeriod()
    {
        var state = MakeState(InstabilityTweaksV0.FractureMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        Fleet? drone = null;
        foreach (var f in state.Fleets.Values)
            if (f.IsLatticeDrone) { drone = f; break; }

        Assert.That(drone!.LatticeDroneGraceTicksRemaining, Is.EqualTo(0));
    }

    // ── Combat tests ──

    [Test]
    public void DroneCombat_GracePeriod_DoesNotAttack()
    {
        var state = MakeState(InstabilityTweaksV0.DriftMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        // Add player fleet at same node.
        var player = new Fleet
        {
            Id = "player_fleet",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            HullHp = 100, HullHpMax = 100,
            ShieldHp = 50, ShieldHpMax = 50,
            BattleStations = BattleStationsState.BattleReady,
        };
        player.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
        state.Fleets[player.Id] = player;

        // Process combat — drone should warn, not attack.
        LatticeDroneCombatSystem.Process(state);

        Assert.That(player.HullHp, Is.EqualTo(100), "Player hull unchanged during grace period");
    }

    [Test]
    public void DroneCombat_HostileDrone_DealsDamage()
    {
        var state = MakeState(InstabilityTweaksV0.FractureMin);
        AdvanceToSpawnCheck(state);
        LatticeDroneSpawnSystem.Process(state);

        var player = new Fleet
        {
            Id = "player_fleet",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            HullHp = 100, HullHpMax = 100,
            ShieldHp = 50, ShieldHpMax = 50,
            BattleStations = BattleStationsState.BattleReady,
        };
        player.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
        state.Fleets[player.Id] = player;

        int totalHpBefore = player.HullHp + player.ShieldHp;
        LatticeDroneCombatSystem.Process(state);
        int totalHpAfter = player.HullHp + player.ShieldHp;

        // Hostile drone attacks immediately — player should take some damage.
        Assert.That(totalHpAfter, Is.LessThan(totalHpBefore), "Player should take damage from hostile drone");
    }

    // ── Projection tests ──

    [Test]
    public void Projection_StrongVsWeak_Victory()
    {
        var attacker = new Fleet
        {
            Id = "att", HullHp = 200, HullHpMax = 200, ShieldHp = 100, ShieldHpMax = 100,
            BattleStations = BattleStationsState.BattleReady,
        };
        attacker.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
        attacker.Slots.Add(new ModuleSlot { SlotId = "w2", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });

        var defender = new Fleet
        {
            Id = "def", HullHp = 30, HullHpMax = 30, ShieldHp = 10, ShieldHpMax = 10,
            BattleStations = BattleStationsState.BattleReady,
        };
        defender.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });

        var projection = CombatSystem.ProjectOutcome(attacker, defender);

        Assert.That(projection.Outcome, Is.EqualTo(CombatSystem.ProjectedOutcome.Victory));
        Assert.That(projection.AttackerLossPct, Is.LessThan(50));
    }

    [Test]
    public void Projection_WeakVsStrong_Defeat()
    {
        var attacker = new Fleet
        {
            Id = "att", HullHp = 30, HullHpMax = 30, ShieldHp = 10, ShieldHpMax = 10,
            BattleStations = BattleStationsState.BattleReady,
        };
        attacker.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });

        var defender = new Fleet
        {
            Id = "def", HullHp = 200, HullHpMax = 200, ShieldHp = 100, ShieldHpMax = 100,
            BattleStations = BattleStationsState.BattleReady,
        };
        defender.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
        defender.Slots.Add(new ModuleSlot { SlotId = "w2", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });

        var projection = CombatSystem.ProjectOutcome(attacker, defender);

        Assert.That(projection.Outcome, Is.EqualTo(CombatSystem.ProjectedOutcome.Defeat));
    }

    [Test]
    public void Projection_IsDeterministic()
    {
        var makeFleet = (string id) =>
        {
            var f = new Fleet
            {
                Id = id, HullHp = 100, HullHpMax = 100, ShieldHp = 50, ShieldHpMax = 50,
                BattleStations = BattleStationsState.BattleReady,
            };
            f.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
            return f;
        };

        var p1 = CombatSystem.ProjectOutcome(makeFleet("a"), makeFleet("b"));
        var p2 = CombatSystem.ProjectOutcome(makeFleet("a"), makeFleet("b"));

        Assert.That(p1.Outcome, Is.EqualTo(p2.Outcome));
        Assert.That(p1.EstimatedRounds, Is.EqualTo(p2.EstimatedRounds));
        Assert.That(p1.AttackerLossPct, Is.EqualTo(p2.AttackerLossPct));
    }
}
