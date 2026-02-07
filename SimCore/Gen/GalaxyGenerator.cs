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

            var mkt = new Market { Id = node.MarketId };

            // Always ensure keys exist for deterministic price publishing and inventory semantics.
            mkt.Inventory["fuel"] = 100;
            mkt.Inventory["ore"] = 0;
            mkt.Inventory["metal"] = 0;

            if (i % 2 == 0)
            {
                // MINE: produces ore, consumes fuel as upkeep (2-good upkeep for Option A tests can target any site)
                mkt.Inventory["ore"] = 500;

                var mine = new IndustrySite
                {
                    Id = $"mine_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int>
                    {
                        { "fuel", 1 },
                        { "ore", 0 } // keep ore key present in Inputs map? no, Inputs should be meaningful only
                    },
                    Outputs = new Dictionary<string, int> { { "ore", 5 } },
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };

                // Remove the dummy input so upkeep inputs are real only
                mine.Inputs.Remove("ore");

                state.IndustrySites.Add(mine.Id, mine);
                node.Name += " (Mining)";
            }
            else
            {
                // REFINERY: consumes ore + fuel each tick, produces metal
                var factory = new IndustrySite
                {
                    Id = $"fac_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int>
                    {
                        { "ore", 10 },
                        { "fuel", 1 }
                    },
                    Outputs = new Dictionary<string, int> { { "metal", 5 } },
                    BufferDays = 2,
                    DegradePerDayBps = 500 // 5% health per day at full deficit
                };
                state.IndustrySites.Add(factory.Id, factory);
                node.Name += " (Refinery)";
            }

            state.Markets.Add(node.MarketId, mkt);
        }

        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;

        for (int i = 0; i < nodesList.Count - 1; i++)
        {
            CreateEdge(state, nodesList[i], nodesList[i + 1]);
        }
        CreateEdge(state, nodesList[nodesList.Count - 1], nodesList[0]);

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
