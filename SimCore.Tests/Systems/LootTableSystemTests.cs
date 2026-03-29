using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
public class LootTableSystemTests
{
    private SimState CreateState(int seed = 42)
    {
        var kernel = new SimKernel(seed);
        SimCore.Gen.GalaxyGenerator.Generate(kernel.State, 30, 100f);
        return kernel.State;
    }

    [Test]
    public void RollLoot_CreatesDropInState()
    {
        var state = CreateState();
        Assert.That(state.LootDrops, Is.Empty);

        LootTableSystem.RollLoot(state, "npc_fleet_1", "node_0");

        Assert.That(state.LootDrops, Has.Count.EqualTo(1));
        var drop = state.LootDrops.Values.First();
        Assert.That(drop.NodeId, Is.EqualTo("node_0"));
        Assert.That(drop.TickCreated, Is.EqualTo(0));
        Assert.That(drop.Credits, Is.GreaterThan(0));
    }

    [Test]
    public void RollLoot_DeterministicById()
    {
        var s1 = CreateState();
        var s2 = CreateState();

        LootTableSystem.RollLoot(s1, "fleet_abc", "node_0");
        LootTableSystem.RollLoot(s2, "fleet_abc", "node_0");

        var d1 = s1.LootDrops.Values.First();
        var d2 = s2.LootDrops.Values.First();

        Assert.That(d2.Rarity, Is.EqualTo(d1.Rarity));
        Assert.That(d2.Credits, Is.EqualTo(d1.Credits));
    }

    [Test]
    public void RollLoot_DifferentFleetIds_ProduceDifferentDropIds()
    {
        var state = CreateState();

        LootTableSystem.RollLoot(state, "fleet_a", "node_0");
        LootTableSystem.RollLoot(state, "fleet_b", "node_0");

        Assert.That(state.LootDrops, Has.Count.EqualTo(2));
        var ids = state.LootDrops.Keys.ToList();
        Assert.That(ids[0], Is.Not.EqualTo(ids[1]));
    }

    [Test]
    public void RollLoot_NullState_NoOp()
    {
        Assert.DoesNotThrow(() => LootTableSystem.RollLoot(null!, "fleet", "node"));
    }

    [Test]
    public void RollLoot_EmptyFleetId_NoOp()
    {
        var state = CreateState();
        LootTableSystem.RollLoot(state, "", "node_0");
        Assert.That(state.LootDrops, Is.Empty);
    }

    [Test]
    public void ProcessDespawn_RemovesExpiredDrops()
    {
        var state = CreateState();

        LootTableSystem.RollLoot(state, "fleet_old", "node_0");
        Assert.That(state.LootDrops, Has.Count.EqualTo(1));

        for (int i = 0; i < LootTweaksV0.DespawnTicks; i++)
            state.AdvanceTick();

        LootTableSystem.ProcessDespawn(state);
        Assert.That(state.LootDrops, Is.Empty);
    }

    [Test]
    public void ProcessDespawn_KeepsFreshDrops()
    {
        var state = CreateState();

        LootTableSystem.RollLoot(state, "fleet_new", "node_0");
        for (int i = 0; i < 10; i++)
            state.AdvanceTick();

        LootTableSystem.ProcessDespawn(state);
        Assert.That(state.LootDrops, Has.Count.EqualTo(1));
    }

    [Test]
    public void RollLoot_AllRarities_Reachable()
    {
        var seen = new HashSet<LootRarity>();

        for (int i = 0; i < 500; i++)
        {
            var s = CreateState();
            LootTableSystem.RollLoot(s, $"exhaust_{i}", "node_0");
            seen.Add(s.LootDrops.Values.First().Rarity);
            if (seen.Count == 4) break;
        }

        Assert.That(seen, Does.Contain(LootRarity.Common));
        Assert.That(seen, Does.Contain(LootRarity.Uncommon));
        Assert.That(seen, Does.Contain(LootRarity.Rare));
        Assert.That(seen, Does.Contain(LootRarity.Epic));
    }

    [Test]
    public void UncommonDrop_HasGoods()
    {
        for (int i = 0; i < 500; i++)
        {
            var s = CreateState();
            LootTableSystem.RollLoot(s, $"unc_{i}", "node_0");
            var drop = s.LootDrops.Values.First();
            if (drop.Rarity == LootRarity.Uncommon)
            {
                Assert.That(drop.Goods, Is.Not.Empty);
                // Uncommon drops have one uncommon good at UncommonGoodsQty.
                // Guaranteed scrap may also add fuel/ore at GuaranteedScrapQty.
                Assert.That(drop.Goods.Values.Any(v => v == LootTweaksV0.UncommonGoodsQty), Is.True);
                return;
            }
        }
        Assert.Fail("Could not find an uncommon drop in 500 iterations");
    }
}
