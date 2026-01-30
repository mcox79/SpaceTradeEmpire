using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SimCore.Entities;
using System.Linq;
using System.Collections.Generic;
using System;

namespace SimCore;

public class SimState
{
    [JsonInclude] public int Tick { get; private set; }
    [JsonInclude] public int InitialSeed { get; private set; }
    [JsonIgnore] public Random? Rng { get; private set; }

    [JsonInclude] public Dictionary<string, Market> Markets { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Node> Nodes { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Edge> Edges { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Fleet> Fleets { get; private set; } = new();
    [JsonInclude] public Dictionary<string, IndustrySite> IndustrySites { get; private set; } = new();

    [JsonInclude] public long PlayerCredits { get; set; } = 1000;
    [JsonInclude] public Dictionary<string, int> PlayerCargo { get; private set; } = new();
    [JsonInclude] public string PlayerLocationNodeId { get; set; } = "";
    [JsonInclude] public string PlayerSelectedDestinationNodeId { get; set; } = "";

    public SimState(int seed)
    {
        InitialSeed = seed;
        Tick = 0;
        Rng = new Random(seed);
    }

    [JsonConstructor]
    public SimState() { }

    public void AdvanceTick() => Tick++;
    public void HydrateAfterLoad() => Rng = new Random(InitialSeed + Tick);

    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");
        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|Markets:{Markets.Count}|Fleets:{Fleets.Count}|Sites:{IndustrySites.Count}|");

        foreach (var f in Fleets.OrderBy(k => k.Key))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}_D:{f.Value.DestinationNodeId}|");
        }

        foreach (var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}|");
            foreach(var kv in m.Value.Inventory.OrderBy(i => i.Key))
            {
                sb.Append($"{kv.Key}:{kv.Value},");
            }
            sb.Append("|");
        }
        
        // Hash Industry State
        foreach (var s in IndustrySites.OrderBy(k => k.Key))
        {
            sb.Append($"Site:{s.Key}|Eff:{s.Value.Efficiency}|");
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}