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

	// GATE.S3_6.DISCOVERY_STATE.001: DiscoveryState contract tests

	[Test]
	public void DiscoveryPhase_HasRequiredValues()
	{
		Assert.That((int)DiscoveryPhase.Seen, Is.EqualTo(0));
		Assert.That((int)DiscoveryPhase.Scanned, Is.EqualTo(1));
		Assert.That((int)DiscoveryPhase.Analyzed, Is.EqualTo(2));
	}

	[Test]
	public void DiscoveryReasonCode_HasRequiredValues()
	{
		Assert.That((int)DiscoveryReasonCode.Ok, Is.EqualTo(0));
		Assert.That((int)DiscoveryReasonCode.NotSeen, Is.EqualTo(1));
		Assert.That((int)DiscoveryReasonCode.AlreadyAnalyzed, Is.EqualTo(2));
	}

	[Test]
	public void DiscoveryScan_ReasonCode_NotSeen_WhenNotInBook()
	{
		var state = new SimState(1);
		Assert.That(IntelSystem.GetScanReasonCode(state, "disc_unknown"), Is.EqualTo(DiscoveryReasonCode.NotSeen));
		Assert.That(IntelSystem.GetAnalyzeReasonCode(state, "disc_unknown"), Is.EqualTo(DiscoveryReasonCode.NotSeen));
	}

	[Test]
	public void DiscoveryScan_ReasonCode_AlreadyAnalyzed_WhenAtMaxPhase()
	{
		var state = new SimState(2);
		state.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Analyzed };
		Assert.That(IntelSystem.GetScanReasonCode(state, "disc_a"), Is.EqualTo(DiscoveryReasonCode.AlreadyAnalyzed));
		Assert.That(IntelSystem.GetAnalyzeReasonCode(state, "disc_a"), Is.EqualTo(DiscoveryReasonCode.AlreadyAnalyzed));
	}

	[Test]
	public void DiscoveryListing_StableOrdering_ByDiscoveryIdAsc()
	{
		var state = new SimState(3);
		state.Intel.Discoveries["disc_003"] = new DiscoveryStateV0 { DiscoveryId = "disc_003", Phase = DiscoveryPhase.Seen };
		state.Intel.Discoveries["disc_001"] = new DiscoveryStateV0 { DiscoveryId = "disc_001", Phase = DiscoveryPhase.Analyzed };
		state.Intel.Discoveries["disc_002"] = new DiscoveryStateV0 { DiscoveryId = "disc_002", Phase = DiscoveryPhase.Scanned };

		var list = IntelSystem.GetDiscoveriesAscending(state);

		Assert.That(list.Count, Is.EqualTo(3));
		Assert.That(list[0].DiscoveryId, Is.EqualTo("disc_001"));
		Assert.That(list[1].DiscoveryId, Is.EqualTo("disc_002"));
		Assert.That(list[2].DiscoveryId, Is.EqualTo("disc_003"));
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
