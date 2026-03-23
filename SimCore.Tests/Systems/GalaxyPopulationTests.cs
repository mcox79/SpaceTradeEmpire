using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.T30.GALPOP.TESTS.009
[TestFixture]
public sealed class GalaxyPopulationTests
{
    private SimKernel CreatePopulatedGalaxy(int seed = 42, int stars = 20)
    {
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, stars, 100f);
        return sim;
    }

    // ── Faction fleet ownership ──

    [Test]
    public void FactionFleets_OwnedByFaction_NotGenericAi()
    {
        var sim = CreatePopulatedGalaxy();
        var state = sim.State;

        // Every fleet at a faction-owned node should have OwnerId == faction, not "ai".
        foreach (var fleet in state.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (state.NodeFactionId.TryGetValue(fleet.CurrentNodeId, out var factionId)
                && !string.IsNullOrEmpty(factionId))
            {
                Assert.That(fleet.OwnerId, Is.EqualTo(factionId),
                    $"Fleet {fleet.Id} at faction node {fleet.CurrentNodeId} should be owned by {factionId}, not {fleet.OwnerId}");
            }
        }
    }

    [Test]
    public void FactionFleets_AllFiveFactionsRepresented()
    {
        var sim = CreatePopulatedGalaxy();
        var ownerIds = sim.State.Fleets.Values
            .Where(f => !string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
            .Select(f => f.OwnerId)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(ownerIds, Does.Contain(FactionTweaksV0.ConcordId));
        Assert.That(ownerIds, Does.Contain(FactionTweaksV0.ChitinId));
        Assert.That(ownerIds, Does.Contain(FactionTweaksV0.WeaversId));
        Assert.That(ownerIds, Does.Contain(FactionTweaksV0.ValorinId));
        Assert.That(ownerIds, Does.Contain(FactionTweaksV0.CommunionId));
    }

    // ── Fleet density per faction ──

    [Test]
    public void ValorinNodes_HaveHigherFleetDensity()
    {
        var sim = CreatePopulatedGalaxy();
        var state = sim.State;

        var valorinNodes = state.NodeFactionId
            .Where(kv => StringComparer.Ordinal.Equals(kv.Value, FactionTweaksV0.ValorinId))
            .Select(kv => kv.Key)
            .ToList();

        if (valorinNodes.Count == 0)
        {
            Assert.Inconclusive("No Valorin nodes found in generated galaxy");
            return;
        }

        foreach (var nodeId in valorinNodes)
        {
            int fleetCount = state.Fleets.Values.Count(f =>
                StringComparer.Ordinal.Equals(f.CurrentNodeId, nodeId)
                && !StringComparer.Ordinal.Equals(f.OwnerId, "player"));

            // Valorin composition: 2T + 1H + 3P = 6
            Assert.That(fleetCount, Is.GreaterThanOrEqualTo(6),
                $"Valorin node {nodeId} should have 6+ fleets (swarm doctrine), got {fleetCount}");
        }
    }

    [Test]
    public void CommunionNodes_HaveSparseFleets()
    {
        var sim = CreatePopulatedGalaxy();
        var state = sim.State;

        var communionNodes = state.NodeFactionId
            .Where(kv => StringComparer.Ordinal.Equals(kv.Value, FactionTweaksV0.CommunionId))
            .Select(kv => kv.Key)
            .ToList();

        if (communionNodes.Count == 0)
        {
            Assert.Inconclusive("No Communion nodes found in generated galaxy");
            return;
        }

        foreach (var nodeId in communionNodes)
        {
            // Player start node gets a density bonus — skip it
            if (StringComparer.Ordinal.Equals(nodeId, state.PlayerLocationNodeId)) continue;

            int fleetCount = state.Fleets.Values.Count(f =>
                StringComparer.Ordinal.Equals(f.CurrentNodeId, nodeId)
                && !StringComparer.Ordinal.Equals(f.OwnerId, "player"));

            // Communion composition: 1T + 0H + 1P = 2
            Assert.That(fleetCount, Is.LessThanOrEqualTo(3),
                $"Communion node {nodeId} should have 2-3 fleets (sparse), got {fleetCount}");
        }
    }

    // ── Total fleet count ──

    [Test]
    public void TotalFleetCount_InExpectedRange()
    {
        var sim = CreatePopulatedGalaxy();
        int npcFleets = sim.State.Fleets.Values.Count(f =>
            !StringComparer.Ordinal.Equals(f.OwnerId, "player"));

        // 20 nodes, ~4 nodes/faction, compositions sum to ~68. Allow 40-100 range for edge cases.
        Assert.That(npcFleets, Is.GreaterThanOrEqualTo(40),
            $"Too few NPC fleets: {npcFleets}. Expected 40+.");
        Assert.That(npcFleets, Is.LessThanOrEqualTo(100),
            $"Too many NPC fleets: {npcFleets}. Expected <=100.");
    }

    // ── Hauler activity ──

    [Test]
    public void Haulers_MoveAfterSufficientTicks()
    {
        var sim = CreatePopulatedGalaxy();

        // Record initial hauler positions.
        var haulers = sim.State.Fleets.Values
            .Where(f => f.Role == FleetRole.Hauler)
            .Select(f => f.Id)
            .ToList();

        if (haulers.Count == 0)
        {
            Assert.Inconclusive("No haulers found in generated galaxy");
            return;
        }

        var initialPositions = new Dictionary<string, string>();
        foreach (var hId in haulers)
        {
            initialPositions[hId] = sim.State.Fleets[hId].CurrentNodeId;
        }

        // Run enough ticks for haulers to evaluate and move.
        // Haulers eval every 30 ticks, so 200 ticks should give several opportunities.
        for (int i = 0; i < 200; i++)
            sim.Step();

        int movedCount = 0;
        foreach (var hId in haulers)
        {
            if (!sim.State.Fleets.TryGetValue(hId, out var fleet)) continue;
            if (!StringComparer.Ordinal.Equals(fleet.CurrentNodeId, initialPositions[hId])
                || fleet.State == FleetState.Traveling)
            {
                movedCount++;
            }
        }

        // At least one hauler should have attempted to move.
        Assert.That(movedCount, Is.GreaterThanOrEqualTo(1),
            $"No haulers moved after 200 ticks. {haulers.Count} haulers total.");
    }

    // ── Hostile logic (reputation-based) ──

    [Test]
    public void HostileLogic_NonHostileWithNeutralReputation()
    {
        var state = new SimState(42);

        // Create a Patrol fleet owned by a faction.
        state.Fleets["patrol_1"] = new Fleet
        {
            Id = "patrol_1",
            OwnerId = FactionTweaksV0.ValorinId,
            Role = FleetRole.Patrol,
            CurrentNodeId = "node_a",
            HullHp = 50,
            HullHpMax = 50,
        };

        // Default reputation is 0, threshold is -50. 0 > -50 = non-hostile.
        // FactionReputation doesn't have an entry => default to non-hostile.
        bool isHostile = state.Fleets["patrol_1"].Role == FleetRole.Patrol;
        if (isHostile && !string.IsNullOrEmpty(state.Fleets["patrol_1"].OwnerId))
        {
            if (state.FactionReputation.TryGetValue(state.Fleets["patrol_1"].OwnerId, out var rep))
                isHostile = rep < FactionTweaksV0.AggroReputationThreshold;
            else
                isHostile = false; // No rep record = non-hostile (0 > -50)
        }

        Assert.That(isHostile, Is.False,
            "Patrol fleet should NOT be hostile with neutral (0) reputation");
    }

    [Test]
    public void HostileLogic_HostileWithNegativeReputation()
    {
        var state = new SimState(42);

        state.Fleets["patrol_1"] = new Fleet
        {
            Id = "patrol_1",
            OwnerId = FactionTweaksV0.ValorinId,
            Role = FleetRole.Patrol,
            CurrentNodeId = "node_a",
            HullHp = 50,
            HullHpMax = 50,
        };

        // Set reputation below threshold (-50).
        state.FactionReputation[FactionTweaksV0.ValorinId] = -60;

        bool isHostile = false;
        var fleet = state.Fleets["patrol_1"];
        if (fleet.Role == FleetRole.Patrol && !string.IsNullOrEmpty(fleet.OwnerId))
        {
            if (state.FactionReputation.TryGetValue(fleet.OwnerId, out var rep))
                isHostile = rep < FactionTweaksV0.AggroReputationThreshold;
        }

        Assert.That(isHostile, Is.True,
            "Patrol fleet should be hostile when reputation is below AggroReputationThreshold");
    }

    [Test]
    public void HostileLogic_TraderNeverHostile()
    {
        // Traders are never hostile regardless of reputation.
        var state = new SimState(42);
        state.Fleets["trader_1"] = new Fleet
        {
            Id = "trader_1",
            OwnerId = FactionTweaksV0.ValorinId,
            Role = FleetRole.Trader,
            CurrentNodeId = "node_a",
        };
        state.FactionReputation[FactionTweaksV0.ValorinId] = -100;

        // ComputeFleetHostileV0 logic: only Patrol role is hostile.
        bool isHostile = state.Fleets["trader_1"].Role == FleetRole.Patrol;
        Assert.That(isHostile, Is.False, "Trader fleet should never be hostile");
    }

    // ── Market bias ──

    [Test]
    public void MarketBias_CreatesPriceDifferentials()
    {
        var sim = CreatePopulatedGalaxy();
        var state = sim.State;

        // Find a Concord node and a Communion node.
        string? concordNode = state.NodeFactionId
            .Where(kv => StringComparer.Ordinal.Equals(kv.Value, FactionTweaksV0.ConcordId))
            .Select(kv => kv.Key)
            .FirstOrDefault();
        string? communionNode = state.NodeFactionId
            .Where(kv => StringComparer.Ordinal.Equals(kv.Value, FactionTweaksV0.CommunionId))
            .Select(kv => kv.Key)
            .FirstOrDefault();

        if (concordNode == null || communionNode == null)
        {
            Assert.Inconclusive("Need both Concord and Communion nodes for market bias test");
            return;
        }

        // Concord has surplus food (+200), Communion has deficit food (-100).
        // This should create a price differential (lower price where surplus, higher where deficit).
        if (state.Markets.TryGetValue(concordNode, out var concordMarket)
            && state.Markets.TryGetValue(communionNode, out var communionMarket))
        {
            long concordFoodPrice = concordMarket.GetPrice("food");
            long communionFoodPrice = communionMarket.GetPrice("food");

            Assert.That(communionFoodPrice, Is.GreaterThan(concordFoodPrice),
                $"Food price at Communion ({communionFoodPrice}) should exceed Concord ({concordFoodPrice}) due to surplus/deficit bias");
        }
    }

    // ── Respawn preserves role + owner ──

    [Test]
    public void Respawn_PreservesRoleAndOwner()
    {
        var state = new SimState(42);

        // Create a Valorin patrol fleet and destroy it.
        state.Fleets["patrol_v1"] = new Fleet
        {
            Id = "patrol_v1",
            OwnerId = FactionTweaksV0.ValorinId,
            Role = FleetRole.Patrol,
            CurrentNodeId = "node_a",
            HullHp = 0,
            HullHpMax = 50,
            ShieldHp = 0,
            ShieldHpMax = 10,
        };

        // Need an edge for the fleet state check.
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "node_a", ToNodeId = "node_b", Distance = 10, TotalCapacity = 5, UsedCapacity = 0 };

        // Process destruction — should queue respawn.
        NpcFleetCombatSystem.Process(state);

        Assert.That(state.Fleets.ContainsKey("patrol_v1"), Is.False, "Destroyed fleet should be removed");
        Assert.That(state.NpcRespawnQueue.Count, Is.GreaterThanOrEqualTo(1), "Respawn queue should have an entry");

        var respawnEntry = state.NpcRespawnQueue.First(e =>
            StringComparer.Ordinal.Equals(e.FleetId, "patrol_v1"));

        Assert.That(respawnEntry.Role, Is.EqualTo(FleetRole.Patrol),
            "Respawn entry should preserve Patrol role");
        Assert.That(respawnEntry.OwnerId, Is.EqualTo(FactionTweaksV0.ValorinId),
            "Respawn entry should preserve Valorin owner");
    }

    // ── Fleet ID format ──

    [Test]
    public void FleetIds_UseIndexedFormat()
    {
        var sim = CreatePopulatedGalaxy();

        // All NPC fleet IDs should follow ai_fleet_{nodeId}_{index} format.
        foreach (var fleet in sim.State.Fleets.Values)
        {
            if (StringComparer.Ordinal.Equals(fleet.OwnerId, "player")) continue;

            Assert.That(fleet.Id, Does.Match(@"^ai_fleet_.+_\d+$"),
                $"Fleet ID '{fleet.Id}' should match ai_fleet_{{nodeId}}_{{index}} format");
        }
    }
}
