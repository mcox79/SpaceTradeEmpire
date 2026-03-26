#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Commands;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// GATE.T59.SHIP.BRIDGE_SHIPYARD.001: Shipyard bridge contract — catalog, purchase, sell, comparison.
public partial class SimBridge
{
    private Godot.Collections.Array _cachedShipyardCatalogV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns available ship classes at a given shipyard station, filtered by progressive disclosure.
    /// Base classes: always visible. Mid-tier (cruiser, carrier): 3+ systems visited.
    /// Dreadnought: rep 25+ with station faction. Variants: rep 75+ with variant faction.
    /// Each entry: {class_id, display_name, slot_count, base_power, cargo_capacity, mass, scan_range,
    ///   core_hull, base_shield, base_fuel_capacity, price, faction_id, base_class_id,
    ///   is_variant, can_afford, meets_rep_requirement}
    /// </summary>
    public Godot.Collections.Array GetShipyardCatalogV0(string stationNodeId)
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();

            if (!ShipyardSystem.IsShipyardStation(state, stationNodeId))
            {
                lock (_snapshotLock) { _cachedShipyardCatalogV0 = arr; }
                return;
            }

            // Station faction for dreadnought disclosure check.
            state.NodeFactionId.TryGetValue(stationNodeId, out var stationFactionId);
            int stationFactionRep = 0;
            if (!string.IsNullOrEmpty(stationFactionId))
                state.FactionReputation.TryGetValue(stationFactionId, out stationFactionRep);

            int visitedCount = state.PlayerVisitedNodeIds.Count;

            foreach (var classDef in ShipClassContentV0.AllClasses)
            {
                // Skip non-purchasable classes (lattice drones, ancient hulls).
                int price = ShipyardSystem.GetPurchasePrice(classDef.ClassId);
                if (price <= 0) continue;

                bool isVariant = !string.IsNullOrEmpty(classDef.FactionId);
                string classId = classDef.ClassId;

                // --- Progressive disclosure filtering ---
                // Mid-tier: cruiser, carrier require 3+ systems visited.
                if (string.Equals(classId, "cruiser", StringComparison.Ordinal) ||
                    string.Equals(classId, "carrier", StringComparison.Ordinal))
                {
                    if (visitedCount < ShipyardTweaksV0.MidTierSystemsRequired)
                        continue;
                }

                // Dreadnought: require rep 25+ with station faction.
                if (string.Equals(classId, "dreadnought", StringComparison.Ordinal))
                {
                    if (visitedCount < ShipyardTweaksV0.MidTierSystemsRequired)
                        continue;
                    if (stationFactionRep < ShipyardTweaksV0.CapitalRepRequired)
                        continue;
                }

                // Variants: require rep 75+ with the variant's own faction.
                bool meetsRepRequirement = true;
                if (isVariant)
                {
                    int variantRep = 0;
                    state.FactionReputation.TryGetValue(classDef.FactionId, out variantRep);
                    if (variantRep < ShipyardTweaksV0.VariantRepRequired)
                    {
                        meetsRepRequirement = false;
                        continue; // Don't show variants the player can't buy yet.
                    }
                }

                bool canAfford = state.PlayerCredits >= price;

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["class_id"] = classDef.ClassId,
                    ["display_name"] = classDef.DisplayName,
                    ["slot_count"] = classDef.SlotCount,
                    ["base_power"] = classDef.BasePower,
                    ["cargo_capacity"] = classDef.CargoCapacity,
                    ["mass"] = classDef.Mass,
                    ["scan_range"] = classDef.ScanRange,
                    ["core_hull"] = classDef.CoreHull,
                    ["base_shield"] = classDef.BaseShield,
                    ["base_fuel_capacity"] = classDef.BaseFuelCapacity,
                    ["price"] = price,
                    ["faction_id"] = classDef.FactionId,
                    ["base_class_id"] = classDef.BaseClassId,
                    ["is_variant"] = isVariant,
                    ["can_afford"] = canAfford,
                    ["meets_rep_requirement"] = meetsRepRequirement,
                });
            }

            lock (_snapshotLock) { _cachedShipyardCatalogV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedShipyardCatalogV0; }
    }

    /// <summary>
    /// Purchase a ship at a station shipyard.
    /// Returns {success, message, new_fleet_id}.
    /// </summary>
    public Godot.Collections.Dictionary PurchaseShipV0(string classId, string stationNodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["success"] = false,
            ["message"] = "",
            ["new_fleet_id"] = "",
        };

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;

            // Pre-checks for meaningful error messages.
            var classDef = ShipClassContentV0.GetById(classId);
            if (classDef == null)
            {
                result["message"] = "unknown_class";
                return result;
            }

            int price = ShipyardSystem.GetPurchasePrice(classId);
            if (price <= 0)
            {
                result["message"] = "not_purchasable";
                return result;
            }

            if (!ShipyardSystem.IsShipyardStation(state, stationNodeId))
            {
                result["message"] = "not_shipyard";
                return result;
            }

            if (state.PlayerCredits < price)
            {
                result["message"] = "insufficient_credits";
                return result;
            }

            // Check faction rep for variants.
            if (!string.IsNullOrEmpty(classDef.FactionId))
            {
                state.FactionReputation.TryGetValue(classDef.FactionId, out int playerRep);
                if (playerRep < ShipyardTweaksV0.VariantRepRequired)
                {
                    result["message"] = "insufficient_rep";
                    return result;
                }
            }

            // Snapshot fleet count to detect new fleet.
            int fleetCountBefore = state.Fleets.Count;

            new PurchaseShipCommand(classId, stationNodeId).Execute(state);

            if (state.Fleets.Count > fleetCountBefore)
            {
                // Find the newly added fleet (highest tick-based ID).
                var newFleet = state.Fleets.Values
                    .Where(f => string.Equals(f.OwnerId, "player", StringComparison.Ordinal) && f.IsStored)
                    .OrderByDescending(f => f.Id, StringComparer.Ordinal)
                    .FirstOrDefault();

                result["success"] = true;
                result["message"] = "purchased";
                result["new_fleet_id"] = newFleet?.Id ?? "";
            }
            else
            {
                result["message"] = "purchase_failed";
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    /// <summary>
    /// Sell a stored ship at a station shipyard.
    /// Returns {success, message, credits_gained}.
    /// </summary>
    public Godot.Collections.Dictionary SellShipV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["success"] = false,
            ["message"] = "",
            ["credits_gained"] = 0,
        };

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;

            if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            {
                result["message"] = "unknown_fleet";
                return result;
            }

            if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal))
            {
                result["message"] = "not_player_ship";
                return result;
            }

            // Check if this is the hero (active) ship.
            var heroFleet = state.Fleets.Values.FirstOrDefault(f =>
                string.Equals(f.OwnerId, "player", StringComparison.Ordinal) && !f.IsStored);
            if (heroFleet != null && string.Equals(heroFleet.Id, fleetId, StringComparison.Ordinal))
            {
                result["message"] = "cannot_sell_active_ship";
                return result;
            }

            long creditsBefore = state.PlayerCredits;

            new SellShipCommand(fleetId).Execute(state);

            if (!state.Fleets.ContainsKey(fleetId))
            {
                long creditsGained = state.PlayerCredits - creditsBefore;
                result["success"] = true;
                result["message"] = "sold";
                result["credits_gained"] = (int)creditsGained;
            }
            else
            {
                result["message"] = "sell_failed";
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    /// <summary>
    /// Side-by-side stat comparison of two ship classes.
    /// Returns {a: {stats}, b: {stats}, deltas: {stat_name: b - a}}.
    /// </summary>
    public Godot.Collections.Dictionary GetShipComparisonV0(string classIdA, string classIdB)
    {
        var result = new Godot.Collections.Dictionary();

        var defA = ShipClassContentV0.GetById(classIdA);
        var defB = ShipClassContentV0.GetById(classIdB);

        if (defA == null || defB == null)
        {
            result["error"] = "unknown_class";
            return result;
        }

        result["a"] = BuildClassStatDict(defA);
        result["b"] = BuildClassStatDict(defB);
        result["deltas"] = new Godot.Collections.Dictionary
        {
            ["slot_count"] = defB.SlotCount - defA.SlotCount,
            ["base_power"] = defB.BasePower - defA.BasePower,
            ["cargo_capacity"] = defB.CargoCapacity - defA.CargoCapacity,
            ["mass"] = defB.Mass - defA.Mass,
            ["scan_range"] = defB.ScanRange - defA.ScanRange,
            ["core_hull"] = defB.CoreHull - defA.CoreHull,
            ["base_shield"] = defB.BaseShield - defA.BaseShield,
            ["base_fuel_capacity"] = defB.BaseFuelCapacity - defA.BaseFuelCapacity,
            ["price"] = ShipyardSystem.GetPurchasePrice(classIdB) - ShipyardSystem.GetPurchasePrice(classIdA),
        };

        return result;
    }

    private static Godot.Collections.Dictionary BuildClassStatDict(ShipClassDef def)
    {
        return new Godot.Collections.Dictionary
        {
            ["class_id"] = def.ClassId,
            ["display_name"] = def.DisplayName,
            ["slot_count"] = def.SlotCount,
            ["base_power"] = def.BasePower,
            ["cargo_capacity"] = def.CargoCapacity,
            ["mass"] = def.Mass,
            ["scan_range"] = def.ScanRange,
            ["core_hull"] = def.CoreHull,
            ["base_shield"] = def.BaseShield,
            ["base_fuel_capacity"] = def.BaseFuelCapacity,
            ["price"] = ShipyardSystem.GetPurchasePrice(def.ClassId),
            ["faction_id"] = def.FactionId,
            ["base_class_id"] = def.BaseClassId,
            ["is_variant"] = !string.IsNullOrEmpty(def.FactionId),
        };
    }
}
