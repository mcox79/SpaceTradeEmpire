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

	var rng = state.Rng ?? throw new InvalidOperationException("SimState.Rng is null. Ensure SimState is hydrated/initialized before Generate().");

        // 1. SCATTER STARS
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
                MarketId = $"mkt_{i}"
            };
            
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);

            state.Markets.Add(node.MarketId, new Market
	    {
	        Id = node.MarketId,
	        BasePrice = 100,
	        Inventory = 50 + rng.Next(50)
	    });

        }
        
        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;

        // 2. CONNECTIVITY SKELETON (Prim's Algorithm)
        // This ensures 100% connectivity by growing a single tree.
        var connected = new HashSet<string>();
        var disconnected = new HashSet<string>(nodesList.Select(n => n.Id));

        // Start with the first node
        var startNode = nodesList[0];
        connected.Add(startNode.Id);
        disconnected.Remove(startNode.Id);

        while (disconnected.Count > 0)
        {
            Node? bestA = null;
	    Node? bestB = null;
            float bestDist = float.MaxValue;

            // Find the shortest bridge from the Connected Cloud -> Disconnected Cloud
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

	    if (bestA == null || bestB == null)
	        throw new InvalidOperationException("GalaxyGenerator: failed to find a bridge edge during connectivity build.");

	    CreateEdge(state, bestA, bestB);
	    connected.Add(bestB.Id);
	    disconnected.Remove(bestB.Id);

        }

        // 3. ADD CYCLES (Flavor)
        // The skeleton is a perfect tree (no loops). We add a few extra edges
        // between close neighbors to create "triangles" and alternate routes.
        foreach (var node in nodesList)
        {
            var neighbors = nodesList
                .Where(n => n.Id != node.Id)
                .OrderBy(n => Vector3.Distance(node.Position, n.Position))
                .Take(2);

            foreach (var target in neighbors)
            {
                // Only add if short enough (avoid cross-galaxy accidental super-lanes)
                if (Vector3.Distance(node.Position, target.Position) < (radius * 0.5f))
                {
                    CreateEdge(state, node, target);
                }
            }
        }
    }

    private static void CreateEdge(SimState state, Node a, Node b)
    {
        string id = $"edge_{GetSortedId(a.Id, b.Id)}";
        
        // Idempotency: Don't add if exists
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