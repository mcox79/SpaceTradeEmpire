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

    // GATE.T62.SHIP.CATALOG_DISCLOSURE.001: Mid-tier base classes requiring exploration.
    private static readonly HashSet<string> MidTierClasses = new(StringComparer.Ordinal)
    {
        "cruiser", "carrier", "dreadnought"
    };

    /// <summary>
    /// GATE.T62.SHIP.CATALOG_DISCLOSURE.001: Check if a ship class is disclosed (visible) to the player.
    /// Rules:
    ///  - Base shuttle/corvette/clipper/frigate/hauler: always visible.
    ///  - Mid-tier base (cruiser/carrier/dreadnought): requires systems visited >= MidTierSystemsRequired.
    ///  - Dreadnought additionally requires faction rep >= CapitalRepRequired at any faction.
    ///  - Faction variants: requires faction rep >= VariantRepRequired for that faction.
    ///  - Ancient/lattice drone: never shown in shipyard catalog.
    /// </summary>
    public static bool IsShipClassDisclosed(ShipClassDef classDef, SimState state)
    {
        if (classDef == null || state == null) return false;

        // Ancient hulls and lattice drones are not purchasable.
        if (classDef.ClassId.StartsWith("ancient_", StringComparison.Ordinal)) return false;
        if (string.Equals(classDef.ClassId, "lattice_drone", StringComparison.Ordinal)) return false;

        int nodesVisited = state.PlayerStats?.NodesVisited ?? 0; // STRUCTURAL: 0 default

        // Faction variants: require faction rep >= VariantRepRequired.
        if (!string.IsNullOrEmpty(classDef.FactionId))
        {
            state.FactionReputation.TryGetValue(classDef.FactionId, out int playerRep);
            if (playerRep < ShipyardTweaksV0.VariantRepRequired) return false;
            // Also apply base-class disclosure rules.
            if (!string.IsNullOrEmpty(classDef.BaseClassId))
            {
                var baseDef = ShipClassContentV0.GetById(classDef.BaseClassId);
                if (baseDef != null && !IsBaseClassDisclosed(baseDef.ClassId, nodesVisited, state))
                    return false;
            }
            return true;
        }

        return IsBaseClassDisclosed(classDef.ClassId, nodesVisited, state);
    }

    private static bool IsBaseClassDisclosed(string classId, int nodesVisited, SimState state)
    {
        if (!MidTierClasses.Contains(classId)) return true; // STRUCTURAL: early-tier always visible

        // Mid-tier: need enough exploration.
        if (nodesVisited < ShipyardTweaksV0.MidTierSystemsRequired) return false;

        // Dreadnought: additional rep gate (any faction >= CapitalRepRequired).
        if (string.Equals(classId, "dreadnought", StringComparison.Ordinal))
        {
            bool hasRep = false;
            foreach (var kvp in state.FactionReputation)
            {
                if (kvp.Value >= ShipyardTweaksV0.CapitalRepRequired) { hasRep = true; break; }
            }
            if (!hasRep) return false;
        }

        return true;
    }

    /// <summary>
    /// GATE.T62.SHIP.CATALOG_DISCLOSURE.001: Get all disclosed ship classes at a station shipyard.
    /// Returns only purchasable classes the player can currently see (not necessarily afford).
    /// </summary>
    public static List<ShipClassDef> GetDisclosedCatalog(SimState state, string stationNodeId)
    {
        var result = new List<ShipClassDef>();
        if (state == null || !IsShipyardStation(state, stationNodeId)) return result;

        // Determine station faction — faction variants from that faction get priority display.
        state.NodeFactionId.TryGetValue(stationNodeId, out var stationFaction);

        foreach (var classDef in ShipClassContentV0.AllClasses)
        {
            if (!IsShipClassDisclosed(classDef, state)) continue;
            // Only show faction variants matching station faction or player-allied factions.
            if (!string.IsNullOrEmpty(classDef.FactionId) && !string.IsNullOrEmpty(stationFaction)
                && !string.Equals(classDef.FactionId, stationFaction, StringComparison.Ordinal))
                continue;
            result.Add(classDef);
        }
        return result;
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
