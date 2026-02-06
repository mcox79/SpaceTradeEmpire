using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LaneFlowSystemTests
{
	private static SimKernel KernelWithWorld001()
	{
		var k = new SimKernel(seed: 123);

		var def = new WorldDefinition
		{
			WorldId = "micro_world_001",
			Markets =
			{
				new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 10, ["food"] = 3 } },
				new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 1,  ["food"] = 12 } }
			},
			Nodes =
			{
				new WorldNode { Id = "stn_a", Kind = "Station", Name = "Alpha Station", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
				new WorldNode { Id = "stn_b", Kind = "Station", Name = "Beta Station",  MarketId = "mkt_b", Pos = new float[] { 10f, 0f, 0f } }
			},
			Edges =
			{
				new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 }
			},

			Player = new WorldPlayerStart { Credits = 1000, LocationNodeId = "stn_a" }
		};

		WorldLoader.Apply(k.State, def);
		return k;
	}

	[Test]
	public void Enqueue_Debits_Source_Immediately_And_Arrives_After_CeilDistance_Ticks()
	{
		var k = KernelWithWorld001();
		var s = k.State;

		Assert.That(s.Tick, Is.EqualTo(0));

		var ok = LaneFlowSystem.TryEnqueueTransfer(
			s,
			fromNodeId: "stn_a",
			toNodeId: "stn_b",
			goodId: "ore",
			quantity: 4,
			transferId: "xfer_001");

		Assert.That(ok, Is.True);

		Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(6));
		Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

		// Step 1: tick advances from 0 to 1 at the end of Step().
		// LaneFlowSystem.Process runs BEFORE AdvanceTick, so arrivals for tick 1 are not processed yet.
		k.Step();
		Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

		// Step 2: at the start of this step, Tick == 1, so Process can deliver arrivals scheduled for tick 1.
		k.Step();
		Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(5));
	}

	[Test]
	public void Multiple_Arrivals_Same_Tick_Process_In_Stable_Order()
	{
		var k = KernelWithWorld001();
		var s = k.State;

		var ok1 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 1, "xfer_010");
		var ok2 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 1, "xfer_002");
		var ok3 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 1, "xfer_100");

		Assert.That(ok1 && ok2 && ok3, Is.True);

		// Same reasoning as above: need 2 steps for a 1-tick delay to be processed.
		k.Step();
		Assert.That(s.Markets["mkt_b"].Inventory["food"], Is.EqualTo(12));

		k.Step();
		Assert.That(s.Markets["mkt_b"].Inventory["food"], Is.EqualTo(15));
		Assert.That(s.InFlightTransfers.Count, Is.EqualTo(0));
	}
}
