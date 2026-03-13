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

    // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Combat stance for zone hit distribution.
    public enum CombatStance { Charge, Broadside, Kite }

    public sealed class CombatProfile
    {
        public int HullHp { get; set; }
        public int HullHpMax { get; set; }
        public int ShieldHp { get; set; }
        public int ShieldHpMax { get; set; }
        public List<WeaponInfo> Weapons { get; set; } = new();
        // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Zone armor HP per facing.
        public int[] ZoneArmorHp { get; set; } = new int[4];
        public string ShipClassId { get; set; } = "";
        // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Heat management fields.
        public int HeatCapacity { get; set; } = CombatTweaksV0.DefaultHeatCapacity;
        public int RejectionRate { get; set; } = CombatTweaksV0.DefaultRejectionRate;
        // GATE.S7.COMBAT_PHASE2.BATTLE_STATIONS.001: Readiness damage multiplier (pct).
        public int ReadinessDamagePct { get; set; } = CombatTweaksV0.NeutralPct;
        // GATE.S7.COMBAT_PHASE2.RADIATOR.001: Total radiator bonus (removed if aft zone destroyed).
        public int RadiatorBonusRate { get; set; }
    }

    public sealed class WeaponInfo
    {
        public string ModuleId { get; set; } = "";
        public int BaseDamage { get; set; }
        public DamageFamily Family { get; set; } = DamageFamily.Neutral;
        // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Heat generated per weapon fire.
        public int HeatPerShot { get; set; } = CombatTweaksV0.DefaultHeatPerShot;
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
            ShipClassId = fleet.ShipClassId,
            // GATE.S7.COMBAT_PHASE2.BATTLE_STATIONS.001: Readiness from fleet state.
            ReadinessDamagePct = fleet.BattleStations switch
            {
                BattleStationsState.StandDown => CombatTweaksV0.StandDownDamagePct,
                BattleStationsState.SpinningUp => CombatTweaksV0.SpinningUpDamagePct,
                _ => CombatTweaksV0.NeutralPct,
            },
        };
        // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Copy zone armor.
        Array.Copy(fleet.ZoneArmorHp, profile.ZoneArmorHp, fleet.ZoneArmorHp.Length);

        // GATE.S7.COMBAT_PHASE2.RADIATOR.001: Sum radiator bonus from installed modules.
        int totalRadiatorBonus = 0; // STRUCTURAL: accumulator init
        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;

            if (slot.SlotKind == SlotKind.Weapon)
            {
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

            // Sum radiator bonus from any slot type.
            var moduleDef = UpgradeContentV0.GetById(slot.InstalledModuleId);
            if (moduleDef is { IsRadiator: true })
                totalRadiatorBonus += moduleDef.RadiatorBonusRate;
        }

        profile.RejectionRate += totalRadiatorBonus;
        profile.RadiatorBonusRate = totalRadiatorBonus;

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
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Fore] = CombatTweaksV0.DefaultZoneArmorFore;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Port] = CombatTweaksV0.DefaultZoneArmorPort;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Starboard] = CombatTweaksV0.DefaultZoneArmorStbd;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Aft] = CombatTweaksV0.DefaultZoneArmorAft;
        }
        else
        {
            fleet.HullHpMax = CombatTweaksV0.AiHullHpMax;
            fleet.ShieldHpMax = CombatTweaksV0.AiShieldHpMax;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Fore] = CombatTweaksV0.AiZoneArmorFore;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Port] = CombatTweaksV0.AiZoneArmorPort;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Starboard] = CombatTweaksV0.AiZoneArmorStbd;
            fleet.ZoneArmorHpMax[(int)ZoneFacing.Aft] = CombatTweaksV0.AiZoneArmorAft;
        }
        fleet.HullHp = fleet.HullHpMax;
        fleet.ShieldHp = fleet.ShieldHpMax;
        Array.Copy(fleet.ZoneArmorHpMax, fleet.ZoneArmorHp, fleet.ZoneArmorHpMax.Length);
    }

    // GATE.S18.SHIP_MODULES.ZONE_ARMOR.001: Zone armor damage routing result.
    public sealed class ZoneDamageResult
    {
        public int ShieldDmg { get; set; }
        public int ZoneArmorDmg { get; set; }
        public int HullDmg { get; set; }
        public ZoneFacing Facing { get; set; }
    }

    /// <summary>
    /// GATE.S18.SHIP_MODULES.ZONE_ARMOR.001: Calculate damage with zone armor layer.
    /// Flow: Shield absorbs first → ZoneArmor[facing] absorbs remainder → Hull takes rest.
    /// Integer arithmetic only, deterministic.
    /// </summary>
    public static ZoneDamageResult CalcDamageWithZoneArmor(
        int baseDamage, DamageFamily family,
        int defenderShieldHp, int zoneArmorHp, int defenderHullHp,
        ZoneFacing facing)
    {
        var result = new ZoneDamageResult { Facing = facing };

        // Apply counter family multipliers.
        int shieldMultPct = family switch
        {
            DamageFamily.Kinetic => CombatTweaksV0.KineticVsShieldPct,
            DamageFamily.Energy => CombatTweaksV0.EnergyVsShieldPct,
            _ => CombatTweaksV0.NeutralPct,
        };
        int hullMultPct = family switch
        {
            DamageFamily.Kinetic => CombatTweaksV0.KineticVsHullPct,
            DamageFamily.Energy => CombatTweaksV0.EnergyVsHullPct,
            _ => CombatTweaksV0.NeutralPct,
        };

        int effectiveVsShield = baseDamage * shieldMultPct / CombatTweaksV0.NeutralPct;
        int effectiveVsHull = baseDamage * hullMultPct / CombatTweaksV0.NeutralPct;

        int remaining = effectiveVsShield;

        // Layer 1: Shield absorbs.
        if (defenderShieldHp > 0)
        {
            result.ShieldDmg = Math.Min(remaining, defenderShieldHp);
            remaining -= result.ShieldDmg;
            // Convert overflow from shield-effective to hull-effective damage.
            if (remaining > 0)
                remaining = remaining * hullMultPct / shieldMultPct;
        }
        else
        {
            remaining = effectiveVsHull;
        }

        // Layer 2: Zone armor absorbs (uses hull-effective damage).
        if (remaining > 0 && zoneArmorHp > 0)
        {
            result.ZoneArmorDmg = Math.Min(remaining, zoneArmorHp);
            remaining -= result.ZoneArmorDmg;
        }

        // Layer 3: Hull absorbs remainder.
        if (remaining > 0)
        {
            result.HullDmg = Math.Min(remaining, defenderHullHp);
        }

        return result;
    }

    // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Determine stance from ship class.
    public static CombatStance DetermineStance(string shipClassId)
    {
        return shipClassId switch
        {
            "frigate" or "dreadnought" => CombatStance.Charge,
            "clipper" or "shuttle" => CombatStance.Kite,
            _ => CombatStance.Broadside, // corvette, cruiser, hauler, carrier, unknown
        };
    }

    // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Get hit distribution for a stance (Fore, Port, Stbd, Aft).
    public static int[] GetStanceDistribution(CombatStance stance)
    {
        return stance switch
        {
            CombatStance.Charge => new[] {
                CombatTweaksV0.ChargeForePct, CombatTweaksV0.ChargePortPct,
                CombatTweaksV0.ChargeStbdPct, CombatTweaksV0.ChargeAftPct },
            CombatStance.Kite => new[] {
                CombatTweaksV0.KiteForePct, CombatTweaksV0.KitePortPct,
                CombatTweaksV0.KiteStbdPct, CombatTweaksV0.KiteAftPct },
            _ => new[] {
                CombatTweaksV0.BroadsideForePct, CombatTweaksV0.BroadsidePortPct,
                CombatTweaksV0.BroadsideStbdPct, CombatTweaksV0.BroadsideAftPct },
        };
    }

    // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Pick zone facing based on stance distribution.
    // Uses deterministic round-robin weighted selection: accumulates hit pct and selects
    // the zone whose cumulative weight bracket contains (weaponIndex * 100 / totalWeapons) % 100.
    public static ZoneFacing PickFacing(CombatStance stance, int weaponIndex, int totalWeapons)
    {
        if (totalWeapons <= 0) return ZoneFacing.Fore;
        int[] dist = GetStanceDistribution(stance);
        int slot = (weaponIndex * CombatTweaksV0.NeutralPct / totalWeapons) % CombatTweaksV0.NeutralPct;
        int cumulative = 0;
        for (int i = 0; i < dist.Length; i++)
        {
            cumulative += dist[i];
            if (slot < cumulative) return (ZoneFacing)i;
        }
        return ZoneFacing.Aft;
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

    // ── GATE.S5.COMBAT_RES.SYSTEM.001: Strategic combat resolution wrapper ──

    public enum CombatResolutionOutcome { Victory, Defeat, Flee }

    public sealed class CombatResolution
    {
        public string AttackerId { get; set; } = "";
        public string DefenderId { get; set; } = "";
        public CombatResolutionOutcome Outcome { get; set; }
        public int RoundsPlayed { get; set; }
        public int AttackerHullRemaining { get; set; }
        public int DefenderHullRemaining { get; set; }
        public int SalvageValue { get; set; }
    }

    /// <summary>
    /// High-level combat resolution: builds profiles, runs strategic resolver, returns outcome
    /// with flee logic (attacker survives max rounds but loses = flee).
    /// </summary>
    public static CombatResolution ResolveCombatV0(
        Fleet attacker, Fleet defender,
        IReadOnlyDictionary<string, int>? weaponBaseDamage = null)
    {
        var profileA = BuildProfile(attacker, weaponBaseDamage);
        var profileB = BuildProfile(defender, weaponBaseDamage);

        var result = StrategicResolverV0.Resolve(profileA, profileB);

        var resolution = new CombatResolution
        {
            AttackerId = attacker.Id,
            DefenderId = defender.Id,
            RoundsPlayed = result.RoundsPlayed,
            AttackerHullRemaining = result.FleetAHullRemaining,
            DefenderHullRemaining = result.FleetBHullRemaining,
            SalvageValue = result.SalvageValue,
        };

        if (result.Winner == StrategicResolverV0.Winner.A)
        {
            resolution.Outcome = CombatResolutionOutcome.Victory;
        }
        else if (result.Winner == StrategicResolverV0.Winner.B)
        {
            resolution.Outcome = CombatResolutionOutcome.Defeat;
        }
        else
        {
            // Draw (max rounds, neither destroyed) — attacker flees
            resolution.Outcome = CombatResolutionOutcome.Flee;
        }

        return resolution;
    }
}
