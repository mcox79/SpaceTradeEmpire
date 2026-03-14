using System;
using System.Collections.Generic;
using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.S7.COMBAT_DEPTH2.TRACKING.001: Hit probability via TrackingBps/EvasionBps.
/// GATE.S7.COMBAT_DEPTH2.DAMAGE_VAR.001: ±20% deterministic damage variance.
/// GATE.S7.COMBAT_DEPTH2.ARMOR_PEN.001: ArmorPenetrationPct zone bypass.
/// GATE.S7.COMBAT_DEPTH2.FORE_KILL.001: Fore zone soft-kill.
/// </summary>
[TestFixture]
public class CombatDepthTests
{
    // ── Tracking/Evasion ──

    [Test]
    public void TrackingBps_DefaultsPopulatedOnBuildProfile()
    {
        var fleet = MakeFleet("corvette", weaponId: "test_cannon_mk1");
        CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
        var profile = CombatSystem.BuildProfile(fleet);

        Assert.That(profile.EvasionBps, Is.EqualTo(CombatDepthTweaksV0.CorvetteEvasionBps));
        Assert.That(profile.Weapons, Has.Count.EqualTo(1));
        Assert.That(profile.Weapons[0].TrackingBps, Is.EqualTo(CombatDepthTweaksV0.CannonTrackingBps));
    }

    [Test]
    public void ShipClassEvasion_LightShipsMoreEvasive()
    {
        Assert.That(CombatSystem.GetShipClassEvasionBps("shuttle"),
            Is.GreaterThan(CombatSystem.GetShipClassEvasionBps("dreadnought")));
    }

    [Test]
    public void HitCheck_HighTrackingVsLowEvasion_MostlyHits()
    {
        // 9500 tracking vs 800 evasion = 8700 bps = 87% hit
        int hits = 0;
        for (int round = 0; round < 100; round++)
        {
            ulong hash = CombatSystem.Fnv1a64Combat(round, 0, "testFleet");
            int roll = (int)(hash % 10000);
            int hitBps = Math.Clamp(CombatDepthTweaksV0.LaserTrackingBps - CombatDepthTweaksV0.DreadnoughtEvasionBps,
                CombatDepthTweaksV0.MinHitBps, CombatDepthTweaksV0.MaxHitBps);
            if (roll < hitBps) hits++;
        }
        Assert.That(hits, Is.GreaterThan(50), $"Expected >50 hits with 87% chance, got {hits}");
    }

    [Test]
    public void HitCheck_LowTrackingVsHighEvasion_MissesMore()
    {
        // 5000 tracking vs 4000 evasion = 1000 net → clamped to 2000 bps (min) = 20% hit
        int hits = 0;
        for (int round = 0; round < 100; round++)
        {
            ulong hash = CombatSystem.Fnv1a64Combat(round, 0, "slowGunner");
            int roll = (int)(hash % 10000);
            int hitBps = Math.Clamp(CombatDepthTweaksV0.TorpedoTrackingBps - CombatDepthTweaksV0.ShuttleEvasionBps,
                CombatDepthTweaksV0.MinHitBps, CombatDepthTweaksV0.MaxHitBps);
            if (roll < hitBps) hits++;
        }
        Assert.That(hits, Is.LessThan(70), $"Expected <70 hits with 20% chance, got {hits}");
    }

    // ── Damage Variance ──

    [Test]
    public void DamageVariance_ProducesRange()
    {
        var dmgValues = new HashSet<int>();
        for (int round = 0; round < 200; round++)
        {
            ulong varHash = CombatSystem.Fnv1a64Combat(round + 7919, 1301, "varTest");
            int varRoll = (int)(varHash % (uint)(CombatDepthTweaksV0.VarianceRangeBps * 2 + 1));
            int varOffset = varRoll - CombatDepthTweaksV0.VarianceRangeBps;
            int dmg = (int)((long)100 * (10000 + varOffset) / 10000);
            dmgValues.Add(dmg);
        }
        Assert.That(dmgValues.Count, Is.GreaterThan(5), $"Expected damage variance, got only {dmgValues.Count} distinct values");
    }

    // ── Armor Penetration ──

    [Test]
    public void ArmorPen_BypassesSomeZoneArmor()
    {
        var noPen = CombatSystem.CalcDamageWithZoneArmor(10, CombatSystem.DamageFamily.Neutral,
            0, 100, 100, ZoneFacing.Fore, armorPenBps: 0);
        var withPen = CombatSystem.CalcDamageWithZoneArmor(10, CombatSystem.DamageFamily.Neutral,
            0, 100, 100, ZoneFacing.Fore, armorPenBps: 2000);

        Assert.That(noPen.ZoneArmorDmg, Is.EqualTo(10));
        Assert.That(noPen.HullDmg, Is.EqualTo(0));
        Assert.That(withPen.HullDmg, Is.GreaterThan(0), "Expected hull damage from armor pen");
        Assert.That(withPen.ZoneArmorDmg, Is.LessThan(10), "Expected less zone armor damage with pen");
        Assert.That(withPen.ZoneArmorDmg + withPen.HullDmg, Is.EqualTo(10));
    }

    [Test]
    public void ArmorPen_SpinalMount_HighPenetration()
    {
        int penBps = CombatSystem.GetWeaponArmorPenBps(CombatSystem.DamageFamily.Kinetic, MountType.Spinal);
        Assert.That(penBps, Is.EqualTo(CombatDepthTweaksV0.SpinalArmorPenBps));
        Assert.That(penBps, Is.GreaterThan(CombatDepthTweaksV0.CannonArmorPenBps));
    }

    [Test]
    public void ArmorPen_ZeroArmorPen_BackwardCompatible()
    {
        var old = CombatSystem.CalcDamageWithZoneArmor(15, CombatSystem.DamageFamily.Kinetic,
            10, 20, 50, ZoneFacing.Port);
        var newResult = CombatSystem.CalcDamageWithZoneArmor(15, CombatSystem.DamageFamily.Kinetic,
            10, 20, 50, ZoneFacing.Port, armorPenBps: 0);

        Assert.That(newResult.ShieldDmg, Is.EqualTo(old.ShieldDmg));
        Assert.That(newResult.ZoneArmorDmg, Is.EqualTo(old.ZoneArmorDmg));
        Assert.That(newResult.HullDmg, Is.EqualTo(old.HullDmg));
    }

    // ── Fore Zone Soft-Kill ──

    [Test]
    public void ForeKill_StrategicResolver_ReducesDamageWhenForeZoneDepleted()
    {
        var attacker = MakeFleet("corvette", weaponId: "test_cannon_mk1");
        CombatSystem.InitFleetCombatStats(attacker, isPlayer: true);
        var profileA = CombatSystem.BuildProfile(attacker);

        var defender = MakeFleet("frigate", weaponId: "test_cannon_mk1");
        CombatSystem.InitFleetCombatStats(defender, isPlayer: false);

        var profileB_full = CombatSystem.BuildProfile(defender);
        var result_full = StrategicResolverV0.Resolve(profileA, profileB_full);

        defender.ZoneArmorHp[(int)ZoneFacing.Fore] = 0;
        var profileB_depleted = CombatSystem.BuildProfile(defender);
        var result_depleted = StrategicResolverV0.Resolve(profileA, profileB_depleted);

        Assert.That(result_depleted.RoundsPlayed, Is.GreaterThan(0));
    }

    // ── Full Strategic Encounter ──

    [Test]
    public void StrategicResolver_WithCombatDepth_Deterministic()
    {
        var fleetA = MakeFleet("corvette", weaponId: "test_laser_mk1");
        CombatSystem.InitFleetCombatStats(fleetA, isPlayer: true);
        var profileA = CombatSystem.BuildProfile(fleetA);

        var fleetB = MakeFleet("frigate", weaponId: "test_cannon_mk1");
        CombatSystem.InitFleetCombatStats(fleetB, isPlayer: false);
        var profileB = CombatSystem.BuildProfile(fleetB);

        var result1 = StrategicResolverV0.Resolve(profileA, profileB);
        var hash1 = StrategicResolverV0.ComputeFrameHash(result1.Frames);

        CombatSystem.InitFleetCombatStats(fleetA, isPlayer: true);
        CombatSystem.InitFleetCombatStats(fleetB, isPlayer: false);
        profileA = CombatSystem.BuildProfile(fleetA);
        profileB = CombatSystem.BuildProfile(fleetB);

        var result2 = StrategicResolverV0.Resolve(profileA, profileB);
        var hash2 = StrategicResolverV0.ComputeFrameHash(result2.Frames);

        Assert.That(hash2, Is.EqualTo(hash1));
        Assert.That(result2.RoundsPlayed, Is.EqualTo(result1.RoundsPlayed));
        Assert.That(result2.Winner, Is.EqualTo(result1.Winner));
    }

    [Test]
    public void StrategicResolver_MissesCreateHitVariance()
    {
        var fastShip = MakeFleet("shuttle", weaponId: "test_laser_mk1");
        CombatSystem.InitFleetCombatStats(fastShip, isPlayer: true);
        var fastProfile = CombatSystem.BuildProfile(fastShip);

        var slowShip = MakeFleet("dreadnought", weaponId: "test_cannon_mk1");
        CombatSystem.InitFleetCombatStats(slowShip, isPlayer: false);
        var slowProfile = CombatSystem.BuildProfile(slowShip);

        Assert.That(fastProfile.EvasionBps, Is.GreaterThan(slowProfile.EvasionBps));
    }

    // ── Helper ──

    private static Fleet MakeFleet(string shipClass, string weaponId)
    {
        return new Fleet
        {
            Id = $"test_{shipClass}",
            OwnerId = "player",
            ShipClassId = shipClass,
            Slots = new List<ModuleSlot>
            {
                new ModuleSlot
                {
                    SlotId = "w1",
                    SlotKind = SlotKind.Weapon,
                    InstalledModuleId = weaponId,
                },
            },
        };
    }
}
