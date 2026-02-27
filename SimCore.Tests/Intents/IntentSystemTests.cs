using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Intents;
using SimCore.Schemas;
using SimCore.World;

namespace SimCore.Tests.Intents;

[TestFixture]
public sealed class IntentSystemTests
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
    public void Intents_AreProcessed_And_Cleared()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        var before = s.Markets["mkt_a"].Inventory["ore"];

        k.EnqueueIntent(new BuyIntent("mkt_a", "ore", 1));
        k.EnqueueIntent(new BuyIntent("mkt_a", "ore", 2));
        k.EnqueueIntent(new BuyIntent("mkt_a", "ore", 3));

        k.Step();

        Assert.That(s.PendingIntents.Count, Is.EqualTo(0));
        Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.Not.EqualTo(before));
    }

    [Test]
    public void DiscoveryScanIntent_TransitionsSeenToScanned_AndIsCleared()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        s.Intel = new IntelBook();
        s.Intel.Discoveries["disc_a"] = new DiscoveryStateV0 { DiscoveryId = "disc_a", Phase = DiscoveryPhase.Seen };

        k.EnqueueIntent(new DiscoveryScanIntentV0(fleetId: "f1", discoveryId: "disc_a"));
        k.Step();

        Assert.That(s.PendingIntents.Count, Is.EqualTo(0));
        Assert.That(s.Intel.Discoveries["disc_a"].Phase, Is.EqualTo(DiscoveryPhase.Scanned));
    }
}
