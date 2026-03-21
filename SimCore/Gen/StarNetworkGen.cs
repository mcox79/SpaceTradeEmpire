using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Gen;

/// <summary>
/// Star network topology generation: node placement, lane wiring, AI fleet seeding.
/// Extracted from GalaxyGenerator for maintainability (GATE.X.HYGIENE.GALAXY_GEN_SPLIT.001).
/// </summary>
public static class StarNetworkGen
{
    public static List<Node> PlaceNodes(SimState state, int starCount, float radius)
    {
        var rng = state.Rng ?? throw new InvalidOperationException("SimState.Rng is null.");
        var nodesList = new List<Node>();

        // Generate unique system names from a separate RNG (derived from main seed)
        // so name generation doesn't shift position RNG sequence.
        // STRUCTURAL: Separate RNG for names so position sequence is unchanged.
        const int STRUCT_NAME_SALT = 0x4E414D45; // "NAME" as ASCII hex
        var nameRng = new Random(state.InitialSeed ^ STRUCT_NAME_SALT);
        var names = SystemNameGen.GenerateUnique(nameRng, starCount);

        for (int i = 0; i < starCount; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(rng.NextDouble() * 2 - 1) * radius;

            // STRUCTURAL: 2.5D disc shape — Y-spread modulated by radial distance from center.
            float yRaw = (float)(rng.NextDouble() * 2 - 1); // STRUCTURAL: same RNG pattern as x/z
            float radialDist = MathF.Sqrt(x * x + z * z) / radius;
            float yFalloff = Math.Clamp(1.0f - radialDist * GalaxyShapeTweaksV0.RadialFalloff, 0f, 1f); // STRUCTURAL: clamp [0,1]
            float y = yRaw * GalaxyShapeTweaksV0.DiscThicknessFraction * radius * yFalloff;

            var node = new Node
            {
                Id = $"star_{i}",
                Name = names[i],
                Position = new Vector3(x, y, z),
                Kind = NodeKind.Star,
                MarketId = $"star_{i}"
            };
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);
        }

        return nodesList;
    }

    public static void WireLanes(SimState state, List<Node> nodesList)
    {
        int starterN = Math.Min(nodesList.Count, GalaxyGenerator.StarterRegionNodeCount);
        int laneCounter = 0;
        var laneKey = new HashSet<string>(StringComparer.Ordinal);

        void AddLane(Node a, Node b, int capacity)
        {
            var u = a.Id;
            var v = b.Id;
            if (string.CompareOrdinal(u, v) > 0)
            {
                (u, v) = (v, u);
            }

            var key = $"{u}|{v}";
            if (!laneKey.Add(key)) return;

            laneCounter++;
            string id = $"lane_{laneCounter:D4}";
            state.Edges.Add(id, new Edge
            {
                Id = id,
                FromNodeId = u,
                ToNodeId = v,
                Distance = Vector3.Distance(a.Position, b.Position),
                TotalCapacity = capacity
            });
        }

        if (starterN >= 2)
        {
            for (int i = 0; i < starterN; i++)
            {
                var a = nodesList[i];
                var b = nodesList[(i + 1) % starterN];
                AddLane(a, b, capacity: 5);
            }

            for (int i = 0; i < starterN && laneKey.Count < 18; i++)
            {
                var a = nodesList[i];
                var b = nodesList[(i + 2) % starterN];
                AddLane(a, b, capacity: 4);
            }

            for (int i = 0; i < starterN && laneKey.Count < 18; i++)
            {
                var a = nodesList[i];
                var b = nodesList[(i + 3) % starterN];
                AddLane(a, b, capacity: 3);
            }
        }

        for (int i = starterN; i < nodesList.Count; i++)
        {
            AddLane(nodesList[i - 1], nodesList[i], capacity: 5);
        }
    }

    // GATE.T30.GALPOP.FACTION_SEED.004: Faction-aware fleet seeding.
    // Each faction seeds its controlled nodes with a fleet composition from FleetPopulationTweaksV0.
    // Must be called AFTER SeedFactionTerritoriesV0 so NodeFactionId is populated.
    public static void SeedAiFleets(SimState state, List<Node> nodesList)
    {
        foreach (var node in nodesList)
        {
            string factionId = state.NodeFactionId.TryGetValue(node.Id, out var fid) ? fid : "";
            var (traders, haulers, patrols) = FleetPopulationTweaksV0.GetComposition(factionId);
            string ownerId = string.IsNullOrEmpty(factionId) ? "ai" : factionId;

            int idx = 0;
            for (int t = 0; t < traders; t++)
                CreateFleet(state, node.Id, ownerId, FleetRole.Trader, idx++);
            for (int h = 0; h < haulers; h++)
                CreateFleet(state, node.Id, ownerId, FleetRole.Hauler, idx++);
            for (int p = 0; p < patrols; p++)
                CreateFleet(state, node.Id, ownerId, FleetRole.Patrol, idx++);
        }
    }

    private static void CreateFleet(SimState state, string nodeId, string ownerId, FleetRole role, int index)
    {
        float speed = role switch
        {
            FleetRole.Trader => FleetSeedTweaksV0.TraderSpeed,
            FleetRole.Hauler => FleetSeedTweaksV0.HaulerSpeed,
            FleetRole.Patrol => FleetSeedTweaksV0.PatrolSpeed,
            _ => FleetSeedTweaksV0.TraderSpeed,
        };

        // NPC weapon assignment by role: patrols get cannons, others get lasers.
        string weaponId = role == FleetRole.Patrol
            ? Content.WellKnownModuleIds.WeaponCannonMk1
            : Content.WellKnownModuleIds.WeaponLaserMk1;

        var fleet = new Fleet
        {
            Id = $"ai_fleet_{nodeId}_{index}",
            OwnerId = ownerId,
            Role = role,
            CurrentNodeId = nodeId,
            Speed = speed,
            State = FleetState.Idle,
            FuelCapacity = NpcShipTweaksV0.DefaultFuelCapacity,
            FuelCurrent = NpcShipTweaksV0.DefaultFuelCapacity,
        };
        fleet.Slots.Add(new Entities.ModuleSlot
        {
            SlotId = "npc_weapon_0",
            SlotKind = Entities.SlotKind.Weapon,
            InstalledModuleId = weaponId,
        });
        state.Fleets.Add(fleet.Id, fleet);
    }

    /// <summary>
    /// GATE.T30.GALPOP.FACTION_SEED.004: Re-seed AI fleets from existing nodes.
    /// Called by WorldLoader after Fleets.Clear() + player fleet creation.
    /// Uses state.Nodes for deterministic iteration order.
    /// NodeFactionId must be populated before calling (WorldLoader does this).
    /// </summary>
    public static void SeedAiFleetsFromState(SimState state)
    {
        var nodesList = new List<Node>();
        foreach (var kv in state.Nodes.OrderBy(n => n.Key, StringComparer.Ordinal))
            nodesList.Add(kv.Value);
        SeedAiFleets(state, nodesList);
    }
}
