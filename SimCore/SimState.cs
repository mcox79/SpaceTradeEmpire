using System.Text;
using System.Security.Cryptography;
using SimCore.Entities;

namespace SimCore;

public class SimState
{
    public int Tick { get; private set; }
    public Random Rng { get; private set; }
    
    // --- WORLD STATE ---
    public Dictionary<string, Market> Markets { get; private set; } = new();
    public Dictionary<string, Node> Nodes { get; private set; } = new();
    public Dictionary<string, Edge> Edges { get; private set; } = new();
    
    // --- ACTORS (New System) ---
    public Dictionary<string, Fleet> Fleets { get; private set; } = new();

    // --- PLAYER STATE (Restored for Compatibility) ---
    // These are required by BuyCommand and GalaxyGenerator
    public long PlayerCredits { get; set; } = 1000;
    public Dictionary<string, int> PlayerCargo { get; set; } = new();
    public string PlayerLocationNodeId { get; set; } = "";

    public SimState(int seed)
    {
        Tick = 0;
        Rng = new Random(seed);
    }

    public void AdvanceTick()
    {
        Tick++;
    }

    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");
        
        // Hash Fleets
        foreach(var f in Fleets.OrderBy(k => k.Key))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}|");
        }

        // Hash Markets
        foreach(var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}_Inv:{m.Value.Inventory}|");
        }

        // Hash Topology (Simplified for perf, but ensures existence)
        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|");
        
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}

public class Market
{
    public string Id { get; set; } = "";
    public int Inventory { get; set; }
    public int BasePrice { get; set; }
    public int CurrentPrice => Math.Max(1, BasePrice + (100 - Inventory)); 
}