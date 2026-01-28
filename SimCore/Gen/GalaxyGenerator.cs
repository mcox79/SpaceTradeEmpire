using System.Numerics;
using SimCore.Entities;
using System.Linq;

namespace SimCore.Gen;

public static class GalaxyGenerator
{
    public static void Generate(SimState state, int starCount, float radius)
    {
        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear();
        state.Fleets.Clear();

        var nodesList = new List<Node>();

        // 1. SCATTER STARS
        for (int i = 0; i < starCount; i++)
        {
            float x = (float)(state.Rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(state.Rng.NextDouble() * 2 - 1) * radius;
            
            var node = new Node
            {
                Id = $"star_{i}",
                Name = $"System {i}",
                Position = new Vector3(x, 0, z),
                Kind = NodeKind.Star,
                MarketId = $"mkt_{i}"
            };
            
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);

            state.Markets.Add(node.MarketId, new Market 
            { 
                Id = node.MarketId, 
                BasePrice = 100, 
                Inventory = 50 + state.Rng.Next(50) 
            });
        }
        
        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;

        // 2. CONNECT NEIGHBORS
        foreach (var node in nodesList)
        {
            var neighbors = nodesList
                .Where(n => n.Id != node.Id)
                .OrderBy(n => Vector3.Distance(node.Position, n.Position))
                .Take(2);

            foreach (var target in neighbors)
            {
                CreateEdge(state, node, target);
            }
        }

        // 3. BRIDGE ISLANDS (Flood Fill Fix)
        while (true)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            
            queue.Enqueue(nodesList[0].Id);
            visited.Add(nodesList[0].Id);
            
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                foreach(var edge in state.Edges.Values)
                {
                    string neighbor = null;
                    if (edge.FromNodeId == currentId) neighbor = edge.ToNodeId;
                    else if (edge.ToNodeId == currentId) neighbor = edge.FromNodeId;
                    
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (visited.Count == nodesList.Count) break;

            Node bestA = null;
            Node bestB = null;
            float bestDist = float.MaxValue;

            foreach (var nodeA in nodesList.Where(n => visited.Contains(n.Id)))
            {
                foreach (var nodeB in nodesList.Where(n => !visited.Contains(n.Id)))
                {
                    float d = Vector3.Distance(nodeA.Position, nodeB.Position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestA = nodeA;
                        bestB = nodeB;
                    }
                }
            }

            if (bestA != null && bestB != null) CreateEdge(state, bestA, bestB);
            else break;
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