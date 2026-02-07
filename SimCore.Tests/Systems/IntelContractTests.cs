using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class IntelContractTests
{
	[Test]
	public void LocalTruth_IsExact_AndAgeIsZero()
	{
		var state = new SimState(123);

		var mktA = new Market { Id = "mkt_a" };
		mktA.Inventory["ore"] = 42;
		state.Markets["mkt_a"] = mktA;

		// Player is local at mkt_a (for Slice 1 we treat node id == market id)
		state.PlayerLocationNodeId = "mkt_a";

		MarketSystem.Process(state);
		IntelSystem.Process(state);

		var view = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_a", goodId: "ore");

		Assert.That(view.Kind, Is.EqualTo(MarketGoodViewKind.LocalTruth));
		Assert.That(view.ExactInventoryQty, Is.EqualTo(42));
		Assert.That(view.AgeTicks, Is.EqualTo(0));
		Assert.That(view.InventoryBand, Is.EqualTo(InventoryBand.Unknown));
	}

	[Test]
	public void RemoteIntel_IsUnknown_WhenNeverObserved()
	{
		var state = new SimState(42);

		var mktA = new Market { Id = "mkt_a" };
		var mktB = new Market { Id = "mkt_b" };
		mktA.Inventory["ore"] = 10;
		mktB.Inventory["ore"] = 10;
		state.Markets["mkt_a"] = mktA;
		state.Markets["mkt_b"] = mktB;

		state.PlayerLocationNodeId = "mkt_a";

		MarketSystem.Process(state);
		IntelSystem.Process(state);

		var view = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

		Assert.That(view.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
		Assert.That(view.InventoryBand, Is.EqualTo(InventoryBand.Unknown));
		Assert.That(view.AgeTicks, Is.EqualTo(-1));
	}

	[Test]
	public void RemoteIntel_IsBanded_AndAgeIncrements_WhenNotReobserved()
	{
		var state = new SimState(777);

		var mktA = new Market { Id = "mkt_a" };
		var mktB = new Market { Id = "mkt_b" };
		mktA.Inventory["ore"] = 10;
		mktB.Inventory["ore"] = 10;
		state.Markets["mkt_a"] = mktA;
		state.Markets["mkt_b"] = mktB;

		state.PlayerLocationNodeId = "mkt_b";

		MarketSystem.Process(state);
		IntelSystem.Process(state);

		// Move away so mkt_b is no longer being refreshed by local observation
		state.PlayerLocationNodeId = "mkt_a";

		// At this point, intel for mkt_b should exist (observed when local), but now it should age.
		var v0 = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

		Assert.That(v0.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
		Assert.That(v0.InventoryBand, Is.Not.EqualTo(InventoryBand.Unknown));
		Assert.That(v0.AgeTicks, Is.EqualTo(0));

		for (int i = 0; i < 5; i++)
		{
			state.AdvanceTick();
			MarketSystem.Process(state);
			IntelSystem.Process(state);
		}

		var v5 = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

		Assert.That(v5.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
		Assert.That(v5.InventoryBand, Is.EqualTo(v0.InventoryBand));
		Assert.That(v5.AgeTicks, Is.EqualTo(5));
	}

	[Test]
	public void RemoteIntel_RemainsStale_WhenTruthChanges_UntilReobserved()
	{
		var state = new SimState(9001);

		var mktA = new Market { Id = "mkt_a" };
		var mktB = new Market { Id = "mkt_b" };
		mktA.Inventory["ore"] = 10;
		mktB.Inventory["ore"] = 10;
		state.Markets["mkt_a"] = mktA;
		state.Markets["mkt_b"] = mktB;

		// Observe B by being local there
		state.PlayerLocationNodeId = "mkt_b";
		MarketSystem.Process(state);
		IntelSystem.Process(state);

		// Move away
		state.PlayerLocationNodeId = "mkt_a";

		var vObs = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");
		Assert.That(vObs.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
		Assert.That(vObs.InventoryBand, Is.Not.EqualTo(InventoryBand.Unknown));
		Assert.That(vObs.AgeTicks, Is.EqualTo(0));

		// Mutate truth at B while away
		mktB.Inventory["ore"] = 999;

		// Tick forward without re-observing B
		for (int i = 0; i < 3; i++)
		{
			state.AdvanceTick();
			MarketSystem.Process(state);
			IntelSystem.Process(state);
		}

		var vStale = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

		Assert.That(vStale.Kind, Is.EqualTo(MarketGoodViewKind.RemoteIntel));
		Assert.That(vStale.InventoryBand, Is.EqualTo(vObs.InventoryBand));
		Assert.That(vStale.AgeTicks, Is.EqualTo(3));

		// Re-observe by being local again
		state.PlayerLocationNodeId = "mkt_b";
		state.AdvanceTick();
		MarketSystem.Process(state);
		IntelSystem.Process(state);

		var vFresh = IntelSystem.GetMarketGoodView(state, targetMarketId: "mkt_b", goodId: "ore");

		Assert.That(vFresh.Kind, Is.EqualTo(MarketGoodViewKind.LocalTruth));
		Assert.That(vFresh.ExactInventoryQty, Is.EqualTo(999));
		Assert.That(vFresh.AgeTicks, Is.EqualTo(0));
	}
}
