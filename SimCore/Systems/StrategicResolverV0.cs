using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

/// <summary>
/// Deterministic fleet-vs-fleet attrition resolver v0.
/// GATE.S5.COMBAT.STRATEGIC_RESOLVER.001 — multi-round attrition.
/// GATE.S5.COMBAT.REPLAY_PROOF.001 — deterministic frame capture + golden hash.
/// </summary>
public static class StrategicResolverV0
{
    // STRUCTURAL: sentinel for "no rounds played yet"
    private const int STRUCT_ZERO = 0; // STRUCTURAL: initial rounds counter
    private const int STRUCT_FIRST_ROUND = 1; // STRUCTURAL: 1-based round numbering start

    public enum Winner { A, B, Draw }

    /// <summary>
    /// Per-round state snapshot for deterministic replay.
    /// GATE.S5.COMBAT.REPLAY_PROOF.001
    /// </summary>
    public sealed class ReplayFrame
    {
        public int Round { get; set; }
        public int AHullRemaining { get; set; }
        public int AShieldRemaining { get; set; }
        public int BHullRemaining { get; set; }
        public int BShieldRemaining { get; set; }
        /// <summary>Total damage dealt by both sides in this round.</summary>
        public int DamageThisRound { get; set; }
        // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Heat state per frame for replay.
        public int AHeat { get; set; }
        public int BHeat { get; set; }

        /// <summary>
        /// Deterministic pipe-delimited serialization (no floats, no culture-dependent output).
        /// Format: "round|ahull|ashield|bhull|bshield|damage|aheat|bheat"
        /// </summary>
        public string Serialize() =>
            $"{Round}|{AHullRemaining}|{AShieldRemaining}|{BHullRemaining}|{BShieldRemaining}|{DamageThisRound}|{AHeat}|{BHeat}";
    }

    public sealed class StrategicResult
    {
        public Winner Winner { get; set; }
        public int RoundsPlayed { get; set; }
        public int FleetAHullRemaining { get; set; }
        public int FleetBHullRemaining { get; set; }
        /// <summary>Total damage dealt across all rounds (salvage proxy).</summary>
        public int SalvageValue { get; set; }
        /// <summary>Per-round frames for replay verification. GATE.S5.COMBAT.REPLAY_PROOF.001</summary>
        public List<ReplayFrame> Frames { get; set; } = new();
    }

    /// <summary>
    /// Resolve a strategic fleet-vs-fleet attrition combat.
    /// Operates on mutable copies of the profiles so the originals are unmodified.
    /// Deterministic: no RNG, no timestamps.
    /// GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Now routes damage through zone armor.
    /// </summary>
    public static StrategicResult Resolve(
        CombatSystem.CombatProfile fleetA,
        CombatSystem.CombatProfile fleetB,
        bool fleetAEscorted = false,
        bool fleetBEscorted = false,
        int attackerMinDamageFloor = 0)
    {
        // Work on mutable copies so caller's profiles are unchanged.
        int aHull = fleetA.HullHp;
        int aShield = fleetA.ShieldHp;
        int bHull = fleetB.HullHp;
        int bShield = fleetB.ShieldHp;
        // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Mutable zone armor copies.
        int[] aZone = (int[])fleetA.ZoneArmorHp.Clone();
        int[] bZone = (int[])fleetB.ZoneArmorHp.Clone();
        // GATE.S7.COMBAT_DEPTH2.FORE_KILL.001: Only track fore-kill for ships that have zone armor.
        bool aHasZoneArmor = Array.Exists(fleetA.ZoneArmorHp, z => z > 0); // STRUCTURAL: check
        bool bHasZoneArmor = Array.Exists(fleetB.ZoneArmorHp, z => z > 0); // STRUCTURAL: check
        var aStance = CombatSystem.DetermineStance(fleetA.ShipClassId);
        var bStance = CombatSystem.DetermineStance(fleetB.ShipClassId);

        // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Heat tracking (starts at zero each combat).
        int aHeat = STRUCT_ZERO;
        int bHeat = STRUCT_ZERO;

        // GATE.S7.COMBAT_PHASE2.RADIATOR.001: Mutable rejection rates (radiator loss reduces cooling).
        int aRejection = fleetA.RejectionRate;
        int bRejection = fleetB.RejectionRate;
        bool aRadiatorLost = false;
        bool bRadiatorLost = false;

        int totalSalvage = STRUCT_ZERO;
        int roundsPlayed = STRUCT_ZERO;

        // GATE.T67.COMBAT.SHIELD_GRACE.001: Track hull HP at start of each round for damage cap.
        int aHullMax = aHull;
        int bHullMax = bHull;
        int maxRounds = Math.Min(CombatTweaksV0.StrategicMaxRounds, CombatDepthTweaksV0.MaxCombatRounds);

        var frames = new List<ReplayFrame>();

        for (int round = STRUCT_FIRST_ROUND; round <= maxRounds; round++)
        {
            int damageThisRound = STRUCT_ZERO;

            // GATE.T67.COMBAT.SHIELD_GRACE.001: Snapshot hull before this round for damage cap.
            int aHullBefore = aHull;
            int bHullBefore = bHull;

            // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001 + BATTLE_STATIONS.001:
            // Combined damage multiplier = min(heatPct, readinessPct).
            // Heat and readiness independently limit damage output.
            int aHeatPct = ComputeHeatDamagePct(aHeat, fleetA.HeatCapacity);
            int aDamagePct = Math.Min(aHeatPct, fleetA.ReadinessDamagePct);
            int bHeatPct = ComputeHeatDamagePct(bHeat, fleetB.HeatCapacity);
            int bDamagePct = Math.Min(bHeatPct, fleetB.ReadinessDamagePct);

            // GATE.T67.COMBAT.SHIELD_GRACE.001: Attrition escalation after round N.
            // Damage bonus increases each round past the threshold to prevent stalemates.
            if (round > CombatDepthTweaksV0.AttritionStartRound)
            {
                int attritionRounds = round - CombatDepthTweaksV0.AttritionStartRound;
                int bonusBps = attritionRounds * CombatDepthTweaksV0.AttritionBonusBpsPerRound;
                aDamagePct = aDamagePct + aDamagePct * bonusBps / 10000;
                bDamagePct = bDamagePct + bDamagePct * bonusBps / 10000;
            }

            // ── Fleet A fires all weapons at Fleet B ──
            // A attacks B → B's stance determines which zone gets hit.
            int aDamageDealt = FireAllWeapons(
                fleetA.Weapons,
                ref bShield,
                ref bHull,
                bZone,
                bStance,
                targetEscorted: fleetBEscorted,
                targetWeaponFamilyForPd: DetermineTargetFamily(fleetB.Weapons),
                damagePct: aDamagePct,
                heat: ref aHeat,
                spinRpm: fleetA.SpinRpm,
                targetEvasionBps: fleetB.EvasionBps,
                round: round,
                shooterId: "A",
                shooterZoneArmor: aHasZoneArmor ? aZone : null,
                targetSpinRpm: fleetB.SpinRpm); // GATE.T60.SPIN.ARMOR_HEAT.001
            damageThisRound += aDamageDealt;

            // GATE.T64.COMBAT.SEED_FLOOR.001: Guarantee minimum damage per round from attacker.
            // If fleet A's weapons dealt less than the floor, apply deficit as direct hull damage.
            if (attackerMinDamageFloor > STRUCT_ZERO && aDamageDealt < attackerMinDamageFloor && bHull > STRUCT_ZERO)
            {
                int deficit = attackerMinDamageFloor - aDamageDealt;
                bHull = Math.Max(STRUCT_ZERO, bHull - deficit);
                damageThisRound += deficit;
            }

            // ── Fleet B fires back (only if still alive) ──
            if (bHull > STRUCT_ZERO)
            {
                damageThisRound += FireAllWeapons(
                    fleetB.Weapons,
                    ref aShield,
                    ref aHull,
                    aZone,
                    aStance,
                    targetEscorted: fleetAEscorted,
                    targetWeaponFamilyForPd: DetermineTargetFamily(fleetA.Weapons),
                    damagePct: bDamagePct,
                    heat: ref bHeat,
                    spinRpm: fleetB.SpinRpm,
                    targetEvasionBps: fleetA.EvasionBps,
                    round: round,
                    shooterId: "B",
                    shooterZoneArmor: bHasZoneArmor ? bZone : null,
                    targetSpinRpm: fleetA.SpinRpm); // GATE.T60.SPIN.ARMOR_HEAT.001
            }

            // GATE.T67.COMBAT.SHIELD_GRACE.001: Shield grace — first N rounds, hull damage absorbed.
            if (round <= CombatDepthTweaksV0.ShieldGraceRounds)
            {
                // Restore hull to pre-round value — shields absorb all hull damage.
                aHull = aHullBefore;
                bHull = bHullBefore;
            }
            else
            {
                // GATE.T67.COMBAT.SHIELD_GRACE.001: Hull damage cap — max 33% of max hull per round.
                int aMaxHullDmg = (int)((long)aHullMax * CombatDepthTweaksV0.MaxHullDamagePerRoundBps / 10000); // STRUCTURAL: 10000 bps
                if (aMaxHullDmg < 1) aMaxHullDmg = 1; // STRUCTURAL: minimum 1 damage
                int aHullDmgThisRound = aHullBefore - aHull;
                if (aHullDmgThisRound > aMaxHullDmg)
                    aHull = aHullBefore - aMaxHullDmg;

                int bMaxHullDmg = (int)((long)bHullMax * CombatDepthTweaksV0.MaxHullDamagePerRoundBps / 10000); // STRUCTURAL: 10000 bps
                if (bMaxHullDmg < 1) bMaxHullDmg = 1; // STRUCTURAL: minimum 1 damage
                int bHullDmgThisRound = bHullBefore - bHull;
                if (bHullDmgThisRound > bMaxHullDmg)
                    bHull = bHullBefore - bMaxHullDmg;
            }

            // GATE.S7.COMBAT_PHASE2.RADIATOR.001: If aft zone is destroyed, lose radiator bonus.
            if (!aRadiatorLost && fleetA.RadiatorBonusRate > STRUCT_ZERO && aZone[(int)ZoneFacing.Aft] <= STRUCT_ZERO)
            {
                aRejection = Math.Max(STRUCT_ZERO, aRejection - fleetA.RadiatorBonusRate);
                aRadiatorLost = true;
            }
            if (!bRadiatorLost && fleetB.RadiatorBonusRate > STRUCT_ZERO && bZone[(int)ZoneFacing.Aft] <= STRUCT_ZERO)
            {
                bRejection = Math.Max(STRUCT_ZERO, bRejection - fleetB.RadiatorBonusRate);
                bRadiatorLost = true;
            }

            // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Passive cooling at end of round.
            // GATE.T60.SPIN.ARMOR_HEAT.001: Spin bonus to heat rejection.
            int aEffectiveRejection = aRejection + (int)((long)aRejection * fleetA.SpinRpm * CombatTweaksV0.SpinHeatRejectionBonusBpsPerRpm / 10000); // STRUCTURAL: 10000 = 100%
            int bEffectiveRejection = bRejection + (int)((long)bRejection * fleetB.SpinRpm * CombatTweaksV0.SpinHeatRejectionBonusBpsPerRpm / 10000); // STRUCTURAL: 10000 = 100%
            aHeat = Math.Max(STRUCT_ZERO, aHeat - aEffectiveRejection);
            bHeat = Math.Max(STRUCT_ZERO, bHeat - bEffectiveRejection);

            totalSalvage += damageThisRound;
            roundsPlayed = round;

            // Capture frame AFTER all firing for this round.
            frames.Add(new ReplayFrame
            {
                Round = round,
                AHullRemaining = aHull,
                AShieldRemaining = aShield,
                BHullRemaining = bHull,
                BShieldRemaining = bShield,
                DamageThisRound = damageThisRound,
                AHeat = aHeat,
                BHeat = bHeat,
            });

            // Check termination.
            bool aDead = aHull <= STRUCT_ZERO;
            bool bDead = bHull <= STRUCT_ZERO;

            if (aDead || bDead)
                break;
        }

        // Determine winner.
        bool aDeadFinal = aHull <= STRUCT_ZERO;
        bool bDeadFinal = bHull <= STRUCT_ZERO;

        Winner winner;
        if (aDeadFinal && bDeadFinal)
            winner = Winner.Draw;
        else if (bDeadFinal)
            winner = Winner.A;
        else if (aDeadFinal)
            winner = Winner.B;
        else
            winner = Winner.Draw; // max rounds reached, neither destroyed

        return new StrategicResult
        {
            Winner = winner,
            RoundsPlayed = roundsPlayed,
            FleetAHullRemaining = Math.Max(STRUCT_ZERO, aHull),
            FleetBHullRemaining = Math.Max(STRUCT_ZERO, bHull),
            SalvageValue = totalSalvage,
            Frames = frames,
        };
    }

    /// <summary>
    /// Fire every weapon in the list at the target, routing through zone armor.
    /// GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Uses CalcDamageWithZoneArmor + stance-based facing.
    /// Mutates targetShield, targetHull, and targetZoneArmor in place.
    /// Returns total damage dealt this salvo.
    /// </summary>
    // GATE.S7.COMBAT_PHASE2.SPIN_TURN.001: Compute turn penalty from spin RPM.
    // Returns basis points of damage reduction (0 = no penalty, 5000 = 50% penalty).
    // Spinal weapons are exempt (fire along axis). Standard weapons take full penalty.
    // Broadside weapons get fixed arc efficiency instead.
    private static int ComputeSpinTurnPenaltyBps(int spinRpm)
    {
        if (spinRpm <= STRUCT_ZERO) return STRUCT_ZERO;
        int penalty = spinRpm * CombatTweaksV0.TurnPenaltyBpsPerRpm;
        return Math.Min(penalty, CombatTweaksV0.MaxTurnPenaltyBps);
    }

    // GATE.S7.COMBAT_PHASE2.MOUNT_TYPE.001: Compute effective damage multiplier (bps) per mount type.
    // Standard: reduced by spin turn penalty. Broadside: fixed efficiency. Spinal: full damage.
    private static int ComputeMountEfficiencyBps(MountType mount, int spinTurnPenaltyBps)
    {
        return mount switch
        {
            MountType.Spinal => CombatTweaksV0.SpinalArcEfficiencyBps,
            MountType.Broadside => CombatTweaksV0.BroadsideArcEfficiencyBps,
            _ => 10000 - spinTurnPenaltyBps, // STRUCTURAL: 10000 bps = 100%
        };
    }

    // GATE.S7.COMBAT_PHASE2.SPIN_FIRE.001: Compute fire cadence (bps) per mount type.
    // Represents fraction of each rotation where the weapon has a firing solution.
    private static int ComputeFireCadenceBps(MountType mount)
    {
        return mount switch
        {
            MountType.Spinal => CombatTweaksV0.SpinalFireCadenceBps,
            MountType.Broadside => CombatTweaksV0.BroadsideFireCadenceBps,
            _ => CombatTweaksV0.StandardFireCadenceBps,
        };
    }

    // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Compute damage multiplier based on heat state.
    // Returns pct (0-100): 100 = full damage, 50 = overheat degradation, 0 = lockout.
    private static int ComputeHeatDamagePct(int heatCurrent, int heatCapacity)
    {
        if (heatCapacity <= STRUCT_ZERO) return CombatTweaksV0.NeutralPct;
        if (heatCurrent > heatCapacity * CombatTweaksV0.LockoutThresholdMultiplier)
            return STRUCT_ZERO; // Weapon lockout
        if (heatCurrent > heatCapacity)
            return CombatTweaksV0.OverheatDamagePct; // Degraded fire rate
        return CombatTweaksV0.NeutralPct; // Normal
    }

    private static int FireAllWeapons(
        List<CombatSystem.WeaponInfo> weapons,
        ref int targetShield,
        ref int targetHull,
        int[] targetZoneArmor,
        CombatSystem.CombatStance targetStance,
        bool targetEscorted,
        CombatSystem.TargetWeaponFamily targetWeaponFamilyForPd,
        int damagePct,
        ref int heat,
        int spinRpm = 0, // GATE.S7.COMBAT_PHASE2.SPIN_TURN.001
        int targetEvasionBps = 0, // GATE.S7.COMBAT_DEPTH2.TRACKING.001
        int round = 0, // GATE.S7.COMBAT_DEPTH2.DAMAGE_VAR.001
        string shooterId = "", // GATE.S7.COMBAT_DEPTH2.TRACKING.001
        int[]? shooterZoneArmor = null, // GATE.S7.COMBAT_DEPTH2.FORE_KILL.001
        int targetSpinRpm = 0) // GATE.T60.SPIN.ARMOR_HEAT.001
    {
        // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Lockout — no weapons fire.
        if (damagePct <= STRUCT_ZERO)
            return STRUCT_ZERO;

        // GATE.S7.COMBAT_PHASE2.SPIN_TURN.001: Pre-compute spin turn penalty for this salvo.
        int spinPenaltyBps = ComputeSpinTurnPenaltyBps(spinRpm);

        int salvoTotal = STRUCT_ZERO;
        int weaponCount = weapons.Count;

        for (int wi = 0; wi < weaponCount; wi++)
        {
            var weapon = weapons[wi];
            if (targetHull <= STRUCT_ZERO)
                break;

            // GATE.S7.COMBAT_DEPTH2.TRACKING.001: Hit check via FNV1a64.
            int hitBps = Math.Clamp(weapon.TrackingBps - targetEvasionBps,
                CombatDepthTweaksV0.MinHitBps, CombatDepthTweaksV0.MaxHitBps);
            ulong hitHash = CombatSystem.Fnv1a64Combat(round, wi, shooterId);
            int hitRoll = (int)(hitHash % 10000); // STRUCTURAL: 10000 bps = 100%
            if (hitRoll >= hitBps)
            {
                // Miss — weapon fires but fails to connect.
                heat += weapon.HeatPerShot;
                continue;
            }

            // GATE.S7.COMBAT_DEPTH2.FORE_KILL.001: Fore zone soft-kill — shooter's own fore zone.
            // If the shooter's fore zone armor is depleted, fore-mounted weapons go offline.
            if (shooterZoneArmor != null && shooterZoneArmor[(int)ZoneFacing.Fore] <= STRUCT_ZERO)
            {
                // Only fore-slot weapons affected (first ~50% of weapons for Charge stance ships).
                var shooterFacing = CombatSystem.PickFacing(CombatSystem.CombatStance.Charge, wi, weaponCount);
                if (shooterFacing == ZoneFacing.Fore)
                {
                    heat += weapon.HeatPerShot;
                    continue;
                }
            }

            var facing = CombatSystem.PickFacing(targetStance, wi, weaponCount);

            // GATE.S5.COMBAT.COUNTER_FAMILY.001: Apply PD counter bonus before zone routing.
            int effectiveBaseDmg = weapon.BaseDamage;
            if (weapon.Family == CombatSystem.DamageFamily.PointDefense &&
                (targetWeaponFamilyForPd == CombatSystem.TargetWeaponFamily.Missile ||
                 targetWeaponFamilyForPd == CombatSystem.TargetWeaponFamily.Torpedo))
            {
                effectiveBaseDmg = weapon.BaseDamage * CombatTweaksV0.PointDefenseCounterMultiplierPct / CombatTweaksV0.NeutralPct;
            }

            // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Apply overheat damage degradation.
            effectiveBaseDmg = effectiveBaseDmg * damagePct / CombatTweaksV0.NeutralPct;

            // GATE.S7.COMBAT_PHASE2.SPIN_TURN.001 + MOUNT_TYPE.001: Apply mount-type efficiency.
            int mountBps = ComputeMountEfficiencyBps(weapon.MountType, spinPenaltyBps);
            effectiveBaseDmg = (int)((long)effectiveBaseDmg * mountBps / 10000); // STRUCTURAL: 10000 = 100%

            // GATE.S7.COMBAT_PHASE2.SPIN_FIRE.001: Apply fire cadence (only when spinning).
            if (spinRpm > STRUCT_ZERO)
            {
                int cadenceBps = ComputeFireCadenceBps(weapon.MountType);
                effectiveBaseDmg = (int)((long)effectiveBaseDmg * cadenceBps / 10000); // STRUCTURAL: 10000 = 100%
            }

            // GATE.T60.SPIN.ARMOR_HEAT.001: Spin armor — energy weapons reduced by target spin.
            if (targetSpinRpm > STRUCT_ZERO && weapon.Family == CombatSystem.DamageFamily.Energy)
            {
                int reductionBps = Math.Min(
                    targetSpinRpm * CombatTweaksV0.SpinArmorEnergyReductionBpsPerRpm,
                    CombatTweaksV0.SpinArmorMaxReductionBps);
                effectiveBaseDmg = (int)((long)effectiveBaseDmg * (10000 - reductionBps) / 10000); // STRUCTURAL: 10000 = 100%
            }

            // GATE.S7.COMBAT_DEPTH2.DAMAGE_VAR.001: ±20% deterministic damage variance.
            ulong varHash = CombatSystem.Fnv1a64Combat(round + 7919, wi + 1301, shooterId); // STRUCTURAL: offset primes for independence from hit hash
            int varRoll = (int)(varHash % (uint)(CombatDepthTweaksV0.VarianceRangeBps * 2 + 1)); // STRUCTURAL: range [0, 2*range]
            int varOffset = varRoll - CombatDepthTweaksV0.VarianceRangeBps; // center around 0
            effectiveBaseDmg = (int)((long)effectiveBaseDmg * (10000 + varOffset) / 10000); // STRUCTURAL: 10000 bps = 100%
            if (effectiveBaseDmg < STRUCT_ZERO) effectiveBaseDmg = STRUCT_ZERO;

            // GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: facing already picked above.
            int zoneHp = targetZoneArmor[(int)facing];

            // GATE.S7.COMBAT_DEPTH2.ARMOR_PEN.001: Pass armor pen to zone damage calc.
            var dmg = CombatSystem.CalcDamageWithZoneArmor(
                effectiveBaseDmg,
                weapon.Family,
                targetShield,
                zoneHp,
                targetHull,
                facing,
                weapon.ArmorPenBps);

            // Apply escort shield damage reduction to the shield component only.
            int shieldDmg = dmg.ShieldDmg;
            if (targetEscorted && shieldDmg > STRUCT_ZERO)
                shieldDmg = CombatSystem.ApplyEscortShieldReduction(shieldDmg);

            int hullDmg = dmg.HullDmg;
            int zoneDmg = dmg.ZoneArmorDmg;

            targetShield = Math.Max(STRUCT_ZERO, targetShield - shieldDmg);
            targetZoneArmor[(int)facing] = Math.Max(STRUCT_ZERO, targetZoneArmor[(int)facing] - zoneDmg);
            targetHull = Math.Max(STRUCT_ZERO, targetHull - hullDmg);

            salvoTotal += shieldDmg + zoneDmg + hullDmg;

            // GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001: Accumulate heat per weapon fire.
            heat += weapon.HeatPerShot;
        }

        return salvoTotal;
    }

    /// <summary>
    /// Determine the dominant TargetWeaponFamily of a fleet's weapons (used by opposing PointDefense).
    /// If any weapon is Missile or Torpedo, return that family. Otherwise Other.
    /// </summary>
    private static CombatSystem.TargetWeaponFamily DetermineTargetFamily(
        List<CombatSystem.WeaponInfo> weapons)
    {
        foreach (var w in weapons)
        {
            var tf = CombatSystem.ClassifyTargetWeaponFamily(w.ModuleId);
            if (tf != CombatSystem.TargetWeaponFamily.Other)
                return tf;
        }
        return CombatSystem.TargetWeaponFamily.Other;
    }

    /// <summary>
    /// Serialize the full frame list to a single deterministic string suitable for SHA256 hashing.
    /// One line per frame, newline-delimited.
    /// </summary>
    public static string SerializeFrames(List<ReplayFrame> frames)
    {
        var sb = new StringBuilder();
        foreach (var f in frames)
        {
            sb.Append(f.Serialize());
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Compute SHA256 hex string over the deterministic serialized frames.
    /// GATE.S5.COMBAT.REPLAY_PROOF.001
    /// </summary>
    public static string ComputeFrameHash(List<ReplayFrame> frames)
    {
        string serialized = SerializeFrames(frames);
        byte[] bytes = Encoding.UTF8.GetBytes(serialized);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
