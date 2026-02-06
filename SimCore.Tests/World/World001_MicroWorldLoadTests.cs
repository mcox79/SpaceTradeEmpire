using System.IO;
using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.World;

namespace SimCore.Tests.World;

public sealed class World001_MicroWorldLoadTests
{
    private static string FindRepoRoot()
    {
        // Typical WorkDirectory:
        // ...\SimCore.Tests\bin\Debug\net8.0
        var d = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);

        while (d is not null)
        {
            if (Directory.Exists(Path.Combine(d.FullName, ".git"))) return d.FullName;
            if (Directory.GetFiles(d.FullName, "*.sln").Length > 0) return d.FullName;
            d = d.Parent;
        }

        // Last resort
        return Directory.GetCurrentDirectory();
    }

    private static WorldDefinition LoadWorld001()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "SimCore.Tests", "TestData", "Worlds", "micro_world_001.json");
        var json = File.ReadAllText(path);

        var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var def = JsonSerializer.Deserialize<WorldDefinition>(json, opt);
        if (def is null) throw new System.InvalidOperationException("Failed to deserialize WorldDefinition for micro_world_001.");
        return def;
    }

    [Test]
    public void World001_Loads_WithExpectedCounts_AndStableIds()
    {
        var def = LoadWorld001();
        var state = new SimState(123);

        WorldLoader.Apply(state, def);

        Assert.That(state.Markets.Count, Is.EqualTo(2));
        Assert.That(state.Nodes.Count, Is.EqualTo(2));
        Assert.That(state.Edges.Count, Is.EqualTo(1));

        Assert.That(state.Markets.ContainsKey("mkt_a"), Is.True);
        Assert.That(state.Markets.ContainsKey("mkt_b"), Is.True);

        Assert.That(state.Nodes.ContainsKey("stn_a"), Is.True);
        Assert.That(state.Nodes.ContainsKey("stn_b"), Is.True);

        Assert.That(state.Edges.ContainsKey("lane_ab"), Is.True);

        Assert.That(state.Nodes["stn_a"].MarketId, Is.EqualTo("mkt_a"));
        Assert.That(state.Nodes["stn_b"].MarketId, Is.EqualTo("mkt_b"));
        Assert.That(state.PlayerLocationNodeId, Is.EqualTo("stn_a"));
        Assert.That(state.PlayerCredits, Is.EqualTo(1000));
    }

    [Test]
    public void World001_IsDeterministic_BySignature()
    {
        var def = LoadWorld001();

        var a = new SimState(777);
        WorldLoader.Apply(a, def);
        var sigA = a.GetSignature();

        var b = new SimState(777);
        WorldLoader.Apply(b, def);
        var sigB = b.GetSignature();

        Assert.That(sigA, Is.EqualTo(sigB));
    }
}
