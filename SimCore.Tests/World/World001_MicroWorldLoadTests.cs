using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.World;

namespace SimCore.Tests.World;

public sealed class World001_MicroWorldLoadTests
{
	private const string MicroWorldJson = @"
{
  ""worldId"": ""micro_world_001"",
  ""markets"": [
    {
      ""id"": ""mkt_a"",
      ""inventory"": { ""ore"": 10, ""food"": 3 }
    },
    {
      ""id"": ""mkt_b"",
      ""inventory"": { ""ore"": 1, ""food"": 12 }
    }
  ],
  ""nodes"": [
    { ""id"": ""stn_a"", ""kind"": ""Station"", ""name"": ""Alpha Station"", ""pos"": [0,0,0], ""marketId"": ""mkt_a"" },
    { ""id"": ""stn_b"", ""kind"": ""Station"", ""name"": ""Beta Station"",  ""pos"": [10,0,0], ""marketId"": ""mkt_b"" }
  ],
  ""edges"": [
    { ""id"": ""lane_ab"", ""fromNodeId"": ""stn_a"", ""toNodeId"": ""stn_b"", ""distance"": 10.0, ""totalCapacity"": 5 }
  ],
  ""player"": { ""credits"": 1000, ""locationNodeId"": ""stn_a"", ""cargo"": { } }
}
";

	[Test]
	public void MicroWorld_Loads_WithExpectedCounts_AndStableIds()
	{
		var def = Deserialize(MicroWorldJson);
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
	}

	[Test]
	public void MicroWorld_IsDeterministic_BySignature()
	{
		var def = Deserialize(MicroWorldJson);

		var a = new SimState(777);
		WorldLoader.Apply(a, def);
		var sigA = a.GetSignature();

		var b = new SimState(777);
		WorldLoader.Apply(b, def);
		var sigB = b.GetSignature();

		Assert.That(sigA, Is.EqualTo(sigB));
	}

	private static WorldDefinition Deserialize(string json)
	{
		var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var def = JsonSerializer.Deserialize<WorldDefinition>(json, opt);
		if (def is null) throw new System.InvalidOperationException("Failed to deserialize WorldDefinition.");
		return def;
	}
}
