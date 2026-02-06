using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class MarketPublishCadenceTests
{
	[Test]
	public void PublishedPrices_Initialize_AndRemainStable_WithinWindow()
	{
		var state = new SimState(123);
		var mkt = new Market { Id = "mkt_a" };
		mkt.Inventory["ore"] = 10;
		state.Markets["mkt_a"] = mkt;

		// Tick 0 publish
		MarketSystem.Process(state);

		int pub0 = mkt.GetPublishedMidPrice("ore");
		Assert.That(pub0, Is.GreaterThan(0));
		Assert.That(mkt.LastPublishedBucket, Is.EqualTo(0));

		// Change inventory inside same window
		mkt.Inventory["ore"] = 200;
		MarketSystem.Process(state);

		int pub1 = mkt.GetPublishedMidPrice("ore");

		// Published should not change inside same bucket
		Assert.That(pub1, Is.EqualTo(pub0));
	}

	[Test]
	public void PublishedPrices_Update_OnNextWindowBoundary()
	{
		var state = new SimState(777);
		var mkt = new Market { Id = "mkt_a" };
		mkt.Inventory["ore"] = 10;
		state.Markets["mkt_a"] = mkt;

		// Publish at tick 0
		MarketSystem.Process(state);
		int pub0 = mkt.GetPublishedMidPrice("ore");
		Assert.That(mkt.LastPublishedBucket, Is.EqualTo(0));

		// Change inventory but stay within bucket 0
		mkt.Inventory["ore"] = 200;

		// Advance to last tick of bucket 0 and process; still bucket 0
		for (int i = 0; i < MarketSystem.PublishWindowTicks - 1; i++) state.AdvanceTick();
		MarketSystem.Process(state);

		int pubStill0 = mkt.GetPublishedMidPrice("ore");
		Assert.That(pubStill0, Is.EqualTo(pub0));
		Assert.That(mkt.LastPublishedBucket, Is.EqualTo(0));

		// Advance into bucket 1 and process; should republish
		state.AdvanceTick(); // tick == PublishWindowTicks
		MarketSystem.Process(state);

		int pub1 = mkt.GetPublishedMidPrice("ore");
		Assert.That(mkt.LastPublishedBucket, Is.EqualTo(1));
		Assert.That(pub1, Is.Not.EqualTo(pub0));
	}
}
