using System;
using System.Collections.Generic;
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

        for (int i = 0; i < starCount; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(rng.NextDouble() * 2 - 1) * radius;

            var node = new Node
            {
                Id = $"star_{i}",
                Name = $"System {i}",
                Position = new Vector3(x, 0, z),
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

    public static void SeedAiFleets(SimState state, List<Node> nodesList)
    {
        foreach (var node in nodesList)
        {
            // GATE.S14.NPC_ALIVE.FLEET_SEED.001: deterministic role diversity.
            uint roleHash = GalaxyGenerator.Fnv1a32Utf8(node.Id + "_fleet_role");
            int bucket = (int)(roleHash % FleetSeedTweaksV0.BucketSize);
            FleetRole role;
            float speed;
            if (bucket < FleetSeedTweaksV0.TraderThreshold) { role = FleetRole.Trader; speed = FleetSeedTweaksV0.TraderSpeed; }
            else if (bucket < FleetSeedTweaksV0.HaulerThreshold) { role = FleetRole.Hauler; speed = FleetSeedTweaksV0.HaulerSpeed; }
            else { role = FleetRole.Patrol; speed = FleetSeedTweaksV0.PatrolSpeed; }

            // Q5: No hostile patrol at player's starting system — new players can dock safely.
            if (node.Id == state.PlayerLocationNodeId && role == FleetRole.Patrol)
            {
                role = FleetRole.Trader;
                speed = FleetSeedTweaksV0.TraderSpeed;
            }

            var fleet = new Fleet
            {
                Id = $"ai_fleet_{node.Id}",
                OwnerId = "ai",
                Role = role,
                CurrentNodeId = node.Id,
                Speed = speed,
                State = FleetState.Idle,
                FuelCapacity = Tweaks.NpcShipTweaksV0.DefaultFuelCapacity,
                FuelCurrent = Tweaks.NpcShipTweaksV0.DefaultFuelCapacity,
            };
            state.Fleets.Add(fleet.Id, fleet);
        }
    }

    /// <summary>
    /// GATE.S14.NPC_ALIVE.FLEET_SEED.001: Re-seed AI fleets from existing nodes.
    /// Called by WorldLoader after Fleets.Clear() + player fleet creation.
    /// Uses state.Nodes for deterministic iteration order.
    /// </summary>
    public static void SeedAiFleetsFromState(SimState state)
    {
        var nodesList = new List<Node>();
        foreach (var kv in state.Nodes.OrderBy(n => n.Key, StringComparer.Ordinal))
            nodesList.Add(kv.Value);
        SeedAiFleets(state, nodesList);
    }
}
