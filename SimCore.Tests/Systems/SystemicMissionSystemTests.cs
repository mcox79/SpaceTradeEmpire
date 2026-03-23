using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("SystemicMissionSystem")]
public sealed class SystemicMissionSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.Nodes["nodeA"] = new Node { Id = "nodeA", MarketId = "nodeA", InstabilityLevel = 0 };
        state.Markets["nodeA"] = new Market
        {
            Id = "nodeA",
            Inventory = new() { ["food"] = 200, ["fuel"] = 200, ["munitions"] = 200 }
        };
        return state;
    }

    [Test]
    public void OffCadenceTick_NoScan()
    {
        var state = CreateState();
        // Advance to 1 past scan interval
        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();
        state.AdvanceTick(); // off-cadence

        SystemicMissionSystem.Process(state);

        Assert.That(state.SystemicOffers, Is.Empty);
    }

    [Test]
    public void ScanTick_NoTriggers_NoOffers()
    {
        var state = CreateState();
        // No warfronts, normal prices, low instability → no triggers

        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();

        SystemicMissionSystem.Process(state);

        Assert.That(state.SystemicOffers, Is.Empty);
    }

    [Test]
    public void WarDemand_LowMunitions_GeneratesOffer()
    {
        var state = CreateState();
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.Skirmish,
            ContestedNodeIds = new List<string> { "nodeA" }
        };
        // Set munitions below war demand threshold
        state.Markets["nodeA"].Inventory["munitions"] = SystemicMissionTweaksV0.WarDemandInventoryThreshold - 1;

        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();

        SystemicMissionSystem.Process(state);

        Assert.That(state.SystemicOffers, Is.Not.Empty);
        Assert.That(state.SystemicOffers[0].TriggerType, Is.EqualTo(SystemicTriggerType.WarDemand));
    }

    [Test]
    public void SupplyShortage_HighInstability_GeneratesOffer()
    {
        var state = CreateState();
        state.Nodes["nodeA"].InstabilityLevel = SystemicMissionTweaksV0.SupplyShortageInstabilityMin + 10;
        // Set inventory below shortage threshold
        state.Markets["nodeA"].Inventory["food"] = SystemicMissionTweaksV0.SupplyShortageInventoryThreshold - 1;

        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();

        SystemicMissionSystem.Process(state);

        bool hasShortage = false;
        foreach (var offer in state.SystemicOffers)
        {
            if (offer.TriggerType == SystemicTriggerType.SupplyShortage)
                hasShortage = true;
        }
        Assert.That(hasShortage, Is.True);
    }

    [Test]
    public void ExpiryPurge_RemovesStaleOffers()
    {
        var state = CreateState();
        state.SystemicOffers.Add(new SystemicMissionOffer
        {
            OfferId = "SYS|test|nodeA|food|0",
            NodeId = "nodeA",
            GoodId = "food",
            TriggerType = SystemicTriggerType.SupplyShortage,
            ExpiryTick = 1 // expires immediately
        });

        // Advance past expiry to a scan tick
        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0
               || state.Tick < 2)
            state.AdvanceTick();

        SystemicMissionSystem.Process(state);

        // Stale offer should be purged
        bool hasStale = false;
        foreach (var o in state.SystemicOffers)
        {
            if (o.OfferId == "SYS|test|nodeA|food|0")
                hasStale = true;
        }
        Assert.That(hasStale, Is.False);
    }

    [Test]
    public void MaxOffers_CapsGeneration()
    {
        var state = CreateState();
        // Fill to max
        for (int i = 0; i < SystemicMissionTweaksV0.MaxSystemicOffers; i++)
        {
            state.SystemicOffers.Add(new SystemicMissionOffer
            {
                OfferId = $"SYS|fill|nodeA|food|{i}",
                NodeId = "nodeA",
                GoodId = "food",
                TriggerType = SystemicTriggerType.SupplyShortage,
                ExpiryTick = 999999
            });
        }
        int countBefore = state.SystemicOffers.Count;

        // Set up conditions that would generate offers
        state.Nodes["nodeA"].InstabilityLevel = 100;
        state.Markets["nodeA"].Inventory["food"] = 1;

        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();

        SystemicMissionSystem.Process(state);

        Assert.That(state.SystemicOffers.Count, Is.EqualTo(countBefore));
    }

    [Test]
    public void NoDuplicateOffers_SameGoodSameNode()
    {
        var state = CreateState();
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.Skirmish,
            ContestedNodeIds = new List<string> { "nodeA" }
        };
        state.Markets["nodeA"].Inventory["munitions"] = 1;

        // First scan
        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();
        SystemicMissionSystem.Process(state);
        int countAfterFirst = state.SystemicOffers.Count;

        // Advance to next scan tick
        state.AdvanceTick();
        while (state.Tick % SystemicMissionTweaksV0.ScanIntervalTicks != 0)
            state.AdvanceTick();
        SystemicMissionSystem.Process(state);

        // Should not add duplicate for same good/node
        Assert.That(state.SystemicOffers.Count, Is.EqualTo(countAfterFirst));
    }
}
