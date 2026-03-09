using System;
using SimCore.Entities;

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
}
