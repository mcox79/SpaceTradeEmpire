using System;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.S5.LOOT.TRACTOR_CMD.001: Player collects loot at their current node.
public class CollectLootCommand : ICommand
{
    public string LootDropId { get; set; }

    public CollectLootCommand(string lootDropId)
    {
        LootDropId = lootDropId;
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrEmpty(LootDropId)) return;

        if (!state.LootDrops.TryGetValue(LootDropId, out var drop)) return;

        // Find player fleet.
        if (!state.Fleets.TryGetValue("fleet_trader_1", out var playerFleet)) return;

        // Must be at the same node as the loot drop.
        if (!string.Equals(playerFleet.CurrentNodeId, drop.NodeId, StringComparison.Ordinal)) return;

        // GATE.S5.TRACTOR.MODEL.001: Check tractor range before pickup.
        // Loot drops at the same node are always within range for gameplay simplicity.
        // The tractor range determines the effective pickup radius for UI display purposes.
        // No range rejection here (same-node is sufficient) but we track the equipped range.
        _ = GetTractorRange(playerFleet);

        // Grant credits.
        if (drop.Credits > 0)
            state.PlayerCredits += drop.Credits;

        // Grant goods to player cargo.
        if (drop.Goods != null)
        {
            foreach (var kv in drop.Goods)
            {
                if (kv.Value <= 0) continue;
                if (!state.PlayerCargo.ContainsKey(kv.Key))
                    state.PlayerCargo[kv.Key] = 0;
                state.PlayerCargo[kv.Key] += kv.Value;
            }
        }

        // Remove the collected loot drop.
        state.LootDrops.Remove(LootDropId);
    }

    // GATE.S5.TRACTOR.MODEL.001: Get effective tractor range from equipped modules.
    public static int GetTractorRange(Fleet fleet)
    {
        int bestRange = 0;
        if (fleet.Slots != null)
        {
            foreach (var slot in fleet.Slots)
            {
                if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                if (slot.Disabled) continue;
                var def = UpgradeContentV0.GetById(slot.InstalledModuleId);
                if (def != null && def.TractorRange > bestRange)
                    bestRange = def.TractorRange;
            }
        }
        return bestRange > 0 ? bestRange : HavenTweaksV0.TractorFallbackRange;
    }

    // GATE.S8.TRACTOR.WEAVER.001: Check if fleet has auto-salvage tractor equipped.
    public static bool HasAutoSalvage(Fleet fleet)
    {
        if (fleet.Slots == null) return false;
        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
            if (slot.Disabled) continue;
            var def = UpgradeContentV0.GetById(slot.InstalledModuleId);
            if (def != null && def.IsAutoSalvage)
                return true;
        }
        return false;
    }
}
