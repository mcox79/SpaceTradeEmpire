using System;
using System.Collections.Generic;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T59.SHIP.SHIPYARD_SYSTEM.001: Shipyard helper methods for purchase/sell commands.
// Not a per-tick system — ship transactions are player-initiated via commands.
public static class ShipyardSystem
{
    // Price lookup: classId -> credits. Returns 0 if unknown.
    private static readonly Dictionary<string, int> PriceTable = new(StringComparer.Ordinal)
    {
        { "shuttle", ShipyardTweaksV0.PriceShuttle },
        { "corvette", ShipyardTweaksV0.PriceCorvette },
        { "clipper", ShipyardTweaksV0.PriceClipper },
        { "frigate", ShipyardTweaksV0.PriceFrigate },
        { "hauler", ShipyardTweaksV0.PriceHauler },
        { "cruiser", ShipyardTweaksV0.PriceCruiser },
        { "carrier", ShipyardTweaksV0.PriceCarrier },
        { "dreadnought", ShipyardTweaksV0.PriceDreadnought },
        // Concord variants
        { "watchman", ShipyardTweaksV0.PriceWatchman },
        { "sentinel", ShipyardTweaksV0.PriceSentinel },
        { "guardian", ShipyardTweaksV0.PriceGuardian },
        // Chitin variants
        { "gambit", ShipyardTweaksV0.PriceGambit },
        { "wager", ShipyardTweaksV0.PriceWager },
        // Weavers variants
        { "spindle", ShipyardTweaksV0.PriceSpindle },
        { "loom", ShipyardTweaksV0.PriceLoom },
        // Valorin variants
        { "fang", ShipyardTweaksV0.PriceFang },
        { "runner", ShipyardTweaksV0.PriceRunner },
        { "raider", ShipyardTweaksV0.PriceRaider },
        // Communion variants
        { "wanderer", ShipyardTweaksV0.PriceWanderer },
        { "pilgrim", ShipyardTweaksV0.PricePilgrim },
    };

    public static int GetPurchasePrice(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return 0;
        return PriceTable.TryGetValue(classId, out var price) ? price : 0;
    }

    /// <summary>
    /// Check if a node is a shipyard-capable station.
    /// Requirements: must be a Station node, must be faction-owned, faction must not be pirates.
    /// </summary>
    public static bool IsShipyardStation(SimState state, string nodeId)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return false;
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return false;
        if (node.Kind != NodeKind.Station) return false;

        // Must be faction-owned (not pirate, not unowned).
        if (!state.NodeFactionId.TryGetValue(nodeId, out var factionId)) return false;
        if (string.IsNullOrEmpty(factionId)) return false;
        if (string.Equals(factionId, FactionTweaksV0.PirateId, StringComparison.Ordinal)) return false;

        return true;
    }

    /// <summary>
    /// Create a new Fleet entity from a ShipClassDef, positioned at the given node.
    /// The fleet is stored (IsStored=true) — the player must switch to it explicitly.
    /// </summary>
    public static Fleet CreateFleetFromClass(ShipClassDef classDef, string nodeId, SimState state)
    {
        // Deterministic fleet ID: use tick + fleet count for uniqueness.
        string fleetId = $"player_ship_{classDef.ClassId}_{state.Tick}_{state.Fleets.Count}";

        var fleet = new Fleet
        {
            Id = fleetId,
            OwnerId = "player",
            ShipClassId = classDef.ClassId,
            CurrentNodeId = nodeId,
            State = FleetState.Docked,
            IsStored = true,
            Speed = 1.0f / Math.Max(classDef.Mass, 1), // STRUCTURAL: inverse mass for speed
            HullHp = classDef.CoreHull,
            HullHpMax = classDef.CoreHull,
            ShieldHp = classDef.BaseShield,
            ShieldHpMax = classDef.BaseShield,
            FuelCapacity = classDef.BaseFuelCapacity,
            FuelCurrent = classDef.BaseFuelCapacity,
            ZoneArmorHp = (int[])classDef.BaseZoneArmor.Clone(),
            ZoneArmorHpMax = (int[])classDef.BaseZoneArmor.Clone(),
            CurrentTask = "Stored",
        };

        // Create empty module slots matching the class slot count.
        for (int i = 0; i < classDef.SlotCount; i++)
        {
            fleet.Slots.Add(new ModuleSlot
            {
                SlotId = $"slot_{i}",
                SlotKind = SlotKind.Cargo,
            });
        }

        return fleet;
    }
}
