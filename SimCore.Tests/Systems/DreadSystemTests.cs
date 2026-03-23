using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.T45 coverage: DreadDrainSystem, SensorGhostSystem, LatticeFaunaSystem.
[TestFixture]
public sealed class DreadSystemTests
{
    /// <summary>Build minimal state with player fleet at a Phase 2 node.</summary>
    private static SimState MakeDreadState(int instabilityLevel = 50, int hull = 100)
    {
        var state = new SimState(42);

        state.Nodes["nodeA"] = new Node { Id = "nodeA", InstabilityLevel = instabilityLevel };
        state.Nodes["nodeB"] = new Node { Id = "nodeB", InstabilityLevel = 0 };
        state.Edges["e_A_B"] = new Edge
        {
            Id = "e_A_B", FromNodeId = "nodeA", ToNodeId = "nodeB", Distance = 5f
        };
        state.PlayerLocationNodeId = "nodeA";
        state.Fleets["player_fleet"] = new Fleet
        {
            Id = "player_fleet", OwnerId = "player", HullHp = hull, HullHpMax = 100,
            Speed = 1.0f, FuelCurrent = 50, FuelCapacity = 100,
        };

        return state;
    }

    // =========================================
    // DreadDrainSystem
    // =========================================

    [Test]
    public void DreadDrain_NoDrainBelowPhase2()
    {
        var state = MakeDreadState(instabilityLevel: 24); // Phase 0 (Stable)
        int hullBefore = state.Fleets["player_fleet"].HullHp;
        for (int i = 0; i < 200; i++)
        {
            DreadDrainSystem.Process(state);
            state.AdvanceTick();
        }
        Assert.That(state.Fleets["player_fleet"].HullHp, Is.EqualTo(hullBefore),
            "Hull should not drain at Phase 0");
    }

    [Test]
    public void DreadDrain_DrainsAtPhase2()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.DriftMin); // Phase 2
        int hullBefore = state.Fleets["player_fleet"].HullHp;

        // Advance exactly one drain interval.
        while (state.Tick % DeepDreadTweaksV0.Phase2DrainIntervalTicks != 0)
            state.AdvanceTick();

        DreadDrainSystem.Process(state);

        Assert.That(state.Fleets["player_fleet"].HullHp,
            Is.EqualTo(hullBefore - DeepDreadTweaksV0.Phase2DrainAmount),
            "Hull should drain by Phase2DrainAmount at Phase 2 interval");
    }

    [Test]
    public void DreadDrain_DrainsAtPhase3_Faster()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin); // Phase 3
        int hullBefore = state.Fleets["player_fleet"].HullHp;

        while (state.Tick % DeepDreadTweaksV0.Phase3DrainIntervalTicks != 0)
            state.AdvanceTick();

        DreadDrainSystem.Process(state);

        Assert.That(state.Fleets["player_fleet"].HullHp,
            Is.EqualTo(hullBefore - DeepDreadTweaksV0.Phase3DrainAmount),
            "Hull should drain by Phase3DrainAmount at Phase 3 interval");
    }

    [Test]
    public void DreadDrain_NoDrainAtPhase4_VoidParadox()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.VoidMin); // Phase 4
        int hullBefore = state.Fleets["player_fleet"].HullHp;
        for (int i = 0; i < 200; i++)
        {
            DreadDrainSystem.Process(state);
            state.AdvanceTick();
        }
        Assert.That(state.Fleets["player_fleet"].HullHp, Is.EqualTo(hullBefore),
            "Void paradox: no drain at Phase 4");
    }

    [Test]
    public void DreadDrain_AccommodationModuleGivesImmunity()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.DriftMin);
        var fleet = state.Fleets["player_fleet"];
        fleet.Slots.Add(new ModuleSlot
        {
            InstalledModuleId = DeepDreadTweaksV0.AccommodationModuleId
        });
        int hullBefore = fleet.HullHp;

        for (int i = 0; i < 200; i++)
        {
            DreadDrainSystem.Process(state);
            state.AdvanceTick();
        }
        Assert.That(fleet.HullHp, Is.EqualTo(hullBefore),
            "Accommodation module should prevent drain");
    }

    [Test]
    public void DreadDrain_HullFloorsAtZero()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin, hull: 1);

        while (state.Tick % DeepDreadTweaksV0.Phase3DrainIntervalTicks != 0)
            state.AdvanceTick();

        DreadDrainSystem.Process(state);

        Assert.That(state.Fleets["player_fleet"].HullHp, Is.GreaterThanOrEqualTo(0),
            "Hull should never go below zero");
    }

    // =========================================
    // SensorGhostSystem
    // =========================================

    [Test]
    public void SensorGhost_NoSpawnBelowMinPhase()
    {
        var state = MakeDreadState(instabilityLevel: 24); // Phase 0

        for (int i = 0; i < 500; i++)
        {
            SensorGhostSystem.Process(state);
            state.AdvanceTick();
        }

        Assert.That(state.SensorGhosts, Is.Empty,
            "No ghosts should spawn below GhostMinPhase");
    }

    [Test]
    public void SensorGhost_SpawnsAtPhase2()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.DriftMin); // Phase 2

        // Run enough ticks for at least one spawn opportunity.
        bool anySpawned = false;
        for (int i = 0; i < 2000; i++)
        {
            SensorGhostSystem.Process(state);
            if (state.SensorGhosts != null && state.SensorGhosts.Count > 0)
            {
                anySpawned = true;
                break;
            }
            state.AdvanceTick();
        }

        Assert.That(anySpawned, Is.True,
            "Ghosts should eventually spawn at Phase 2 with enough ticks");
    }

    [Test]
    public void SensorGhost_GhostsExpire()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.DriftMin);

        // Spawn some ghosts.
        for (int i = 0; i < 1000; i++)
        {
            SensorGhostSystem.Process(state);
            state.AdvanceTick();
        }

        if (state.SensorGhosts == null || state.SensorGhosts.Count == 0)
        {
            Assert.Inconclusive("No ghosts spawned to test expiry");
            return;
        }

        // Record current ghost, advance past its expiry.
        var ghost = state.SensorGhosts[0];
        while (state.Tick < ghost.ExpiryTick + 1)
        {
            SensorGhostSystem.Process(state);
            state.AdvanceTick();
        }

        SensorGhostSystem.Process(state);
        Assert.That(state.SensorGhosts.Find(g => g.Id == ghost.Id), Is.Null,
            "Expired ghost should be removed");
    }

    [Test]
    public void SensorGhost_MaxConcurrentRespected()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.VoidMin); // Phase 4, max spawn rate

        for (int i = 0; i < 5000; i++)
        {
            SensorGhostSystem.Process(state);
            Assert.That(state.SensorGhosts?.Count ?? 0,
                Is.LessThanOrEqualTo(DeepDreadTweaksV0.GhostMaxConcurrent),
                $"Max concurrent ghosts exceeded at tick {state.Tick}");
            state.AdvanceTick();
        }
    }

    [Test]
    public void SensorGhost_HasValidFields()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.DriftMin);

        for (int i = 0; i < 2000; i++)
        {
            SensorGhostSystem.Process(state);
            if (state.SensorGhosts != null && state.SensorGhosts.Count > 0) break;
            state.AdvanceTick();
        }

        if (state.SensorGhosts == null || state.SensorGhosts.Count == 0)
        {
            Assert.Inconclusive("No ghosts spawned");
            return;
        }

        var ghost = state.SensorGhosts[0];
        Assert.That(ghost.Id, Is.Not.Empty, "Ghost should have an Id");
        Assert.That(ghost.NodeId, Is.Not.Empty, "Ghost should have a NodeId");
        Assert.That(ghost.ApparentFleetType, Is.Not.Empty, "Ghost should have a type");
        Assert.That(ghost.ExpiryTick, Is.GreaterThan(ghost.SpawnTick), "Expiry should be after spawn");
    }

    [Test]
    public void SensorGhost_IsDeterministic()
    {
        // Run twice with same seed — ghost history must match.
        var ids1 = RunGhostHistory(42);
        var ids2 = RunGhostHistory(42);

        Assert.That(ids1, Is.EqualTo(ids2), "Ghost spawn sequence must be deterministic");
    }

    private static List<string> RunGhostHistory(int seed)
    {
        var state = new SimState(seed);
        state.Nodes["nodeA"] = new Node { Id = "nodeA", InstabilityLevel = InstabilityTweaksV0.DriftMin };
        state.Nodes["nodeB"] = new Node { Id = "nodeB", InstabilityLevel = 0 };
        state.Edges["e_A_B"] = new Edge { Id = "e_A_B", FromNodeId = "nodeA", ToNodeId = "nodeB", Distance = 5f };
        state.PlayerLocationNodeId = "nodeA";
        state.Fleets["pf"] = new Fleet { Id = "pf", OwnerId = "player", HullHp = 100, Speed = 1f };

        var ids = new List<string>();
        for (int i = 0; i < 500; i++)
        {
            SensorGhostSystem.Process(state);
            if (state.SensorGhosts != null)
            {
                foreach (var g in state.SensorGhosts)
                {
                    if (!ids.Contains(g.Id)) ids.Add(g.Id);
                }
            }
            state.AdvanceTick();
        }
        return ids;
    }

    // =========================================
    // LatticeFaunaSystem
    // =========================================

    [Test]
    public void LatticeFauna_NoSpawnBelowMinPhase()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.DriftMin); // Phase 2, need Phase 3+
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;

        for (int i = 0; i < 500; i++)
        {
            LatticeFaunaSystem.Process(state);
            state.AdvanceTick();
        }

        Assert.That(state.LatticeFauna?.Count ?? 0, Is.EqualTo(0),
            "No fauna should spawn below SpawnMinPhase (3)");
    }

    [Test]
    public void LatticeFauna_NoSpawnWithoutFractureSignature()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin); // Phase 3
        state.FractureUnlocked = false;
        state.FractureExposureJumps = 0;

        for (int i = 0; i < 500; i++)
        {
            LatticeFaunaSystem.Process(state);
            state.AdvanceTick();
        }

        Assert.That(state.LatticeFauna?.Count ?? 0, Is.EqualTo(0),
            "No fauna should spawn without fracture signature");
    }

    [Test]
    public void LatticeFauna_SpawnsAtPhase3WithSignature()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin);
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;

        bool anySpawned = false;
        for (int i = 0; i < 2000; i++)
        {
            LatticeFaunaSystem.Process(state);
            if (state.LatticeFauna != null && state.LatticeFauna.Count > 0)
            {
                anySpawned = true;
                break;
            }
            state.AdvanceTick();
        }

        Assert.That(anySpawned, Is.True,
            "Fauna should eventually spawn at Phase 3 with fracture signature");
    }

    [Test]
    public void LatticeFauna_MaxConcurrentRespected()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin);
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;

        for (int i = 0; i < 5000; i++)
        {
            LatticeFaunaSystem.Process(state);
            Assert.That(state.LatticeFauna?.Count ?? 0,
                Is.LessThanOrEqualTo(LatticeFaunaTweaksV0.MaxConcurrent),
                $"Max concurrent fauna exceeded at tick {state.Tick}");
            state.AdvanceTick();
        }
    }

    [Test]
    public void LatticeFauna_ApproachingTransitionsToPresent()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin);
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;

        // Keep running until we get an Approaching fauna.
        LatticeFauna? approaching = null;
        for (int i = 0; i < 2000; i++)
        {
            LatticeFaunaSystem.Process(state);
            state.AdvanceTick();
            if (state.LatticeFauna != null)
            {
                approaching = state.LatticeFauna.Find(f => f.State == LatticeFaunaState.Approaching);
                if (approaching != null) break;
            }
        }

        if (approaching == null)
        {
            Assert.Inconclusive("No fauna spawned");
            return;
        }

        // Advance to arrival tick.
        while (state.Tick < approaching.ArrivalTick)
        {
            LatticeFaunaSystem.Process(state);
            state.AdvanceTick();
        }
        LatticeFaunaSystem.Process(state);

        Assert.That(approaching.State, Is.EqualTo(LatticeFaunaState.Present),
            "Fauna should transition to Present after ArrivalTick");
    }

    [Test]
    public void LatticeFauna_FuelDrainWhilePresent()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin);
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;
        // Set player as "not dark" (has a destination).
        state.PlayerSelectedDestinationNodeId = "nodeB";

        // Manually inject a Present fauna.
        state.LatticeFauna = new List<LatticeFauna>
        {
            new LatticeFauna
            {
                Id = "test_fauna", NodeId = "nodeA",
                State = LatticeFaunaState.Present, SpawnTick = 0, ArrivalTick = 0
            }
        };

        int fuelBefore = state.Fleets["player_fleet"].FuelCurrent;
        LatticeFaunaSystem.Process(state);

        Assert.That(state.Fleets["player_fleet"].FuelCurrent,
            Is.EqualTo(fuelBefore - LatticeFaunaTweaksV0.FuelDrainPerTick),
            "Fauna should drain fuel while present and player is active");
    }

    [Test]
    public void LatticeFauna_GoingDarkCausesDeparture()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin);
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;
        state.PlayerSelectedDestinationNodeId = ""; // Going dark

        state.LatticeFauna = new List<LatticeFauna>
        {
            new LatticeFauna
            {
                Id = "test_fauna", NodeId = "nodeA",
                State = LatticeFaunaState.Present, SpawnTick = 0, ArrivalTick = 0,
                DarkTicksAccumulated = LatticeFaunaTweaksV0.GoDarkTicks - 1
            }
        };

        LatticeFaunaSystem.Process(state);

        // Fauna should transition to Departing.
        Assert.That(state.LatticeFauna[0].State, Is.EqualTo(LatticeFaunaState.Departing),
            "Fauna should depart after GoDarkTicks");
    }

    [Test]
    public void LatticeFauna_ResidueLeftAfterDeparture()
    {
        var state = MakeDreadState(instabilityLevel: InstabilityTweaksV0.FractureMin);
        state.FractureUnlocked = true;
        state.FractureExposureJumps = 1;
        state.LatticeFaunaResidue = new Dictionary<string, int>();

        state.LatticeFauna = new List<LatticeFauna>
        {
            new LatticeFauna
            {
                Id = "test_fauna", NodeId = "nodeA",
                State = LatticeFaunaState.Departing, SpawnTick = 0, ArrivalTick = 0
            }
        };

        LatticeFaunaSystem.Process(state);

        Assert.That(state.LatticeFaunaResidue.ContainsKey("nodeA"), Is.True,
            "Departing fauna should leave residue at its node");
        Assert.That(state.LatticeFauna, Is.Empty,
            "Departed fauna should be removed from list");
    }
}
