using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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

        /// <summary>
        /// Deterministic pipe-delimited serialization (no floats, no culture-dependent output).
        /// Format: "round|ahull|ashield|bhull|bshield|damage"
        /// </summary>
        public string Serialize() =>
            $"{Round}|{AHullRemaining}|{AShieldRemaining}|{BHullRemaining}|{BShieldRemaining}|{DamageThisRound}";
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
    /// </summary>
    public static StrategicResult Resolve(
        CombatSystem.CombatProfile fleetA,
        CombatSystem.CombatProfile fleetB,
        bool fleetAEscorted = false,
        bool fleetBEscorted = false)
    {
        // Work on mutable copies so caller's profiles are unchanged.
        int aHull = fleetA.HullHp;
        int aShield = fleetA.ShieldHp;
        int bHull = fleetB.HullHp;
        int bShield = fleetB.ShieldHp;

        int totalSalvage = STRUCT_ZERO;
        int roundsPlayed = STRUCT_ZERO;

        var frames = new List<ReplayFrame>();

        for (int round = STRUCT_FIRST_ROUND; round <= CombatTweaksV0.StrategicMaxRounds; round++)
        {
            int damageThisRound = STRUCT_ZERO;

            // ── Fleet A fires all weapons at Fleet B ──
            damageThisRound += FireAllWeapons(
                fleetA.Weapons,
                ref bShield,
                ref bHull,
                targetEscorted: fleetBEscorted,
                targetWeaponFamilyForPd: DetermineTargetFamily(fleetB.Weapons));

            // ── Fleet B fires back (only if still alive) ──
            if (bHull > STRUCT_ZERO)
            {
                damageThisRound += FireAllWeapons(
                    fleetB.Weapons,
                    ref aShield,
                    ref aHull,
                    targetEscorted: fleetAEscorted,
                    targetWeaponFamilyForPd: DetermineTargetFamily(fleetA.Weapons));
            }

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
    /// Fire every weapon in the list at the target, applying escort shield reduction if active.
    /// Mutates targetShield and targetHull in place.
    /// Returns total damage dealt this salvo.
    /// </summary>
    private static int FireAllWeapons(
        List<CombatSystem.WeaponInfo> weapons,
        ref int targetShield,
        ref int targetHull,
        bool targetEscorted,
        CombatSystem.TargetWeaponFamily targetWeaponFamilyForPd)
    {
        int salvoTotal = STRUCT_ZERO;

        foreach (var weapon in weapons)
        {
            if (targetHull <= STRUCT_ZERO)
                break;

            var dmg = CombatSystem.CalcDamage(
                weapon.BaseDamage,
                weapon.Family,
                targetShield,
                targetHull,
                targetWeaponFamilyForPd);

            // Apply escort shield damage reduction to the shield component only.
            int shieldDmg = dmg.ShieldDmg;
            if (targetEscorted && shieldDmg > STRUCT_ZERO)
                shieldDmg = CombatSystem.ApplyEscortShieldReduction(shieldDmg);

            int hullDmg = dmg.HullDmg;

            targetShield = Math.Max(STRUCT_ZERO, targetShield - shieldDmg);
            targetHull = Math.Max(STRUCT_ZERO, targetHull - hullDmg);

            salvoTotal += shieldDmg + hullDmg;
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
