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
        int tickBefore = GetSimTickV0();
        EnqueueCommand(new StartCombatCommand("fleet_trader_1", opponentFleetId));
        WaitForTickAdvance(tickBefore, 200);
    }

    // Clears combat state flags so the next GetCombatStatusV0 returns in_combat=false.
    public void DispatchClearCombatV0()
    {
        int tickBefore = GetSimTickV0();
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
            result["hull"] = fleet.HullHp;
            result["hull_max"] = fleet.HullHpMax;
            result["shield"] = fleet.ShieldHp;
            result["shield_max"] = fleet.ShieldHpMax;
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
    public Godot.Collections.Dictionary SetDoctrineV0(string fleetId, string doctrineType, bool enabled, string targetFleetId = "")
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

            player.ShieldHp -= dmg.ShieldDmg;
            player.HullHp -= dmg.HullDmg;

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
}
