using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.STATION_MEMORY.001: Track per-station per-good delivery
// history. The Stationmaster NPC uses this to generate contextual dialogue.
// Station memory enables "you're reliable" and war-context lines.
public static class StationMemorySystem
{
    /// <summary>
    /// Record a delivery of goods at a station. Called by SellCommand/TradeCommand
    /// after a successful player sale.
    /// </summary>
    public static void RecordDelivery(SimState state, string nodeId, string goodId, int quantity)
    {
        if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(goodId)) return;
        if (quantity <= 0) return;

        // Enforce max records to prevent unbounded growth
        if (state.StationMemory.Count >= NarrativeTweaksV0.StationMemoryMaxRecords)
        {
            // Already at cap — only update existing records, don't create new ones
            string key = StationDeliveryRecord.Key(nodeId, goodId);
            if (!state.StationMemory.ContainsKey(key)) return;
        }

        string recordKey = StationDeliveryRecord.Key(nodeId, goodId);
        if (!state.StationMemory.TryGetValue(recordKey, out var record))
        {
            record = new StationDeliveryRecord
            {
                NodeId = nodeId,
                GoodId = goodId,
                TotalDeliveries = 0,
                TotalQuantity = 0,
                FirstDeliveryTick = state.Tick,
                LastDeliveryTick = state.Tick
            };
            state.StationMemory[recordKey] = record;
        }

        record.TotalDeliveries++;
        record.TotalQuantity += quantity;
        record.LastDeliveryTick = state.Tick;
    }

    /// <summary>
    /// Get total deliveries of a specific good to a specific node.
    /// Returns 0 if no record exists.
    /// </summary>
    public static int GetDeliveryCount(SimState state, string nodeId, string goodId)
    {
        string key = StationDeliveryRecord.Key(nodeId, goodId);
        return state.StationMemory.TryGetValue(key, out var record) ? record.TotalDeliveries : 0;
    }

    /// <summary>
    /// Get total quantity of a specific good delivered to a specific node.
    /// Returns 0 if no record exists.
    /// </summary>
    public static int GetTotalQuantity(SimState state, string nodeId, string goodId)
    {
        string key = StationDeliveryRecord.Key(nodeId, goodId);
        return state.StationMemory.TryGetValue(key, out var record) ? record.TotalQuantity : 0;
    }

    /// <summary>
    /// Check if the player has enough deliveries to a node to be considered "reliable"
    /// by the Stationmaster NPC.
    /// </summary>
    public static bool IsReliableAtStation(SimState state, string nodeId)
    {
        int totalDeliveries = 0;
        foreach (var kv in state.StationMemory)
        {
            if (kv.Value.NodeId == nodeId)
                totalDeliveries += kv.Value.TotalDeliveries;
        }
        return totalDeliveries >= NarrativeTweaksV0.StationmasterReliableThreshold;
    }
}
