using System.Collections.Generic;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("CollectLoot")]
public sealed class CollectLootCommandTests
{
    private SimState CreateState()
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { Id = "node_a", Name = "Alpha" };
        state.PlayerCredits = 100;

        var fleet = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            State = FleetState.Idle
        };
        state.Fleets["fleet_trader_1"] = fleet;
        return state;
    }

    private LootDrop CreateDrop(string nodeId, int credits, Dictionary<string, int>? goods = null)
    {
        return new LootDrop
        {
            Id = "loot_test_1",
            NodeId = nodeId,
            Rarity = LootRarity.Common,
            TickCreated = 0,
            Credits = credits,
            Goods = goods ?? new Dictionary<string, int>()
        };
    }

    [Test]
    public void CollectLoot_GrantsCredits()
    {
        var state = CreateState();
        var drop = CreateDrop("node_a", 50);
        state.LootDrops["loot_test_1"] = drop;

        new CollectLootCommand("loot_test_1").Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(150));
        Assert.That(state.LootDrops, Is.Empty);
    }

    [Test]
    public void CollectLoot_GrantsGoods()
    {
        var state = CreateState();
        var goods = new Dictionary<string, int> { { "fuel", 5 }, { "ore", 3 } };
        var drop = CreateDrop("node_a", 0, goods);
        state.LootDrops["loot_test_1"] = drop;

        new CollectLootCommand("loot_test_1").Execute(state);

        Assert.That(state.PlayerCargo["fuel"], Is.EqualTo(5));
        Assert.That(state.PlayerCargo["ore"], Is.EqualTo(3));
        Assert.That(state.LootDrops, Is.Empty);
    }

    [Test]
    public void CollectLoot_FailsIfWrongNode()
    {
        var state = CreateState();
        var drop = CreateDrop("node_b", 50);
        state.LootDrops["loot_test_1"] = drop;

        new CollectLootCommand("loot_test_1").Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(100), "Credits should not change");
        Assert.That(state.LootDrops, Has.Count.EqualTo(1), "Loot should remain");
    }

    [Test]
    public void CollectLoot_FailsIfDropNotFound()
    {
        var state = CreateState();

        new CollectLootCommand("nonexistent").Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(100));
    }

    [Test]
    public void CollectLoot_EmptyId_NoOp()
    {
        var state = CreateState();

        new CollectLootCommand("").Execute(state);
        Assert.That(state.PlayerCredits, Is.EqualTo(100));
    }

    [Test]
    public void CollectLoot_AddsToExistingCargo()
    {
        var state = CreateState();
        state.PlayerCargo["fuel"] = 10;

        var goods = new Dictionary<string, int> { { "fuel", 5 } };
        var drop = CreateDrop("node_a", 0, goods);
        state.LootDrops["loot_test_1"] = drop;

        new CollectLootCommand("loot_test_1").Execute(state);

        Assert.That(state.PlayerCargo["fuel"], Is.EqualTo(15));
    }

    [Test]
    public void CollectLoot_CreditsAndGoods_Combined()
    {
        var state = CreateState();
        var goods = new Dictionary<string, int> { { "metal", 2 } };
        var drop = CreateDrop("node_a", 25, goods);
        state.LootDrops["loot_test_1"] = drop;

        new CollectLootCommand("loot_test_1").Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(125));
        Assert.That(state.PlayerCargo["metal"], Is.EqualTo(2));
        Assert.That(state.LootDrops, Is.Empty);
    }
}
