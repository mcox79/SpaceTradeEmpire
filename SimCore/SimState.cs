using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SimCore.Entities;

namespace SimCore;

public class SimState
{
    [JsonInclude] public int Tick { get; private set; }
    [JsonInclude] public int InitialSeed { get; private set; }

    [JsonIgnore] public Random? Rng { get; private set; }

    // --- WORLD STATE ---
    [JsonInclude] public Dictionary<string, Market> Markets { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Node> Nodes { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Edge> Edges { get; private set; } = new();

    // --- ACTORS ---
    [JsonInclude] public Dictionary<string, Fleet> Fleets { get; private set; } = new();

    // --- PLAYER STATE ---
    [JsonInclude] public long PlayerCredits { get; set; } = 1000;
    [JsonInclude] public Dictionary<string, int> PlayerCargo { get; private set; } = new();
    [JsonInclude] public string PlayerLocationNodeId { get; set; } = "";

    public SimState(int seed)
    {
        InitialSeed = seed;
        Tick = 0;
        Rng = new Random(seed);
    }

    [JsonConstructor]
    public SimState()
    {
        // Collections are already initialized via property initializers.
        // RNG is restored in HydrateAfterLoad().
    }

    public void AdvanceTick() => Tick++;

    public void HydrateAfterLoad()
    {
        // Deterministic re-seed. This does not preserve Random's internal state, but
        // it restores a stable RNG for continued deterministic generation post-load.
        Rng = new Random(InitialSeed + Tick);
    }

    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");
        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|Markets:{Markets.Count}|Fleets:{Fleets.Count}|");

        foreach (var f in Fleets.OrderBy(k => k.Key))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}_D:{f.Value.DestinationNodeId}_P:{f.Value.TravelProgress}|");
        }

        foreach (var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}_Inv:{m.Value.Inventory}_Base:{m.Value.BasePrice}|");
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
