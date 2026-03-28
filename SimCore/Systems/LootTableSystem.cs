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

        // GATE.T64.COMBAT.PITY_JACKPOT.001: Pity timer + jackpot override.
        if (state.PlayerStats != null)
        {
            // Jackpot: every Nth kill forces Rare+.
            if (LootTweaksV0.JackpotKillInterval > 0
                && state.PlayerStats.NpcFleetsDestroyed > 0
                && state.PlayerStats.NpcFleetsDestroyed % LootTweaksV0.JackpotKillInterval == 0
                && rarity < LootRarity.Rare)
            {
                rarity = LootRarity.Rare;
            }

            // Pity: after N consecutive commons, force Uncommon+.
            if (rarity == LootRarity.Common)
            {
                state.PlayerStats.ConsecutiveCommonLootCount++;
                if (state.PlayerStats.ConsecutiveCommonLootCount >= LootTweaksV0.PityThreshold)
                {
                    rarity = LootRarity.Uncommon;
                    state.PlayerStats.ConsecutiveCommonLootCount = 0;
                }
            }
            else
            {
                state.PlayerStats.ConsecutiveCommonLootCount = 0;
            }
        }

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
                // Common: goods only (salvage is THINGS, not magic money).
                int comGoodIdx = (int)(rewardHash % (ulong)LootTweaksV0.CommonGoodsPool.Length);
                int comQty = LootTweaksV0.CommonGoodsQtyMin + (int)((rewardHash >> 8) % (ulong)LootTweaksV0.CommonGoodsRange);
                drop.Goods[LootTweaksV0.CommonGoodsPool[comGoodIdx]] = comQty;
                break;

            case LootRarity.Uncommon:
                drop.Credits = LootTweaksV0.UncommonCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.UncommonCreditsRange);
                int goodIdx = (int)((rewardHash >> 8) % (ulong)LootTweaksV0.UncommonGoodsPool.Length);
                drop.Goods[LootTweaksV0.UncommonGoodsPool[goodIdx]] = LootTweaksV0.UncommonGoodsQty;
                break;

            case LootRarity.Rare:
                // Valuable tech salvage + modest credits from encrypted data cores.
                drop.Credits = LootTweaksV0.RareCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.RareCreditsRange);
                int rareIdx = (int)((rewardHash >> 8) % (ulong)LootTweaksV0.RareGoodsPool.Length);
                int rareQty = LootTweaksV0.RareGoodsQtyMin + (int)((rewardHash >> 16) % (ulong)LootTweaksV0.RareGoodsRange);
                drop.Goods[LootTweaksV0.RareGoodsPool[rareIdx]] = rareQty;
                break;

            case LootRarity.Epic:
                // Exotic materials from advanced ships + credits from intact data vaults.
                drop.Credits = LootTweaksV0.EpicCreditsMin + (int)(rewardHash % (ulong)LootTweaksV0.EpicCreditsRange);
                int epicIdx = (int)((rewardHash >> 8) % (ulong)LootTweaksV0.EpicGoodsPool.Length);
                int epicQty = LootTweaksV0.EpicGoodsQtyMin + (int)((rewardHash >> 16) % (ulong)LootTweaksV0.EpicGoodsRange);
                drop.Goods[LootTweaksV0.EpicGoodsPool[epicIdx]] = epicQty;
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
    /// GATE.T61.SALVAGE.LOOT_TABLE.001: Role-based salvage loot from destroyed NPC fleet.
    /// Considers fleet role (trader/patrol/hauler), equipment count, and cargo spillage.
    /// Called from NpcFleetCombatSystem when a non-pirate fleet is destroyed.
    /// </summary>
    public static void RollSalvageLoot(SimState state, Fleet fleet, string nodeId)
    {
        if (state is null || fleet is null) return;

        string fleetId = fleet.Id;
        ulong hash = Fnv1a64(fleetId + "_salvage_" + state.Tick);

        // Determine role from fleet properties (deterministic).
        bool isPatrol = fleet.Slots.Count > 0 && fleet.Slots.Any(s =>
            s.SlotKind == Entities.SlotKind.Weapon && !string.IsNullOrEmpty(s.InstalledModuleId));
        bool isHauler = fleet.Cargo.Values.Sum() > 0;
        // Default to trader if not patrol or hauler.

        int creditsMin, creditsMax, techMin, techMax;
        if (isPatrol)
        {
            creditsMin = SalvageTweaksV0.PatrolCreditsMin;
            creditsMax = SalvageTweaksV0.PatrolCreditsMax;
            techMin = SalvageTweaksV0.PatrolSalvageTechMin;
            techMax = SalvageTweaksV0.PatrolSalvageTechMax;
        }
        else if (isHauler)
        {
            creditsMin = SalvageTweaksV0.HaulerCreditsMin;
            creditsMax = SalvageTweaksV0.HaulerCreditsMax;
            techMin = SalvageTweaksV0.HaulerSalvageTechMin;
            techMax = SalvageTweaksV0.HaulerSalvageTechMax;
        }
        else
        {
            creditsMin = SalvageTweaksV0.TraderCreditsMin;
            creditsMax = SalvageTweaksV0.TraderCreditsMax;
            techMin = SalvageTweaksV0.TraderSalvageTechMin;
            techMax = SalvageTweaksV0.TraderSalvageTechMax;
        }

        // Equipment bonus: more modules = more credits.
        int moduleCount = fleet.Slots.Count(s => !string.IsNullOrEmpty(s.InstalledModuleId));
        int equipBonusBps = moduleCount * SalvageTweaksV0.EquipmentBonusBpsPerModule;

        int creditsRange = creditsMax - creditsMin + 1; // STRUCTURAL: +1 for inclusive range
        int credits = creditsMin + (int)(hash % (ulong)creditsRange);
        if (equipBonusBps > 0)
            credits = (int)((long)credits * (10000 + equipBonusBps) / 10000);

        // Salvaged tech drop.
        ulong hash2 = Fnv1a64(fleetId + "_salvage_tech_" + state.Tick);
        int techRange = techMax - techMin + 1; // STRUCTURAL: +1 for inclusive range
        int techQty = techRange > 0 ? techMin + (int)(hash2 % (ulong)techRange) : 0;

        // Rarity: base Common, upgrade chance per module.
        LootRarity rarity = LootRarity.Common;
        if (moduleCount > 0)
        {
            int upgradeChance = moduleCount * SalvageTweaksV0.RarityUpgradeBpsPerModule;
            int rarityRoll = (int)(hash % 10000UL);
            if (rarityRoll < upgradeChance)
                rarity = LootRarity.Uncommon;
        }

        var drop = new LootDrop
        {
            Id = $"loot_{fleetId}_{state.Tick}",
            NodeId = nodeId,
            Rarity = rarity,
            TickCreated = state.Tick,
            Credits = credits,
        };

        if (techQty > 0)
            drop.Goods[Content.WellKnownGoodIds.SalvagedTech] = techQty;

        // Hull material salvage: metal and composites stripped from destroyed hull plating.
        ulong hash3 = Fnv1a64(fleetId + "_salvage_hull_" + state.Tick);
        int metalRange = SalvageTweaksV0.HullMetalMax - SalvageTweaksV0.HullMetalMin + 1; // STRUCTURAL: +1 for inclusive range
        int metalQty = metalRange > 0 ? SalvageTweaksV0.HullMetalMin + (int)(hash3 % (ulong)metalRange) : 0;
        if (metalQty > 0)
        {
            drop.Goods.TryGetValue(Content.WellKnownGoodIds.Metal, out int existingMetal);
            drop.Goods[Content.WellKnownGoodIds.Metal] = existingMetal + metalQty;
        }

        ulong hash4 = Fnv1a64(fleetId + "_salvage_comp_" + state.Tick);
        int compRange = SalvageTweaksV0.HullCompositesMax - SalvageTweaksV0.HullCompositesMin + 1; // STRUCTURAL: +1 for inclusive range
        int compQty = compRange > 0 ? SalvageTweaksV0.HullCompositesMin + (int)(hash4 % (ulong)compRange) : 0;
        if (compQty > 0)
        {
            drop.Goods.TryGetValue(Content.WellKnownGoodIds.Composites, out int existingComp);
            drop.Goods[Content.WellKnownGoodIds.Composites] = existingComp + compQty;
        }

        // Cargo spillage: drop a percentage of the fleet's cargo.
        if (fleet.Cargo.Count > 0)
        {
            foreach (var kv in fleet.Cargo.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (kv.Value <= 0) continue;
                int spillQty = kv.Value * SalvageTweaksV0.CargoSpillPct / 100;
                if (spillQty > 0)
                {
                    drop.Goods.TryGetValue(kv.Key, out int existing);
                    drop.Goods[kv.Key] = existing + spillQty;
                }
            }
        }

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
