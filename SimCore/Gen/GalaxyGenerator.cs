using System.Numerics;
using SimCore.Entities;
using System.Linq;
using System.Collections.Generic;
using System;

namespace SimCore.Gen;

public static class GalaxyGenerator
{
    public static void Generate(SimState state, int starCount, float radius)
    {
        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear();
        state.Fleets.Clear();
        state.IndustrySites.Clear();

        var nodesList = new List<Node>();
        var rng = state.Rng ?? throw new InvalidOperationException("SimState.Rng is null.");

        // 1. SCATTER STARS & GENERATE ECONOMY
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
                // FIX: ID UNIFICATION. Market ID matches Node ID.
                MarketId = $"star_{i}"
            };
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);

            // MARKET SETUP
            var mkt = new Market { Id = node.MarketId };
            mkt.Inventory["fuel"] = 100;

            // INDUSTRY DISTRIBUTION (Simple Toggle)
            // Even = Mining Colony (Supplier)
            // Odd = Industrial Hub (Consumer)
            if (i % 2 == 0)
            {
                // MINE: Has Ore, Produces Ore
                mkt.Inventory["ore"] = 500; // Abundant Supply
                
                var mine = new IndustrySite
                {
                    Id = $"mine_{i}",
                    NodeId = node.Id,
                    Outputs = new Dictionary<string, int> { { "ore", 5 } }
                };
                state.IndustrySites.Add(mine.Id, mine);
                node.Name += " (Mining)";
            }
            else
            {
                // REFINERY: Needs Ore, Has None (Triggers Logistics)
                mkt.Inventory["ore"] = 0; // Starved!
                
                var factory = new IndustrySite
                {
                    Id = $"fac_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int> { { "ore", 10 } },
                    Outputs = new Dictionary<string, int> { { "metal", 5 } }
                };
                state.IndustrySites.Add(factory.Id, factory);
                node.Name += " (Refinery)";
            }
            
            state.Markets.Add(node.MarketId, mkt);
        }
        
        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;

        // 2. CONNECTIVITY SKELETON (Prim's Algorithm)
        var connected = new HashSet<string>();
        var disconnected = new HashSet<string>(nodesList.Select(n => n.Id));
        
        var startNode = nodesList[0];
        connected.Add(startNode.Id);
        disconnected.Remove(startNode.Id);

        while (disconnected.Count > 0)
        {
            Node? bestA = null;
            Node? bestB = null;
            float bestDist = float.MaxValue;

            foreach (var idA in connected)
            {
                var nodeA = state.Nodes[idA];
                foreach (var idB in disconnected)
                {
                    var nodeB = state.Nodes[idB];
                    float d = Vector3.Distance(nodeA.Position, nodeB.Position);
                    
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestA = nodeA;
                        bestB = nodeB;
                    }
                }
            }

            if (bestA == null || bestB == null) break;

            CreateEdge(state, bestA, bestB);
            connected.Add(bestB.Id);
            disconnected.Remove(bestB.Id);
        }

        // 3. SPAWN AI FLEETS (Couriers)
        foreach (var node in nodesList)
        {
            var fleet = new Fleet
            {
                Id = $"ai_fleet_{node.Id}",
                OwnerId = "ai",
                CurrentNodeId = node.Id,
                Speed = 0.8f,
                State = FleetState.Idle,
                Supplies = 100
            };
            state.Fleets.Add(fleet.Id, fleet);
        }
    }

    private static void CreateEdge(SimState state, Node a, Node b)
    {
        string id = $"edge_{GetSortedId(a.Id, b.Id)}";
        if (!state.Edges.ContainsKey(id))
        {
            state.Edges.Add(id, new Edge
            {
                Id = id,
                FromNodeId = a.Id,
                ToNodeId = b.Id,
                Distance = Vector3.Distance(a.Position, b.Position),
                TotalCapacity = 5
            });
        }
    }

    private static string GetSortedId(string a, string b)
    {
        return string.Compare(a, b) < 0 ? $"{a}_{b}" : $"{b}_{a}";
    }
}