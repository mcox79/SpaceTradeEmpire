#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // ── GATE.S5.COMBAT_LOCAL.BRIDGE_COMBAT.001: Combat status queries ──

    private Godot.Collections.Dictionary _cachedCombatStatusV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns the current combat status: {in_combat (bool), opponent_id (string),
    ///          player_hull (int), player_shield (int),
    ///          opponent_hull (int), opponent_shield (int)}.
    /// Nonblocking: returns last cached snapshot if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetCombatStatusV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["in_combat"] = state.InCombat,
                ["opponent_id"] = state.CombatOpponentId ?? "",
                ["player_hull"] = 0,
                ["player_shield"] = 0,
                ["opponent_hull"] = 0,
                ["opponent_shield"] = 0,
            };

            if (state.InCombat && state.Fleets.TryGetValue("fleet_trader_1", out var player))
            {
                d["player_hull"] = player.HullHp;
                d["player_shield"] = player.ShieldHp;

                if (!string.IsNullOrEmpty(state.CombatOpponentId)
                    && state.Fleets.TryGetValue(state.CombatOpponentId, out var opp))
                {
                    d["opponent_hull"] = opp.HullHp;
                    d["opponent_shield"] = opp.ShieldHp;
                }
            }

            lock (_snapshotLock)
            {
                _cachedCombatStatusV0 = d;
            }
        }, 0);

        lock (_snapshotLock)
        {
            return _cachedCombatStatusV0.Duplicate();
        }
    }

    /// <summary>
    /// Returns the most recent combat log: {outcome (string), cause (string), event_count (int)}.
    /// Returns empty dict if no combat has occurred.
    /// </summary>
    public Godot.Collections.Dictionary GetLastCombatLogV0()
    {
        var result = new Godot.Collections.Dictionary();
        TryExecuteSafeRead(state =>
        {
            if (state.CombatLogs.Count == 0) return;
            var last = state.CombatLogs[^1];
            result["outcome"] = last.Outcome.ToString();
            result["cause"] = last.CauseOfDeath ?? "";
            result["event_count"] = last.Events.Count;
        }, 0);
        return result;
    }

    // GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001
    // Dispatches a StartCombatCommand to run a full encounter between the player fleet and an opponent.
    // Blocks until the sim thread processes the command so callers can read combat results immediately.
    public void DispatchStartCombatV0(string opponentFleetId)
    {
        int tickBefore = GetSimTickBlocking();
        EnqueueCommand(new StartCombatCommand("fleet_trader_1", opponentFleetId));
        WaitForTickAdvance(tickBefore, 200);
    }

    // Clears combat state flags so the next GetCombatStatusV0 returns in_combat=false.
    public void DispatchClearCombatV0()
    {
        int tickBefore = GetSimTickBlocking();
        EnqueueCommand(new ClearCombatCommand());
        WaitForTickAdvance(tickBefore, 200);
    }

    // ── Real-time turret combat API (per-shot damage) ──

    // Cached weapon base damage map from content registry.
    // Adding new weapons to the content registry auto-flows through here.
    private Dictionary<string, int>? _weaponBaseDmgMap;

    private Dictionary<string, int> GetWeaponBaseDmgMap()
    {
        if (_weaponBaseDmgMap != null) return _weaponBaseDmgMap;
        var reg = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var m in reg.Modules)
        {
            if (m.BaseDamage > 0)
                map[m.Id] = m.BaseDamage;
        }
        _weaponBaseDmgMap = map;
        return map;
    }

    /// <summary>
    /// Ensure all fleets have combat HP initialized via CombatTweaksV0 defaults.
    /// Idempotent — skips fleets that already have HullHpMax set.
    /// </summary>
    public void InitFleetCombatHpV0()
    {
        _stateLock.EnterWriteLock();
        try
        {
            foreach (var fleet in _kernel.State.Fleets.Values)
            {
                if (fleet.HullHpMax > 0) continue;
                bool isPlayer = string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal);
                CombatSystem.InitFleetCombatStats(fleet, isPlayer);
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Read-only HP query: {hull, hull_max, shield, shield_max, alive}.
    /// Nonblocking — returns defaults if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetFleetCombatHpV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["hull"] = 0, ["hull_max"] = 0,
            ["shield"] = 0, ["shield_max"] = 0,
            ["alive"] = false,
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;
            result["hull"] = Math.Max(0, fleet.HullHp);
            result["hull_max"] = Math.Max(0, fleet.HullHpMax);
            result["shield"] = Math.Max(0, fleet.ShieldHp);
            result["shield_max"] = Math.Max(0, fleet.ShieldHpMax);
            result["alive"] = fleet.HullHp > 0;
        }, 0);

        return result;
    }

    /// <summary>
    /// Player fires one turret shot at target. Weapon stats resolved from content registry.
    /// Returns {shield_dmg, hull_dmg, target_hull, target_shield, killed}.
    /// </summary>
    public Godot.Collections.Dictionary ApplyTurretShotV0(string targetFleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["shield_dmg"] = 0, ["hull_dmg"] = 0,
            ["target_hull"] = 0, ["target_shield"] = 0,
            ["killed"] = false,
        };

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return result;
            if (!state.Fleets.TryGetValue(targetFleetId, out var target)) return result;
            if (target.HullHp <= 0) { result["killed"] = true; return result; }

            // Resolve weapon from first equipped weapon slot (data-driven via content registry).
            var weaponMap = GetWeaponBaseDmgMap();
            string weaponId = "";
            int baseDmg = SimCore.Tweaks.CombatTweaksV0.DefaultWeaponBaseDamage;
            foreach (var slot in player.Slots)
            {
                if (slot.SlotKind != SimCore.Entities.SlotKind.Weapon) continue;
                if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                weaponId = slot.InstalledModuleId;
                if (weaponMap.TryGetValue(weaponId, out var d)) baseDmg = d;
                break;
            }

            var family = CombatSystem.ClassifyWeapon(weaponId);
            var dmg = CombatSystem.CalcDamage(baseDmg, family, target.ShieldHp, target.HullHp);

            target.ShieldHp -= dmg.ShieldDmg;
            target.HullHp -= dmg.HullDmg;

            result["shield_dmg"] = dmg.ShieldDmg;
            result["hull_dmg"] = dmg.HullDmg;
            result["target_hull"] = target.HullHp;
            result["target_shield"] = target.ShieldHp;
            result["killed"] = target.HullHp <= 0;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    /// <summary>
    /// Regenerates shield HP for a fleet at the given rate per second.
    /// Shield never exceeds ShieldHpMax. Idempotent when already full.
    /// </summary>
    public void TickShieldRegenV0(string fleetId, float deltaSec)
    {
        const float REGEN_PER_SEC = 5.0f;
        if (deltaSec <= 0f) return;

        _stateLock.EnterWriteLock();
        try
        {
            if (!_kernel.State.Fleets.TryGetValue(fleetId, out var fleet)) return;
            if (fleet.ShieldHpMax <= 0 || fleet.ShieldHp >= fleet.ShieldHpMax) return;
            if (fleet.HullHp <= 0) return; // dead fleets don't regen

            int regenAmount = (int)Math.Ceiling(REGEN_PER_SEC * deltaSec);
            fleet.ShieldHp = Math.Min(fleet.ShieldHp + regenAmount, fleet.ShieldHpMax);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // ── GATE.S5.COMBAT.BRIDGE_DOCTRINE.001: Escort doctrine queries and mutation ──

    /// <summary>
    /// Returns escort doctrine status for a fleet: {escort_active (bool), escort_target_id (string), error (string)}.
    /// Nonblocking read — returns error dict if read lock is unavailable or fleet not found.
    /// </summary>
    public Godot.Collections.Dictionary GetDoctrineStatusV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["escort_active"] = false,
            ["escort_target_id"] = "",
            ["error"] = "",
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            {
                result["error"] = $"fleet not found: {fleetId}";
                return;
            }
            result["escort_active"] = fleet.EscortDoctrineActive;
            result["escort_target_id"] = fleet.EscortTargetFleetId;
        }, 0);

        return result;
    }

    /// <summary>
    /// Sets or clears escort doctrine for a fleet. Returns {ok (bool), error (string)}.
    /// doctrineType "escort": if enabled calls fleet.SetEscortDoctrine(targetFleetId), else ClearEscortDoctrine().
    /// Unknown doctrineType → error dict with ok=false.
    /// </summary>
    // Renamed from SetDoctrineV0 to avoid overload conflict with SimBridge.Automation.SetFleetDoctrineV0.
    public Godot.Collections.Dictionary SetEscortDoctrineV0(string fleetId, string doctrineType, bool enabled, string targetFleetId = "")
    {
        var result = new Godot.Collections.Dictionary
        {
            ["ok"] = false,
            ["error"] = "",
        };

        if (!string.Equals(doctrineType, "escort", StringComparison.Ordinal))
        {
            result["error"] = $"unknown doctrine type: {doctrineType}";
            return result;
        }

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            {
                result["error"] = $"fleet not found: {fleetId}";
                return result;
            }

            if (enabled)
                fleet.SetEscortDoctrine(targetFleetId);
            else
                fleet.ClearEscortDoctrine();

            result["ok"] = true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // GATE.S5.COMBAT_RES.PROOF.001: Strategic combat resolution bridge.
    // Runs full fleet-vs-fleet attrition resolver and returns outcome.
    // Returns {outcome (string: Victory/Defeat/Flee), rounds (int),
    //          attacker_hull (int), defender_hull (int), salvage (int)}.
    public Godot.Collections.Dictionary ResolveCombatV0(string attackerFleetId, string defenderFleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["outcome"] = "Error",
            ["rounds"] = 0,
            ["attacker_hull"] = 0,
            ["defender_hull"] = 0,
            ["salvage"] = 0,
        };

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue(attackerFleetId, out var attacker)) return result;
            if (!state.Fleets.TryGetValue(defenderFleetId, out var defender)) return result;

            var weaponMap = GetWeaponBaseDmgMap();
            var resolution = CombatSystem.ResolveCombatV0(attacker, defender, weaponMap);

            result["outcome"] = resolution.Outcome.ToString();
            result["rounds"] = resolution.RoundsPlayed;
            result["attacker_hull"] = resolution.AttackerHullRemaining;
            result["defender_hull"] = resolution.DefenderHullRemaining;
            result["salvage"] = resolution.SalvageValue;

            // Apply HP damage to fleets
            attacker.HullHp = resolution.AttackerHullRemaining;
            defender.HullHp = resolution.DefenderHullRemaining;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // ── GATE.S11.GAME_FEEL.COMBAT_BRIDGE.001: Recent combat events ring buffer ──

    private const int RecentCombatCapacity = 20;
    private Godot.Collections.Array _cachedRecentCombatEventsV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns the most recent combat events (up to 20) across all combat logs.
    /// Each dict: tick (int), attacker_id (string), defender_id (string),
    ///            damage (int), outcome (string).
    /// Newest events last. Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetRecentCombatEventsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            if (state.CombatLogs.Count == 0)
            {
                lock (_snapshotLock) { _cachedRecentCombatEventsV0 = arr; }
                return;
            }

            // Collect all events from all logs, newest logs last.
            // CombatLogs is ordered oldest→newest; within each log, Events is ordered by tick.
            var allEvents = new List<(CombatSystem.CombatEventEntry entry, CombatSystem.CombatOutcome outcome)>();
            foreach (var log in state.CombatLogs)
            {
                foreach (var evt in log.Events)
                {
                    allEvents.Add((evt, log.Outcome));
                }
            }

            // Take last N events (ring buffer behavior).
            int startIdx = allEvents.Count > RecentCombatCapacity
                ? allEvents.Count - RecentCombatCapacity
                : 0;

            for (int i = startIdx; i < allEvents.Count; i++)
            {
                var (evt, outcome) = allEvents[i];
                var d = new Godot.Collections.Dictionary
                {
                    ["tick"] = evt.Tick,
                    ["attacker_id"] = evt.AttackerId ?? "",
                    ["defender_id"] = evt.DefenderId ?? "",
                    ["damage"] = evt.DamageDealt,
                    ["outcome"] = outcome.ToString(),
                };
                arr.Add(d);
            }

            lock (_snapshotLock) { _cachedRecentCombatEventsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedRecentCombatEventsV0; }
    }

    // ── GATE.S6.ANOMALY.ENCOUNTER_BRIDGE.001: Anomaly encounter snapshot queries ──

    private Godot.Collections.Dictionary _cachedAnomalyEncounterSnapshotV0 = new Godot.Collections.Dictionary();
    private Godot.Collections.Array _cachedActiveEncountersV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns a snapshot of a single anomaly encounter by ID.
    /// Keys: encounter_id, family, difficulty, status (string "Pending"/"Completed"),
    ///       loot_items (array of dicts with good_id+qty), credit_reward,
    ///       discovery_lead_node_id, node_id.
    /// Returns empty dict if encounter not found.
    /// Nonblocking: returns last cached snapshot if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetAnomalyEncounterSnapshotV0(string encounterId)
    {
        TryExecuteSafeRead(state =>
        {
            if (!state.AnomalyEncounters.TryGetValue(encounterId, out var enc))
            {
                lock (_snapshotLock) { _cachedAnomalyEncounterSnapshotV0 = new Godot.Collections.Dictionary(); }
                return;
            }

            var lootArr = new Godot.Collections.Array();
            foreach (var kv in enc.LootItems)
            {
                var lootEntry = new Godot.Collections.Dictionary
                {
                    ["good_id"] = kv.Key,
                    ["qty"] = kv.Value,
                };
                lootArr.Add(lootEntry);
            }

            var d = new Godot.Collections.Dictionary
            {
                ["encounter_id"] = enc.EncounterId,
                ["family"] = enc.Family,
                ["difficulty"] = enc.Difficulty,
                ["status"] = enc.Status.ToString(),
                ["loot_items"] = lootArr,
                ["credit_reward"] = enc.CreditReward,
                ["discovery_lead_node_id"] = enc.DiscoveryLeadNodeId,
                ["node_id"] = enc.NodeId,
            };

            lock (_snapshotLock) { _cachedAnomalyEncounterSnapshotV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedAnomalyEncounterSnapshotV0.Duplicate(); }
    }

    /// <summary>
    /// Returns an array of snapshots for all anomaly encounters, sorted by EncounterId ordinal.
    /// Each element is a dictionary with the same keys as GetAnomalyEncounterSnapshotV0.
    /// Nonblocking: returns last cached array if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Array GetActiveEncountersV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            var sorted = state.AnomalyEncounters.Values
                .OrderBy(e => e.EncounterId, StringComparer.Ordinal)
                .ToList();

            foreach (var enc in sorted)
            {
                var lootArr = new Godot.Collections.Array();
                foreach (var kv in enc.LootItems)
                {
                    var lootEntry = new Godot.Collections.Dictionary
                    {
                        ["good_id"] = kv.Key,
                        ["qty"] = kv.Value,
                    };
                    lootArr.Add(lootEntry);
                }

                var d = new Godot.Collections.Dictionary
                {
                    ["encounter_id"] = enc.EncounterId,
                    ["family"] = enc.Family,
                    ["difficulty"] = enc.Difficulty,
                    ["status"] = enc.Status.ToString(),
                    ["loot_items"] = lootArr,
                    ["credit_reward"] = enc.CreditReward,
                    ["discovery_lead_node_id"] = enc.DiscoveryLeadNodeId,
                    ["node_id"] = enc.NodeId,
                };
                arr.Add(d);
            }

            lock (_snapshotLock) { _cachedActiveEncountersV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedActiveEncountersV0; }
    }

    // GATE.S16.NPC_ALIVE.COMBAT_BRIDGE.001: Sim-driven NPC fleet damage via command queue.
    // Enqueues NpcFleetDamageCommand for processing at next tick.
    // Returns {destroyed (bool), hull_remaining (int), shield_remaining (int)}.
    // 2-arg overload for GDScript call() compatibility (default params not marshalled).
    public Godot.Collections.Dictionary DamageNpcFleetV0(string fleetId, int damage)
        => DamageNpcFleetV0(fleetId, damage, 2);

    public Godot.Collections.Dictionary DamageNpcFleetV0(string fleetId, int damage, int delayTicks)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["destroyed"] = false,
            ["hull_remaining"] = 0,
            ["shield_remaining"] = 0,
        };

        if (string.IsNullOrWhiteSpace(fleetId) || damage <= 0) return result;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new NpcFleetDamageCommand(fleetId, damage, delayTicks));
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        // Read back current HP for immediate feedback.
        TryExecuteSafeRead(state =>
        {
            if (state.Fleets.TryGetValue(fleetId, out var fleet))
            {
                result["hull_remaining"] = fleet.HullHp;
                result["shield_remaining"] = fleet.ShieldHp;
                result["destroyed"] = fleet.HullHp <= 0 && fleet.HullHpMax > 0;
            }
            else
            {
                result["destroyed"] = true;
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// AI fleet fires one shot at player. Weapon stats resolved from AI's slots or defaults.
    /// Returns {shield_dmg, hull_dmg, player_hull, player_shield, killed}.
    /// </summary>
    public Godot.Collections.Dictionary ApplyAiShotAtPlayerV0(string aiFleetId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["shield_dmg"] = 0, ["hull_dmg"] = 0,
            ["player_hull"] = 0, ["player_shield"] = 0,
            ["killed"] = false,
        };

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue(aiFleetId, out var ai)) return result;
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return result;
            if (player.HullHp <= 0) { result["killed"] = true; return result; }

            // Resolve AI weapon (data-driven — AI fleets can equip weapons too).
            var weaponMap = GetWeaponBaseDmgMap();
            string weaponId = "";
            int baseDmg = SimCore.Tweaks.CombatTweaksV0.DefaultWeaponBaseDamage;
            foreach (var slot in ai.Slots)
            {
                if (slot.SlotKind != SimCore.Entities.SlotKind.Weapon) continue;
                if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                weaponId = slot.InstalledModuleId;
                if (weaponMap.TryGetValue(weaponId, out var d)) baseDmg = d;
                break;
            }

            var family = CombatSystem.ClassifyWeapon(weaponId);
            var dmg = CombatSystem.CalcDamage(baseDmg, family, player.ShieldHp, player.HullHp);

            int hullBefore = player.HullHp;
            player.ShieldHp -= dmg.ShieldDmg;
            player.HullHp -= dmg.HullDmg;

            // Damage floor: prevent one-shot kills. If hull was above 25% before
            // this hit, clamp to at least 15% after. Gives the player a chance to react.
            if (player.HullHpMax > 0)
            {
                int floor25 = player.HullHpMax / 4;
                int floor15 = player.HullHpMax * 15 / 100;
                if (hullBefore > floor25 && player.HullHp < floor15)
                    player.HullHp = floor15;
            }

            result["shield_dmg"] = dmg.ShieldDmg;
            result["hull_dmg"] = dmg.HullDmg;
            result["player_hull"] = player.HullHp;
            result["player_shield"] = player.ShieldHp;
            result["killed"] = player.HullHp <= 0;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // ── GATE.S7.COMBAT_PHASE2.BRIDGE.001: Heat, battle stations, and radiator queries ──

    private Godot.Collections.Dictionary _cachedHeatSnapshotV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns heat system state for player fleet:
    /// {heat_current (int), heat_capacity (int), rejection_rate (int),
    ///  is_overheated (bool), is_locked_out (bool)}.
    /// Nonblocking: returns last cached snapshot if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetHeatSnapshotV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["heat_current"] = 0,
                ["heat_capacity"] = SimCore.Tweaks.CombatTweaksV0.DefaultHeatCapacity,
                ["rejection_rate"] = SimCore.Tweaks.CombatTweaksV0.DefaultRejectionRate,
                ["is_overheated"] = false,
                ["is_locked_out"] = false,
            };

            if (state.Fleets.TryGetValue("fleet_trader_1", out var player))
            {
                var profile = CombatSystem.BuildProfile(player);
                d["heat_current"] = 0; // Heat is per-combat; zero outside active combat
                d["heat_capacity"] = profile.HeatCapacity;
                d["rejection_rate"] = profile.RejectionRate;
                d["is_overheated"] = false; // Heat tracked per-combat, not persisted
                d["is_locked_out"] = false;
            }

            lock (_snapshotLock)
            {
                _cachedHeatSnapshotV0 = d;
            }
        }, 0);

        lock (_snapshotLock)
        {
            return _cachedHeatSnapshotV0.Duplicate();
        }
    }

    /// <summary>
    /// Returns battle stations state for player fleet:
    /// {state (string: StandDown/SpinningUp/BattleReady), spin_up_ticks_remaining (int)}.
    /// </summary>
    public Godot.Collections.Dictionary GetBattleStationsStateV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["state"] = "StandDown",
            ["spin_up_ticks_remaining"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            result["state"] = player.BattleStations.ToString();
            result["spin_up_ticks_remaining"] = player.BattleStationsSpinUpTicksRemaining;
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns radiator module status for player fleet:
    /// {is_intact (bool), bonus_rate (int)}.
    /// is_intact indicates whether aft zone armor is still alive (radiator bonus active).
    /// </summary>
    public Godot.Collections.Dictionary GetRadiatorStatusV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["is_intact"] = true,
            ["bonus_rate"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            var profile = CombatSystem.BuildProfile(player);
            result["bonus_rate"] = profile.RadiatorBonusRate;
            result["is_intact"] = player.ZoneArmorHp[(int)SimCore.Entities.ZoneFacing.Aft] > 0 || profile.RadiatorBonusRate == 0;
        }, 0);

        return result;
    }

    /// <summary>
    /// Toggles battle stations for the player fleet.
    /// If currently StandDown → starts SpinningUp (ticks remaining = SpinUpTicks).
    /// If currently SpinningUp or BattleReady → resets to StandDown.
    /// Returns {new_state (string), ok (bool)}.
    /// </summary>
    public Godot.Collections.Dictionary ToggleBattleStationsV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["new_state"] = "StandDown",
            ["ok"] = false,
        };

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return result;

            if (player.BattleStations == SimCore.Entities.BattleStationsState.StandDown)
            {
                player.BattleStations = SimCore.Entities.BattleStationsState.SpinningUp;
                player.BattleStationsSpinUpTicksRemaining = SimCore.Tweaks.CombatTweaksV0.BattleStationsSpinUpTicks;
            }
            else
            {
                player.BattleStations = SimCore.Entities.BattleStationsState.StandDown;
                player.BattleStationsSpinUpTicksRemaining = 0;
            }

            result["new_state"] = player.BattleStations.ToString();
            result["ok"] = true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // ── GATE.S7.COMBAT_PHASE2.SPIN_BRIDGE.001: Spin state + mount type queries ──

    /// <summary>
    /// Returns spin combat state for player fleet:
    /// {spin_rpm (int), turn_penalty_bps (int), mount_types (Array of dicts)}.
    /// Nonblocking: returns defaults if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetSpinStateV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["spin_rpm"] = 0,
            ["turn_penalty_bps"] = 0,
            ["mount_types"] = new Godot.Collections.Array(),
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            result["spin_rpm"] = player.SpinRpm;
            int penaltyBps = player.SpinRpm * SimCore.Tweaks.CombatTweaksV0.TurnPenaltyBpsPerRpm;
            if (penaltyBps > SimCore.Tweaks.CombatTweaksV0.MaxTurnPenaltyBps)
                penaltyBps = SimCore.Tweaks.CombatTweaksV0.MaxTurnPenaltyBps;
            result["turn_penalty_bps"] = penaltyBps;

            var mountArr = new Godot.Collections.Array();
            foreach (var slot in player.Slots)
            {
                if (slot.SlotKind != SimCore.Entities.SlotKind.Weapon) continue;
                mountArr.Add(new Godot.Collections.Dictionary
                {
                    ["slot_id"] = slot.SlotId ?? "",
                    ["module_id"] = slot.InstalledModuleId ?? "",
                    ["mount_type"] = slot.MountType.ToString(),
                });
            }
            result["mount_types"] = mountArr;
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns per-slot mount types for a fleet (for ship fitting UI).
    /// Array of {slot_id, slot_kind, mount_type, installed_module_id}.
    /// </summary>
    public Godot.Collections.Array GetMountTypesV0(string fleetId)
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;
            foreach (var slot in fleet.Slots)
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["slot_id"] = slot.SlotId ?? "",
                    ["slot_kind"] = slot.SlotKind.ToString(),
                    ["mount_type"] = slot.MountType.ToString(),
                    ["installed_module_id"] = slot.InstalledModuleId ?? "",
                });
            }
        }, 0);
        return result;
    }

    // Diagnostic: returns total loot drops count + all drop node IDs for debugging.
    public Godot.Collections.Dictionary GetLootDiagV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["total"] = 0,
            ["player_node"] = "",
            ["drop_nodes"] = "",
        };
        TryExecuteSafeRead(state =>
        {
            result["total"] = state.LootDrops.Count;
            if (state.Fleets.TryGetValue("fleet_trader_1", out var player))
                result["player_node"] = player.CurrentNodeId ?? "";
            var nodes = new System.Collections.Generic.List<string>();
            foreach (var kv in state.LootDrops)
                nodes.Add(kv.Value.NodeId ?? "null");
            result["drop_nodes"] = string.Join(",", nodes);
        }, 0);
        return result;
    }

    // GATE.S5.LOOT.BRIDGE_PROOF.001: Returns loot drops at the player's current node.
    // [{drop_id, rarity, credits, goods_count, tick_created}]
    public Godot.Collections.Array GetNearbyLootV0()
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            var nodeId = player.CurrentNodeId;
            if (string.IsNullOrEmpty(nodeId)) return;

            foreach (var kv in state.LootDrops)
            {
                var drop = kv.Value;
                if (!string.Equals(drop.NodeId, nodeId, StringComparison.Ordinal)) continue;
                var d = new Godot.Collections.Dictionary();
                d["drop_id"] = drop.Id;
                d["rarity"] = drop.Rarity.ToString();
                d["credits"] = drop.Credits;
                d["goods_count"] = drop.Goods?.Count ?? 0;
                d["tick_created"] = drop.TickCreated;
                result.Add(d);
            }
        });
        return result;
    }

    // GATE.S5.LOOT.BRIDGE_PROOF.001: Dispatches collect loot command.
    // Returns {success, credits_gained, goods_gained}
    public Godot.Collections.Dictionary DispatchCollectLootV0(string dropId)
    {
        var result = new Godot.Collections.Dictionary();
        result["success"] = false;
        result["credits_gained"] = 0;
        result["goods_gained"] = 0;
        if (string.IsNullOrEmpty(dropId)) return result;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.LootDrops.TryGetValue(dropId, out var drop))
                return result;

            long creditsBefore = state.PlayerCredits;
            int cargoBefore = 0;
            foreach (var kv in state.PlayerCargo) cargoBefore += kv.Value;

            new SimCore.Commands.CollectLootCommand(dropId).Execute(state);

            long creditsGained = state.PlayerCredits - creditsBefore;
            int cargoAfter = 0;
            foreach (var kv in state.PlayerCargo) cargoAfter += kv.Value;
            int goodsGained = cargoAfter - cargoBefore;

            result["success"] = !state.LootDrops.ContainsKey(dropId);
            result["credits_gained"] = creditsGained;
            result["goods_gained"] = goodsGained;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }

    // ── GATE.S7.COMBAT_DEPTH2.BRIDGE.001: Combat projection + weapon tracking queries ──

    private Godot.Collections.Dictionary _cachedCombatProjectionV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns a pre-combat projection comparing two fleets.
    /// Keys: outcome (string: "victory"/"defeat"/"pyrrhic"/"stalemate"),
    ///       attacker_loss_pct (int 0-100), defender_loss_pct (int 0-100),
    ///       estimated_rounds (int).
    /// Nonblocking: returns last cached snapshot if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetCombatProjectionV0(string fleetId, string targetFleetId)
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["outcome"] = "stalemate",
                ["attacker_loss_pct"] = 0,
                ["defender_loss_pct"] = 0,
                ["estimated_rounds"] = 0,
            };

            if (!state.Fleets.TryGetValue(fleetId, out var attacker)
                || !state.Fleets.TryGetValue(targetFleetId, out var defender))
            {
                lock (_snapshotLock) { _cachedCombatProjectionV0 = d; }
                return;
            }

            var projection = CombatSystem.ProjectOutcome(attacker, defender);
            d["outcome"] = projection.Outcome switch
            {
                CombatSystem.ProjectedOutcome.Victory => "victory",
                CombatSystem.ProjectedOutcome.Defeat => "defeat",
                CombatSystem.ProjectedOutcome.Pyrrhic => "pyrrhic",
                CombatSystem.ProjectedOutcome.Stalemate => "stalemate",
                _ => "stalemate",
            };
            d["attacker_loss_pct"] = projection.AttackerLossPct;
            d["defender_loss_pct"] = projection.DefenderLossPct;
            d["estimated_rounds"] = projection.EstimatedRounds;

            lock (_snapshotLock) { _cachedCombatProjectionV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedCombatProjectionV0.Duplicate(); }
    }

    private Godot.Collections.Array _cachedWeaponTrackingV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns per-weapon-slot tracking details for a fleet.
    /// Array of dicts: slot_id (string), module_id (string), tracking_bps (int),
    ///                 ship_evasion_bps (int), hit_pct (int), armor_pen_bps (int).
    /// Nonblocking: returns last cached array if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetWeaponTrackingV0(string fleetId)
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();

            if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            {
                lock (_snapshotLock) { _cachedWeaponTrackingV0 = arr; }
                return;
            }

            int shipEvasionBps = CombatSystem.GetShipClassEvasionBps(fleet.ShipClassId);

            foreach (var slot in fleet.Slots)
            {
                if (slot.SlotKind != SimCore.Entities.SlotKind.Weapon) continue;
                if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;

                var family = CombatSystem.ClassifyWeapon(slot.InstalledModuleId);
                int trackingBps = CombatSystem.GetWeaponTrackingBps(slot.InstalledModuleId, family);
                int armorPenBps = CombatSystem.GetWeaponArmorPenBps(family, slot.MountType);

                // Hit chance: tracking vs evasion. Clamp 5%-95%.
                int hitBps = Math.Max(trackingBps - shipEvasionBps, 500);
                if (hitBps > 9500) hitBps = 9500;
                int hitPct = hitBps / 100;

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["slot_id"] = slot.SlotId ?? "",
                    ["module_id"] = slot.InstalledModuleId ?? "",
                    ["tracking_bps"] = trackingBps,
                    ["ship_evasion_bps"] = shipEvasionBps,
                    ["hit_pct"] = hitPct,
                    ["armor_pen_bps"] = armorPenBps,
                });
            }

            lock (_snapshotLock) { _cachedWeaponTrackingV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedWeaponTrackingV0; }
    }

    // ── GATE.S8.LATTICE_DRONES.BRIDGE.001: Drone queries + alerts ──

    private Godot.Collections.Array _cachedLatticeDroneAlertsV0 = new Godot.Collections.Array();
    private Godot.Collections.Dictionary _cachedDroneActivityV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns drone alerts at a specific node.
    /// Array of dicts: fleet_id (string), hull_hp (int), shield_hp (int),
    ///                 grace_ticks_remaining (int), is_hostile (bool).
    /// Nonblocking: returns last cached array if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetLatticeDroneAlertsV0(string nodeId)
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();

            if (string.IsNullOrEmpty(nodeId))
            {
                lock (_snapshotLock) { _cachedLatticeDroneAlertsV0 = arr; }
                return;
            }

            foreach (var kv in state.Fleets)
            {
                var fleet = kv.Value;
                if (!fleet.IsLatticeDrone) continue;
                if (!string.Equals(fleet.CurrentNodeId, nodeId, StringComparison.Ordinal)) continue;

                arr.Add(new Godot.Collections.Dictionary
                {
                    ["fleet_id"] = fleet.Id ?? "",
                    ["hull_hp"] = fleet.HullHp,
                    ["shield_hp"] = fleet.ShieldHp,
                    ["grace_ticks_remaining"] = fleet.LatticeDroneGraceTicksRemaining,
                    ["is_hostile"] = fleet.LatticeDroneGraceTicksRemaining <= 0,
                });
            }

            lock (_snapshotLock) { _cachedLatticeDroneAlertsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedLatticeDroneAlertsV0; }
    }

    /// <summary>
    /// Returns global drone activity summary.
    /// Keys: total_drones (int), nodes_with_drones (int), hostile_count (int), territorial_count (int).
    /// Nonblocking: returns last cached snapshot if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetDroneActivityV0()
    {
        TryExecuteSafeRead(state =>
        {
            int total = 0;
            int hostile = 0;
            int territorial = 0;
            var nodesWithDrones = new HashSet<string>(StringComparer.Ordinal);

            foreach (var kv in state.Fleets)
            {
                var fleet = kv.Value;
                if (!fleet.IsLatticeDrone) continue;
                total++;
                if (!string.IsNullOrEmpty(fleet.CurrentNodeId))
                    nodesWithDrones.Add(fleet.CurrentNodeId);
                if (fleet.LatticeDroneGraceTicksRemaining <= 0)
                    hostile++;
                else
                    territorial++;
            }

            var d = new Godot.Collections.Dictionary
            {
                ["total_drones"] = total,
                ["nodes_with_drones"] = nodesWithDrones.Count,
                ["hostile_count"] = hostile,
                ["territorial_count"] = territorial,
            };

            lock (_snapshotLock) { _cachedDroneActivityV0 = d; }
        }, 0);

        lock (_snapshotLock) { return _cachedDroneActivityV0.Duplicate(); }
    }

    // GATE.T61.SALVAGE.COLLECTION_UX.001: Salvage loot queries.

    /// Returns nearby loot drops at the player's current node.
    /// Array of {id, rarity, credits, goods (Dictionary), tick_created}.
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetSalvageLootV0()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        TryExecuteSafeRead(state =>
        {
            string? playerNode = null;
            if (state.Fleets.TryGetValue("fleet_trader_1", out var pf))
                playerNode = pf.CurrentNodeId;
            if (string.IsNullOrEmpty(playerNode)) return;

            foreach (var kv in state.LootDrops)
            {
                var drop = kv.Value;
                if (!string.Equals(drop.NodeId, playerNode, StringComparison.Ordinal)) continue;
                var goods = new Godot.Collections.Dictionary();
                foreach (var g in drop.Goods)
                    goods[g.Key] = g.Value;
                result.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = drop.Id,
                    ["rarity"] = drop.Rarity.ToString(),
                    ["credits"] = drop.Credits,
                    ["goods"] = goods,
                    ["tick_created"] = drop.TickCreated,
                });
            }
        }, 0);
        return result;
    }

    /// Collect a specific loot drop by ID — adds credits+goods to player.
    public bool CollectSalvageV0(string lootId)
    {
        if (string.IsNullOrEmpty(lootId)) return false;
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            var state = _kernel.State;
            if (!state.LootDrops.TryGetValue(lootId, out var drop)) { _stateLock.ExitWriteLock(); return false; }

            state.PlayerCredits += drop.Credits;
            if (state.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                foreach (var g in drop.Goods)
                {
                    fleet.Cargo.TryGetValue(g.Key, out int existing);
                    fleet.Cargo[g.Key] = existing + g.Value;
                }
            }
            state.LootDrops.Remove(lootId);
            success = true;
        }
        finally
        {
            if (_stateLock.IsWriteLockHeld)
                _stateLock.ExitWriteLock();
        }
        return success;
    }

    // ── Player targeting (presentation-layer, does not affect SimCore determinism) ──

    private string _lockedTargetFleetId = "";

    /// <summary>
    /// Returns true if at least one hostile NPC fleet is alive and in the same node as the player.
    /// Nonblocking read.
    /// </summary>
    public bool HasHostileInRangeV0()
    {
        bool result = false;
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            var playerNode = player.CurrentNodeId;
            foreach (var kv in state.Fleets)
            {
                if (string.Equals(kv.Key, "fleet_trader_1", StringComparison.Ordinal)) continue;
                var f = kv.Value;
                if (f.HullHp <= 0) continue;
                if (!string.Equals(f.CurrentNodeId, playerNode, StringComparison.Ordinal)) continue;
                if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal)) continue;
                result = true;
                return;
            }
        }, 0);
        return result;
    }

    /// <summary>
    /// Returns the currently locked target fleet ID, or "" if none.
    /// </summary>
    public string GetLockedTargetV0()
    {
        return _lockedTargetFleetId;
    }

    /// <summary>
    /// Lock onto a specific fleet. Pass "" to clear.
    /// </summary>
    public void SetLockedTargetV0(string fleetId)
    {
        _lockedTargetFleetId = fleetId ?? "";
    }

    /// <summary>
    /// Clear the current target lock.
    /// </summary>
    public void ClearLockedTargetV0()
    {
        _lockedTargetFleetId = "";
    }

    /// <summary>
    /// Cycle to the next hostile fleet in the same node. Returns the new target fleet ID.
    /// If no current lock, targets nearest. If already locked, cycles to next (by ID order).
    /// Returns "" if no hostiles available.
    /// </summary>
    public string CycleTargetV0()
    {
        string newTarget = "";
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            var playerNode = player.CurrentNodeId;

            // Collect all hostile fleets in same node, alive, sorted by ID.
            var hostiles = new List<string>();
            foreach (var kv in state.Fleets)
            {
                if (string.Equals(kv.Key, "fleet_trader_1", StringComparison.Ordinal)) continue;
                var f = kv.Value;
                if (f.HullHp <= 0) continue;
                if (!string.Equals(f.CurrentNodeId, playerNode, StringComparison.Ordinal)) continue;
                if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal)) continue;
                hostiles.Add(kv.Key);
            }
            if (hostiles.Count == 0) return;
            hostiles.Sort(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(_lockedTargetFleetId))
            {
                newTarget = hostiles[0];
            }
            else
            {
                int idx = hostiles.IndexOf(_lockedTargetFleetId);
                newTarget = hostiles[(idx + 1) % hostiles.Count];
            }
        }, 0);

        _lockedTargetFleetId = newTarget;
        return newTarget;
    }

    /// <summary>
    /// Target the nearest hostile fleet in the same node. Returns the fleet ID or "".
    /// </summary>
    public string TargetNearestHostileV0()
    {
        string nearest = "";
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var player)) return;
            var playerNode = player.CurrentNodeId;

            foreach (var kv in state.Fleets)
            {
                if (string.Equals(kv.Key, "fleet_trader_1", StringComparison.Ordinal)) continue;
                var f = kv.Value;
                if (f.HullHp <= 0) continue;
                if (!string.Equals(f.CurrentNodeId, playerNode, StringComparison.Ordinal)) continue;
                if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal)) continue;
                nearest = kv.Key;
                return; // First hostile found — good enough for nearest heuristic.
            }
        }, 0);

        _lockedTargetFleetId = nearest;
        return nearest;
    }
}
