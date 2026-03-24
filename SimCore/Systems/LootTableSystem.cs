using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S5.LOOT.DROP_SYSTEM.001: Loot drop system — generates loot on NPC fleet kill.
public static class LootTableSystem
{
    /// <summary>
    /// Rolls loot for a destroyed NPC fleet and adds a LootDrop to state.
    /// Called from NpcFleetCombatSystem on destruction.
    /// Uses deterministic FNV1a hash of fleetId + tick for roll.
    /// </summary>
    public static void RollLoot(SimState state, string fleetId, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(fleetId)) return;

        ulong hash = Fnv1a64(fleetId + "_loot_" + state.Tick);
        int roll = (int)(hash % (ulong)LootTweaksV0.TotalWeight);

        LootRarity rarity;
        if (roll < LootTweaksV0.CommonWeight)
            rarity = LootRarity.Common;
        else if (roll < LootTweaksV0.CommonWeight + LootTweaksV0.UncommonWeight)
            rarity = LootRarity.Uncommon;
        else if (roll < LootTweaksV0.CommonWeight + LootTweaksV0.UncommonWeight + LootTweaksV0.RareWeight)
            rarity = LootRarity.Rare;
        else
            rarity = LootRarity.Epic;

        var drop = new LootDrop
        {
            Id = $"loot_{fleetId}_{state.Tick}",
            NodeId = nodeId,
            Rarity = rarity,
            TickCreated = state.Tick,
        };

        ulong rewardHash = Fnv1a64(fleetId + "_reward_" + state.Tick);

        switch (rarity)
        {
            case LootRarity.Common:
                drop.Credits = LootTweaksV0.CommonCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.CommonCreditsRange);
                break;

            case LootRarity.Uncommon:
                drop.Credits = LootTweaksV0.UncommonCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.UncommonCreditsRange);
                int goodIdx = (int)((rewardHash >> 8) % (ulong)LootTweaksV0.UncommonGoodsPool.Length);
                drop.Goods[LootTweaksV0.UncommonGoodsPool[goodIdx]] = LootTweaksV0.UncommonGoodsQty;
                break;

            case LootRarity.Rare:
                drop.Credits = LootTweaksV0.RareCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.RareCreditsRange);
                break;

            case LootRarity.Epic:
                drop.Credits = LootTweaksV0.EpicCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.EpicCreditsRange);
                break;
        }

        state.LootDrops[drop.Id] = drop;
    }

    /// <summary>
    /// GATE.T55.COMBAT.PIRATE_FACTION.001: Enhanced pirate loot — salvaged_tech + rare_metals + credits.
    /// Deterministic from fleetId + tick. Called instead of RollLoot when destroyed fleet is pirate.
    /// </summary>
    public static void RollPirateLoot(SimState state, string fleetId, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(fleetId)) return;

        ulong hash = Fnv1a64(fleetId + "_pirate_loot_" + state.Tick);

        int techRange = FactionTweaksV0.PirateLootSalvagedTechMax - FactionTweaksV0.PirateLootSalvagedTechMin + 1;
        int techQty = FactionTweaksV0.PirateLootSalvagedTechMin + (int)(hash % (ulong)techRange);

        ulong hash2 = Fnv1a64(fleetId + "_pirate_metals_" + state.Tick);
        int metalsRange = FactionTweaksV0.PirateLootRareMetalsMax - FactionTweaksV0.PirateLootRareMetalsMin + 1;
        int metalsQty = FactionTweaksV0.PirateLootRareMetalsMin + (int)(hash2 % (ulong)metalsRange);

        ulong hash3 = Fnv1a64(fleetId + "_pirate_credits_" + state.Tick);
        int creditsRange = FactionTweaksV0.PirateLootCreditsMax - FactionTweaksV0.PirateLootCreditsMin + 1;
        int credits = FactionTweaksV0.PirateLootCreditsMin + (int)(hash3 % (ulong)creditsRange);

        var drop = new LootDrop
        {
            Id = $"loot_{fleetId}_{state.Tick}",
            NodeId = nodeId,
            Rarity = LootRarity.Uncommon, // Pirate loot is always at least uncommon quality.
            TickCreated = state.Tick,
            Credits = credits,
        };
        drop.Goods[Content.WellKnownGoodIds.SalvagedTech] = techQty;
        drop.Goods[Content.WellKnownGoodIds.RareMetals] = metalsQty;

        state.LootDrops[drop.Id] = drop;
    }

    /// <summary>
    /// Process loot despawn: remove drops older than DespawnTicks.
    /// Called once per tick from SimKernel.
    /// </summary>
    public static void ProcessDespawn(SimState state)
    {
        if (state is null) return;

        var expired = new List<string>();
        foreach (var kv in state.LootDrops)
        {
            if (state.Tick - kv.Value.TickCreated >= LootTweaksV0.DespawnTicks)
                expired.Add(kv.Key);
        }
        foreach (var id in expired)
            state.LootDrops.Remove(id);
    }

    private static ulong Fnv1a64(string input)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in input) { hash ^= (byte)c; hash *= 1099511628211UL; }
        return hash;
    }
}
