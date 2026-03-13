#nullable enable

using Godot;
using SimCore;
using SimCore.Commands;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // --- Haven Starbase bridge methods (GATE.S8.HAVEN.BRIDGE.001) ---

    private static readonly string[] HavenTierNames =
    {
        "Undiscovered", "Powered", "Inhabited", "Operational", "Expanded", "Awakened"
    };

    // Cached haven status snapshot (nonblocking UI readout).
    private Godot.Collections.Dictionary _cachedHavenStatusV0 = new();

    // GATE.S8.HAVEN.BRIDGE.001: Haven starbase status snapshot.
    public Godot.Collections.Dictionary GetHavenStatusV0()
    {
        TryExecuteSafeRead(state =>
        {
            var haven = state.Haven;
            if (haven == null)
            {
                var empty = new Godot.Collections.Dictionary
                {
                    ["discovered"] = false,
                    ["node_id"] = "",
                    ["tier"] = 0,
                    ["tier_name"] = "Undiscovered",
                    ["upgrade_ticks_remaining"] = 0,
                    ["upgrade_target_tier"] = 0,
                    ["max_bays"] = 0,
                    ["stored_ship_ids"] = new Godot.Collections.Array(),
                    ["installed_fragment_count"] = 0,
                    ["bidirectional_thread"] = false
                };
                lock (_snapshotLock) { _cachedHavenStatusV0 = empty; }
                return;
            }

            int tierInt = (int)haven.Tier;
            string tierName = (tierInt >= 0 && tierInt < HavenTierNames.Length)
                ? HavenTierNames[tierInt]
                : "Unknown";

            var storedIds = new Godot.Collections.Array();
            if (haven.StoredShipIds != null)
            {
                foreach (var id in haven.StoredShipIds)
                    storedIds.Add(id ?? "");
            }

            var d = new Godot.Collections.Dictionary
            {
                ["discovered"] = haven.Discovered,
                ["node_id"] = haven.NodeId ?? "",
                ["tier"] = tierInt,
                ["tier_name"] = tierName,
                ["upgrade_ticks_remaining"] = haven.UpgradeTicksRemaining,
                ["upgrade_target_tier"] = (int)haven.UpgradeTargetTier,
                ["max_bays"] = HavenUpgradeSystem.GetMaxHangarBays(haven.Tier),
                ["stored_ship_ids"] = storedIds,
                ["installed_fragment_count"] = haven.InstalledFragmentIds?.Count ?? 0,
                ["bidirectional_thread"] = haven.BidirectionalThread
            };

            lock (_snapshotLock) { _cachedHavenStatusV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedHavenStatusV0; }
    }

    // Cached haven market snapshot (nonblocking UI readout).
    private Godot.Collections.Array _cachedHavenMarketV0 = new();

    // GATE.S8.HAVEN.BRIDGE.001: Haven market goods snapshot.
    public Godot.Collections.Array GetHavenMarketV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            var haven = state.Haven;
            if (haven == null || string.IsNullOrEmpty(haven.MarketId))
            {
                lock (_snapshotLock) { _cachedHavenMarketV0 = result; }
                return;
            }

            if (!state.Markets.TryGetValue(haven.MarketId, out var market))
            {
                lock (_snapshotLock) { _cachedHavenMarketV0 = result; }
                return;
            }

            // Deterministic ordering: good ID ordinal.
            var goods = market.Inventory.Keys
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();

            foreach (var goodId in goods)
            {
                int stock = market.Inventory.TryGetValue(goodId, out var qty) ? qty : 0;
                var entry = new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["stock"] = stock,
                    ["buy_price"] = market.GetBuyPrice(goodId),
                    ["sell_price"] = market.GetSellPrice(goodId)
                };
                result.Add(entry);
            }

            lock (_snapshotLock) { _cachedHavenMarketV0 = result; }
        }, 0);

        lock (_snapshotLock) { return _cachedHavenMarketV0; }
    }

    // GATE.S8.HAVEN.BRIDGE.001: Initiate Haven tier upgrade.
    public bool UpgradeHavenV0()
    {
        if (IsLoading) return false;

        int tickBefore;
        _stateLock.EnterReadLock();
        try
        {
            tickBefore = _kernel.State.Tick;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.UpgradeHavenCommand());
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        var timeoutMs = Math.Max(250, (TickDelayMs * 3) + 50);
        WaitForTickAdvance(tickBefore, timeoutMs);
        return true;
    }

    // GATE.S8.HAVEN.BRIDGE.001: Swap active ship with stored ship at Haven.
    public bool SwapShipV0(string activeFleetId, string storedFleetId)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(activeFleetId)) return false;
        if (string.IsNullOrWhiteSpace(storedFleetId)) return false;

        int tickBefore;
        _stateLock.EnterReadLock();
        try
        {
            tickBefore = _kernel.State.Tick;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.SwapShipCommand(activeFleetId, storedFleetId));
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        var timeoutMs = Math.Max(250, (TickDelayMs * 3) + 50);
        WaitForTickAdvance(tickBefore, timeoutMs);
        return true;
    }

    // --- GATE.S8.ADAPTATION.BRIDGE.001: Adaptation fragment queries ---

    private Godot.Collections.Array _cachedAdaptationFragmentsV0 = new();

    // Returns all 16 adaptation fragments with collection status.
    public Godot.Collections.Array GetAdaptationFragmentsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var def in AdaptationFragmentContentV0.AllFragments)
            {
                bool collected = state.AdaptationFragments.TryGetValue(def.FragmentId, out var frag) && frag.IsCollected;
                bool deposited = state.Haven?.TrophyWall?.ContainsKey(def.FragmentId) ?? false;
                string nodeId = frag?.NodeId ?? "";

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["fragment_id"] = def.FragmentId,
                    ["name"] = def.Name,
                    ["description"] = def.Description,
                    ["kind"] = def.Kind.ToString(),
                    ["resonance_pair_id"] = def.ResonancePairId,
                    ["collected"] = collected,
                    ["deposited"] = deposited,
                    ["node_id"] = nodeId,
                });
            }
            lock (_snapshotLock) { _cachedAdaptationFragmentsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedAdaptationFragmentsV0; }
    }

    private Godot.Collections.Array _cachedResonancePairsV0 = new();

    // Returns 8 resonance pairs with completion status and bonus descriptions.
    public Godot.Collections.Array GetResonancePairsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            var completed = AdaptationFragmentSystem.GetCompletedResonancePairs(state);

            foreach (var pair in AdaptationFragmentContentV0.AllResonancePairs)
            {
                var defA = AdaptationFragmentContentV0.GetById(pair.FragmentA);
                var defB = AdaptationFragmentContentV0.GetById(pair.FragmentB);
                bool hasA = state.AdaptationFragments.TryGetValue(pair.FragmentA, out var fragA) && fragA.IsCollected;
                bool hasB = state.AdaptationFragments.TryGetValue(pair.FragmentB, out var fragB) && fragB.IsCollected;

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["pair_id"] = pair.PairId,
                    ["fragment_a"] = pair.FragmentA,
                    ["fragment_b"] = pair.FragmentB,
                    ["fragment_a_name"] = defA?.Name ?? pair.FragmentA,
                    ["fragment_b_name"] = defB?.Name ?? pair.FragmentB,
                    ["has_a"] = hasA,
                    ["has_b"] = hasB,
                    ["complete"] = completed.Contains(pair.PairId),
                    ["bonus_description"] = pair.BonusDescription,
                });
            }
            lock (_snapshotLock) { _cachedResonancePairsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedResonancePairsV0; }
    }

    // Collect a fragment at the player's current node.
    public Godot.Collections.Dictionary CollectFragmentV0(string fragmentId)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "" };
        if (IsLoading) return result;
        if (string.IsNullOrWhiteSpace(fragmentId)) { result["reason"] = "empty_id"; return result; }

        _stateLock.EnterWriteLock();
        try
        {
            var r = AdaptationFragmentSystem.CollectFragment(_kernel.State, fragmentId);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
            result["completed_pair_id"] = r.CompletedPairId ?? "";
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }

    // Deposit a collected fragment into Haven's Trophy Wall.
    public Godot.Collections.Dictionary DepositFragmentV0(string fragmentId)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "" };
        if (IsLoading) return result;
        if (string.IsNullOrWhiteSpace(fragmentId)) { result["reason"] = "empty_id"; return result; }

        _stateLock.EnterWriteLock();
        try
        {
            var r = AdaptationFragmentSystem.DepositFragment(_kernel.State, fragmentId);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }

    // --- GATE.S8.HAVEN.TROPHY_BRIDGE.001: Trophy Wall queries ---

    private Godot.Collections.Array _cachedTrophyWallV0 = new();

    // Returns trophy wall state: all fragments with deposited status.
    public Godot.Collections.Array GetTrophyWallV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            var trophyWall = state.Haven?.TrophyWall;

            foreach (var def in AdaptationFragmentContentV0.AllFragments)
            {
                bool deposited = trophyWall?.ContainsKey(def.FragmentId) ?? false;
                int depositedTick = (deposited && trophyWall!.TryGetValue(def.FragmentId, out var tick)) ? tick : -1;
                bool collected = state.AdaptationFragments.TryGetValue(def.FragmentId, out var frag) && frag.IsCollected;

                // Check if this fragment's resonance pair is complete (both deposited).
                var pairDef = AdaptationFragmentContentV0.GetPairById(def.ResonancePairId);
                bool pairComplete = false;
                if (pairDef != null && trophyWall != null)
                    pairComplete = trophyWall.ContainsKey(pairDef.FragmentA) && trophyWall.ContainsKey(pairDef.FragmentB);

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["fragment_id"] = def.FragmentId,
                    ["name"] = def.Name,
                    ["kind"] = def.Kind.ToString(),
                    ["collected"] = collected,
                    ["deposited"] = deposited,
                    ["deposited_tick"] = depositedTick,
                    ["resonance_pair_complete"] = pairComplete,
                });
            }
            lock (_snapshotLock) { _cachedTrophyWallV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedTrophyWallV0; }
    }

    // --- GATE.S8.HAVEN.RESIDENTS_BRIDGE.001: Haven Residents queries ---

    private Godot.Collections.Array _cachedHavenResidentsV0 = new();

    // Returns Haven residents list.
    public Godot.Collections.Array GetHavenResidentsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            var haven = state.Haven;
            if (haven == null)
            {
                lock (_snapshotLock) { _cachedHavenResidentsV0 = arr; }
                return;
            }

            foreach (var res in haven.Residents)
            {
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["resident_id"] = res.ResidentId,
                    ["name"] = res.Name,
                    ["role"] = res.Role,
                    ["appeared_at_tier"] = res.AppearedAtTier,
                    ["dialogue_hint"] = res.Role == "keeper"
                        ? "The Keeper watches silently, waiting."
                        : "Available for conversation.",
                    ["available"] = true,
                });
            }

            lock (_snapshotLock) { _cachedHavenResidentsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedHavenResidentsV0; }
    }

    // --- GATE.S8.ANCIENT_HULLS.BRIDGE.001: Ancient hull queries ---

    private Godot.Collections.Array _cachedAncientHullsV0 = new();

    // Returns ancient hull data with restoration status.
    public Godot.Collections.Array GetAncientHullsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            string[] ancientIds = { "ancient_bastion", "ancient_seeker", "ancient_threshold" };

            foreach (var classId in ancientIds)
            {
                var classDef = ShipClassContentV0.GetById(classId);
                if (classDef == null) continue;

                // Check if this hull has been restored (exists as a fleet in hangar).
                bool restored = false;
                string restoredFleetId = "";
                foreach (var kv in state.Fleets)
                {
                    if (string.Equals(kv.Value.ShipClassId, classId, StringComparison.Ordinal))
                    {
                        restored = true;
                        restoredFleetId = kv.Key;
                        break;
                    }
                }

                bool canRestore = !restored
                    && state.Haven != null
                    && (int)state.Haven.Tier >= AncientHullTweaksV0.RestoreMinHavenTier
                    && state.PlayerCredits >= AdaptationTweaksV0.HullRestoreCreditCost
                    && (state.PlayerCargo.TryGetValue("exotic_matter", out var em) ? em : 0) >= AdaptationTweaksV0.HullRestoreExoticMatterCost;

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["ship_class_id"] = classId,
                    ["display_name"] = classDef.DisplayName,
                    ["slot_count"] = classDef.SlotCount,
                    ["cargo_capacity"] = classDef.CargoCapacity,
                    ["scan_range"] = classDef.ScanRange,
                    ["core_hull"] = classDef.CoreHull,
                    ["base_shield"] = classDef.BaseShield,
                    ["base_fuel_capacity"] = classDef.BaseFuelCapacity,
                    ["restored"] = restored,
                    ["restored_fleet_id"] = restoredFleetId,
                    ["can_restore"] = canRestore,
                    ["restore_credit_cost"] = AdaptationTweaksV0.HullRestoreCreditCost,
                    ["restore_exotic_matter_cost"] = AdaptationTweaksV0.HullRestoreExoticMatterCost,
                    ["restore_duration_ticks"] = AdaptationTweaksV0.HullRestoreDurationTicks,
                    ["min_haven_tier"] = AncientHullTweaksV0.RestoreMinHavenTier,
                });
            }
            lock (_snapshotLock) { _cachedAncientHullsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedAncientHullsV0; }
    }

    // Initiate ancient hull restoration at Haven.
    public Godot.Collections.Dictionary RestoreAncientHullV0(string shipClassId)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "" };
        if (IsLoading) return result;
        if (string.IsNullOrWhiteSpace(shipClassId)) { result["reason"] = "empty_id"; return result; }

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            var haven = state.Haven;
            if (haven == null || !haven.Discovered) { result["reason"] = "haven_not_available"; return result; }
            if ((int)haven.Tier < AncientHullTweaksV0.RestoreMinHavenTier) { result["reason"] = "haven_tier_too_low"; return result; }

            var classDef = ShipClassContentV0.GetById(shipClassId);
            if (classDef == null) { result["reason"] = "unknown_hull"; return result; }

            // Check if already restored.
            foreach (var kv in state.Fleets)
            {
                if (string.Equals(kv.Value.ShipClassId, shipClassId, StringComparison.Ordinal))
                { result["reason"] = "already_restored"; return result; }
            }

            // Check resources.
            if (state.PlayerCredits < AdaptationTweaksV0.HullRestoreCreditCost)
            { result["reason"] = "insufficient_credits"; return result; }
            int emQty = state.PlayerCargo.TryGetValue("exotic_matter", out var emv) ? emv : 0;
            if (emQty < AdaptationTweaksV0.HullRestoreExoticMatterCost)
            { result["reason"] = "insufficient_exotic_matter"; return result; }

            // Check hangar space.
            int maxBays = HavenUpgradeSystem.GetMaxHangarBays(haven.Tier);
            int maxStored = maxBays - 1;
            if (maxStored <= 0 || haven.StoredShipIds.Count >= maxStored)
            { result["reason"] = "hangar_full"; return result; }

            // Deduct resources.
            state.PlayerCredits -= AdaptationTweaksV0.HullRestoreCreditCost;
            state.PlayerCargo["exotic_matter"] = emQty - AdaptationTweaksV0.HullRestoreExoticMatterCost;
            if (state.PlayerCargo["exotic_matter"] <= 0)
                state.PlayerCargo.Remove("exotic_matter");

            // Create the fleet and store it in hangar.
            string fleetId = $"fleet_{shipClassId}_{state.Tick}";
            var fleet = new SimCore.Entities.Fleet
            {
                Id = fleetId,
                ShipClassId = shipClassId,
                IsStored = true,
                State = SimCore.Entities.FleetState.Idle,
                CurrentNodeId = haven.NodeId,
                HullHp = classDef.CoreHull,
                HullHpMax = classDef.CoreHull,
                ShieldHp = classDef.BaseShield,
                ShieldHpMax = classDef.BaseShield,
                FuelCapacity = classDef.BaseFuelCapacity,
                FuelCurrent = classDef.BaseFuelCapacity,
            };

            // Initialize zone armor from class definition.
            for (int i = 0; i < 4 && i < classDef.BaseZoneArmor.Length; i++)
            {
                fleet.ZoneArmorHp[i] = classDef.BaseZoneArmor[i];
                fleet.ZoneArmorHpMax[i] = classDef.BaseZoneArmor[i];
            }

            // Initialize slots — alternate between Weapon/Utility/Engine/Cargo.
            SimCore.Entities.SlotKind[] kindCycle = {
                SimCore.Entities.SlotKind.Weapon,
                SimCore.Entities.SlotKind.Utility,
                SimCore.Entities.SlotKind.Engine,
                SimCore.Entities.SlotKind.Cargo,
            };
            for (int i = 0; i < classDef.SlotCount; i++)
            {
                fleet.Slots.Add(new SimCore.Entities.ModuleSlot
                {
                    SlotKind = kindCycle[i % kindCycle.Length],
                    Condition = 100,
                });
            }

            state.Fleets[fleetId] = fleet;
            haven.StoredShipIds.Add(fleetId);

            result["success"] = true;
            result["fleet_id"] = fleetId;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }
}
