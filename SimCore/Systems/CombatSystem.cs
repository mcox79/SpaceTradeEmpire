using System;
using System.Collections.Generic;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

/// <summary>
/// Deterministic combat system v0 (GATE.S5.COMBAT_LOCAL.DAMAGE_MODEL.001).
/// </summary>
public static class CombatSystem
{
    // GATE.S5.COMBAT.COUNTER_FAMILY.001: PointDefense added alongside existing families.
    public enum DamageFamily { Neutral, Kinetic, Energy, PointDefense }

    // GATE.S5.COMBAT.COUNTER_FAMILY.001: weapon families that PointDefense counters.
    public enum TargetWeaponFamily { Other, Missile, Torpedo }

    public sealed class DamageResult
    {
        public int ShieldDmg { get; set; }
        public int HullDmg { get; set; }
        public int Overkill { get; set; }
    }

    public sealed class CombatProfile
    {
        public int HullHp { get; set; }
        public int HullHpMax { get; set; }
        public int ShieldHp { get; set; }
        public int ShieldHpMax { get; set; }
        public List<WeaponInfo> Weapons { get; set; } = new();
    }

    public sealed class WeaponInfo
    {
        public string ModuleId { get; set; } = "";
        public int BaseDamage { get; set; }
        public DamageFamily Family { get; set; } = DamageFamily.Neutral;
    }

    public static DamageFamily ClassifyWeapon(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return DamageFamily.Neutral;
        if (moduleId.Contains("cannon", StringComparison.Ordinal)) return DamageFamily.Kinetic;
        if (moduleId.Contains("laser", StringComparison.Ordinal)) return DamageFamily.Energy;
        // GATE.S5.COMBAT.COUNTER_FAMILY.001
        if (moduleId.Contains("point_defense", StringComparison.Ordinal)) return DamageFamily.PointDefense;
        return DamageFamily.Neutral;
    }

    // GATE.S5.COMBAT.COUNTER_FAMILY.001: Classify a weapon module by its target family (used to determine
    // whether PointDefense counter bonus applies).
    public static TargetWeaponFamily ClassifyTargetWeaponFamily(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return TargetWeaponFamily.Other;
        if (moduleId.Contains("missile", StringComparison.Ordinal)) return TargetWeaponFamily.Missile;
        if (moduleId.Contains("torpedo", StringComparison.Ordinal)) return TargetWeaponFamily.Torpedo;
        return TargetWeaponFamily.Other;
    }

    public static CombatProfile BuildProfile(Fleet fleet, IReadOnlyDictionary<string, int>? weaponBaseDamage = null)
    {
        var profile = new CombatProfile
        {
            HullHp = fleet.HullHp,
            HullHpMax = fleet.HullHpMax,
            ShieldHp = fleet.ShieldHp,
            ShieldHpMax = fleet.ShieldHpMax,
        };

        foreach (var slot in fleet.Slots)
        {
            if (slot.SlotKind != SlotKind.Weapon) continue;
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;

            int baseDmg = CombatTweaksV0.DefaultWeaponBaseDamage;
            if (weaponBaseDamage != null && weaponBaseDamage.TryGetValue(slot.InstalledModuleId, out var d))
                baseDmg = d;

            profile.Weapons.Add(new WeaponInfo
            {
                ModuleId = slot.InstalledModuleId,
                BaseDamage = baseDmg,
                Family = ClassifyWeapon(slot.InstalledModuleId),
            });
        }

        return profile;
    }

    /// <summary>
    /// Calculate damage from a single weapon hit against a defender.
    /// Applies counter family multipliers, then distributes to shields first, overflow to hull.
    /// </summary>
    public static DamageResult CalcDamage(int baseDamage, DamageFamily family, int defenderShieldHp, int defenderHullHp)
        => CalcDamage(baseDamage, family, defenderShieldHp, defenderHullHp, TargetWeaponFamily.Other);

    /// <summary>
    /// GATE.S5.COMBAT.COUNTER_FAMILY.001: Calculate damage from a single weapon hit against a defender,
    /// with awareness of the target's weapon family for PointDefense counter bonus.
    /// When the firing weapon is PointDefense and the target uses missiles/torpedoes, apply 2x damage multiplier.
    /// No bonus or penalty for any other combination.
    /// </summary>
    public static DamageResult CalcDamage(int baseDamage, DamageFamily family, int defenderShieldHp, int defenderHullHp, TargetWeaponFamily targetWeaponFamily)
    {
        // GATE.S5.COMBAT.COUNTER_FAMILY.001: PointDefense counter bonus.
        int effectiveBaseDamage = baseDamage;
        if (family == DamageFamily.PointDefense &&
            (targetWeaponFamily == TargetWeaponFamily.Missile || targetWeaponFamily == TargetWeaponFamily.Torpedo))
        {
            effectiveBaseDamage = baseDamage * CombatTweaksV0.PointDefenseCounterMultiplierPct / CombatTweaksV0.NeutralPct;
        }

        // Determine effective damage vs shields and hull using counter families.
        int shieldMultPct = family switch
        {
            DamageFamily.Kinetic => CombatTweaksV0.KineticVsShieldPct,
            DamageFamily.Energy => CombatTweaksV0.EnergyVsShieldPct,
            // PointDefense uses neutral multipliers (its power comes from the counter bonus above).
            _ => CombatTweaksV0.NeutralPct,
        };
        int hullMultPct = family switch
        {
            DamageFamily.Kinetic => CombatTweaksV0.KineticVsHullPct,
            DamageFamily.Energy => CombatTweaksV0.EnergyVsHullPct,
            _ => CombatTweaksV0.NeutralPct,
        };

        // Swap local variable so rest of method is unchanged.
        baseDamage = effectiveBaseDamage;

        // Calculate effective damage vs each layer (integer arithmetic, no floats).
        int effectiveVsShield = baseDamage * shieldMultPct / CombatTweaksV0.NeutralPct;
        int effectiveVsHull = baseDamage * hullMultPct / CombatTweaksV0.NeutralPct;

        var result = new DamageResult();

        // Shields absorb first.
        if (defenderShieldHp > 0)
        {
            result.ShieldDmg = Math.Min(effectiveVsShield, defenderShieldHp);
            int shieldOverflow = effectiveVsShield - result.ShieldDmg;
            // Overflow converts to hull damage at hull multiplier ratio.
            if (shieldOverflow > 0)
            {
                int overflowHull = shieldOverflow * hullMultPct / shieldMultPct;
                result.HullDmg = Math.Min(overflowHull, defenderHullHp);
            }
        }
        else
        {
            // No shields: full hull damage.
            result.HullDmg = Math.Min(effectiveVsHull, defenderHullHp);
        }

        // Overkill: damage beyond what was needed to destroy the target.
        int totalHpBefore = defenderShieldHp + defenderHullHp;
        int totalDmgDealt = result.ShieldDmg + result.HullDmg;
        int remainingHp = totalHpBefore - totalDmgDealt;
        result.Overkill = remainingHp < 0 ? -remainingHp : 0;

        return result;
    }

    // GATE.S5.COMBAT.ESCORT_DOCTRINE.001: Calculate the shield damage reduction for a fleet that is
    // being escorted. Incoming shield damage is reduced by EscortShieldDamageReductionPct percent.
    // Returns the reduced shield damage value (integer arithmetic, deterministic, no floats).
    public static int ApplyEscortShieldReduction(int incomingShieldDmg)
    {
        // Reduction formula: dmg * (100 - reductionPct) / 100
        return incomingShieldDmg * (CombatTweaksV0.NeutralPct - CombatTweaksV0.EscortShieldDamageReductionPct) / CombatTweaksV0.NeutralPct;
    }

    public static void InitFleetCombatStats(Fleet fleet, bool isPlayer)
    {
        if (isPlayer)
        {
            fleet.HullHpMax = CombatTweaksV0.DefaultHullHpMax;
            fleet.ShieldHpMax = CombatTweaksV0.DefaultShieldHpMax;
        }
        else
        {
            fleet.HullHpMax = CombatTweaksV0.AiHullHpMax;
            fleet.ShieldHpMax = CombatTweaksV0.AiShieldHpMax;
        }
        fleet.HullHp = fleet.HullHpMax;
        fleet.ShieldHp = fleet.ShieldHpMax;
    }

    // ── GATE.S5.COMBAT_LOCAL.COMBAT_TICK.001: Combat encounter lifecycle ──

    public sealed class CombatEventEntry
    {
        public int Tick { get; set; }
        public string AttackerId { get; set; } = "";
        public string DefenderId { get; set; } = "";
        public string WeaponId { get; set; } = "";
        public int DamageDealt { get; set; }
        public int DefenderHullRemaining { get; set; }
        public int DefenderShieldRemaining { get; set; }
    }

    public enum CombatOutcome { InProgress, Win, Loss, Draw }

    // ── GATE.S5.COMBAT_LOCAL.COMBAT_LOG.001: Combat log + cause chain ──

    public sealed class CombatLog
    {
        public List<CombatEventEntry> Events { get; set; } = new();
        public CombatOutcome Outcome { get; set; } = CombatOutcome.InProgress;
        public string CauseOfDeath { get; set; } = "";
    }

    /// <summary>
    /// Run one combat round: each fleet fires all weapons at the opponent.
    /// Attacker fires first, then defender (if still alive). Returns events for this tick.
    /// </summary>
    public static void TickCombat(
        Fleet attacker, Fleet defender,
        IReadOnlyDictionary<string, int>? weaponBaseDamage,
        int tick,
        CombatLog log)
    {
        // Attacker fires all weapons
        FireWeapons(attacker, defender, weaponBaseDamage, tick, log);

        // Defender fires back (if still alive)
        if (defender.HullHp > 0)
            FireWeapons(defender, attacker, weaponBaseDamage, tick, log);

        // Check outcome
        bool attackerDead = attacker.HullHp <= 0;
        bool defenderDead = defender.HullHp <= 0;

        if (attackerDead && defenderDead)
        {
            log.Outcome = CombatOutcome.Draw;
            log.CauseOfDeath = $"mutual destruction at tick {tick}";
        }
        else if (defenderDead)
        {
            log.Outcome = CombatOutcome.Win;
        }
        else if (attackerDead)
        {
            log.Outcome = CombatOutcome.Loss;
            // Find the weapon that dealt the killing blow
            for (int i = log.Events.Count - 1; i >= 0; i--)
            {
                var ev = log.Events[i];
                if (ev.DefenderId == attacker.Id && ev.DefenderHullRemaining <= 0)
                {
                    log.CauseOfDeath = $"hull destroyed by {ev.WeaponId} from {ev.AttackerId} at tick {ev.Tick}";
                    break;
                }
            }
        }
    }

    private static void FireWeapons(
        Fleet shooter, Fleet target,
        IReadOnlyDictionary<string, int>? weaponBaseDamage,
        int tick,
        CombatLog log)
    {
        foreach (var slot in shooter.Slots)
        {
            if (slot.SlotKind != SlotKind.Weapon) continue;
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
            if (target.HullHp <= 0) break;

            int baseDmg = CombatTweaksV0.DefaultWeaponBaseDamage;
            if (weaponBaseDamage != null && weaponBaseDamage.TryGetValue(slot.InstalledModuleId, out var d))
                baseDmg = d;

            var family = ClassifyWeapon(slot.InstalledModuleId);
            var result = CalcDamage(baseDmg, family, target.ShieldHp, target.HullHp);

            target.ShieldHp = Math.Max(0, target.ShieldHp - result.ShieldDmg);
            target.HullHp = Math.Max(0, target.HullHp - result.HullDmg);

            log.Events.Add(new CombatEventEntry
            {
                Tick = tick,
                AttackerId = shooter.Id,
                DefenderId = target.Id,
                WeaponId = slot.InstalledModuleId,
                DamageDealt = result.ShieldDmg + result.HullDmg,
                DefenderHullRemaining = target.HullHp,
                DefenderShieldRemaining = target.ShieldHp,
            });
        }
    }

    /// <summary>
    /// Run a full combat encounter until one side is destroyed or max rounds reached.
    /// </summary>
    public static CombatLog RunEncounter(
        Fleet attacker, Fleet defender,
        IReadOnlyDictionary<string, int>? weaponBaseDamage,
        int maxRounds = 100)
    {
        var log = new CombatLog();
        for (int tick = 1; tick <= maxRounds; tick++)
        {
            TickCombat(attacker, defender, weaponBaseDamage, tick, log);
            if (log.Outcome != CombatOutcome.InProgress) break;
        }
        return log;
    }
}
