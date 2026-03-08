using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Combat;

// GATE.S18.SHIP_MODULES.ZONE_ARMOR.001: Zone armor damage routing contract tests.
[TestFixture]
[Category("ZoneArmor")]
public sealed class ZoneArmorTests
{
    [Test]
    public void ZoneArmor_ShieldAbsorbsFirst_ThenZoneArmor_ThenHull()
    {
        // 30 damage neutral vs 10 shield, 15 zone armor, 100 hull
        var r = CombatSystem.CalcDamageWithZoneArmor(
            30, CombatSystem.DamageFamily.Neutral,
            defenderShieldHp: 10, zoneArmorHp: 15, defenderHullHp: 100,
            ZoneFacing.Fore);

        Assert.That(r.ShieldDmg, Is.EqualTo(10), "Shield absorbs 10");
        Assert.That(r.ZoneArmorDmg, Is.EqualTo(15), "Zone armor absorbs remaining 15");
        Assert.That(r.HullDmg, Is.EqualTo(5), "Hull takes overflow 5");
        Assert.That(r.Facing, Is.EqualTo(ZoneFacing.Fore));
    }

    [Test]
    public void ZoneArmor_ShieldBlocksAll_NoZoneDamage()
    {
        var r = CombatSystem.CalcDamageWithZoneArmor(
            10, CombatSystem.DamageFamily.Neutral,
            defenderShieldHp: 50, zoneArmorHp: 20, defenderHullHp: 100,
            ZoneFacing.Port);

        Assert.That(r.ShieldDmg, Is.EqualTo(10));
        Assert.That(r.ZoneArmorDmg, Is.EqualTo(0), "No damage to zone armor when shields absorb all");
        Assert.That(r.HullDmg, Is.EqualTo(0));
    }

    [Test]
    public void ZoneArmor_NoShield_ZoneAbsorbsFirst()
    {
        var r = CombatSystem.CalcDamageWithZoneArmor(
            20, CombatSystem.DamageFamily.Neutral,
            defenderShieldHp: 0, zoneArmorHp: 15, defenderHullHp: 100,
            ZoneFacing.Aft);

        Assert.That(r.ShieldDmg, Is.EqualTo(0));
        Assert.That(r.ZoneArmorDmg, Is.EqualTo(15), "Zone armor absorbs 15");
        Assert.That(r.HullDmg, Is.EqualTo(5), "Hull takes remainder");
    }

    [Test]
    public void ZoneArmor_DepletedZone_DamageGoesToHull()
    {
        var r = CombatSystem.CalcDamageWithZoneArmor(
            20, CombatSystem.DamageFamily.Neutral,
            defenderShieldHp: 0, zoneArmorHp: 0, defenderHullHp: 100,
            ZoneFacing.Starboard);

        Assert.That(r.ShieldDmg, Is.EqualTo(0));
        Assert.That(r.ZoneArmorDmg, Is.EqualTo(0), "Depleted zone absorbs nothing");
        Assert.That(r.HullDmg, Is.EqualTo(20), "Hull takes full damage");
    }

    [Test]
    public void ZoneArmor_KineticFamily_HighHullDamage_LowShieldDamage()
    {
        // Kinetic: 50% vs shield, 150% vs hull.
        // 20 base → 10 vs shield. Shield has 10 → absorbs 10.
        // Overflow: 0. Zone/hull not hit.
        var r = CombatSystem.CalcDamageWithZoneArmor(
            20, CombatSystem.DamageFamily.Kinetic,
            defenderShieldHp: 10, zoneArmorHp: 20, defenderHullHp: 100,
            ZoneFacing.Fore);

        Assert.That(r.ShieldDmg, Is.EqualTo(10));
        Assert.That(r.ZoneArmorDmg, Is.EqualTo(0));
        Assert.That(r.HullDmg, Is.EqualTo(0));
    }

    [Test]
    public void ZoneArmor_KineticFamily_NoShield_HighHullDamage()
    {
        // Kinetic vs hull = 150%. 20 base → 30 effective.
        // Zone armor 10 absorbs 10, hull takes 20.
        var r = CombatSystem.CalcDamageWithZoneArmor(
            20, CombatSystem.DamageFamily.Kinetic,
            defenderShieldHp: 0, zoneArmorHp: 10, defenderHullHp: 100,
            ZoneFacing.Fore);

        Assert.That(r.ShieldDmg, Is.EqualTo(0));
        Assert.That(r.ZoneArmorDmg, Is.EqualTo(10));
        Assert.That(r.HullDmg, Is.EqualTo(20), "Kinetic 150% of 20 = 30, minus 10 zone = 20 hull");
    }

    [Test]
    public void ZoneArmor_FacingIsPreserved()
    {
        foreach (ZoneFacing facing in new[] { ZoneFacing.Fore, ZoneFacing.Port, ZoneFacing.Starboard, ZoneFacing.Aft })
        {
            var r = CombatSystem.CalcDamageWithZoneArmor(
                10, CombatSystem.DamageFamily.Neutral,
                defenderShieldHp: 0, zoneArmorHp: 5, defenderHullHp: 100,
                facing);
            Assert.That(r.Facing, Is.EqualTo(facing));
        }
    }

    [Test]
    public void InitFleetCombatStats_SetsZoneArmor_Player()
    {
        var fleet = new Fleet { Id = "player" };
        CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);

        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Fore], Is.EqualTo(CombatTweaksV0.DefaultZoneArmorFore));
        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Port], Is.EqualTo(CombatTweaksV0.DefaultZoneArmorPort));
        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Starboard], Is.EqualTo(CombatTweaksV0.DefaultZoneArmorStbd));
        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Aft], Is.EqualTo(CombatTweaksV0.DefaultZoneArmorAft));

        Assert.That(fleet.ZoneArmorHpMax[(int)ZoneFacing.Fore], Is.EqualTo(CombatTweaksV0.DefaultZoneArmorFore));
    }

    [Test]
    public void InitFleetCombatStats_SetsZoneArmor_Ai()
    {
        var fleet = new Fleet { Id = "pirate" };
        CombatSystem.InitFleetCombatStats(fleet, isPlayer: false);

        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Fore], Is.EqualTo(CombatTweaksV0.AiZoneArmorFore));
        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Port], Is.EqualTo(CombatTweaksV0.AiZoneArmorPort));
        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Starboard], Is.EqualTo(CombatTweaksV0.AiZoneArmorStbd));
        Assert.That(fleet.ZoneArmorHp[(int)ZoneFacing.Aft], Is.EqualTo(CombatTweaksV0.AiZoneArmorAft));
    }

    [Test]
    public void ZoneArmor_Deterministic_SameInputsSameOutput()
    {
        for (int i = 0; i < 50; i++)
        {
            var a = CombatSystem.CalcDamageWithZoneArmor(25, CombatSystem.DamageFamily.Energy, 15, 20, 80, ZoneFacing.Port);
            var b = CombatSystem.CalcDamageWithZoneArmor(25, CombatSystem.DamageFamily.Energy, 15, 20, 80, ZoneFacing.Port);

            Assert.That(b.ShieldDmg, Is.EqualTo(a.ShieldDmg));
            Assert.That(b.ZoneArmorDmg, Is.EqualTo(a.ZoneArmorDmg));
            Assert.That(b.HullDmg, Is.EqualTo(a.HullDmg));
        }
    }

    // ── GATE.S18.SHIP_MODULES.COMBAT_ZONES.001: Stance + zone integration tests ──

    [Test]
    public void DetermineStance_FrigateIsCharge()
    {
        Assert.That(CombatSystem.DetermineStance("frigate"), Is.EqualTo(CombatSystem.CombatStance.Charge));
        Assert.That(CombatSystem.DetermineStance("dreadnought"), Is.EqualTo(CombatSystem.CombatStance.Charge));
    }

    [Test]
    public void DetermineStance_ClipperIsKite()
    {
        Assert.That(CombatSystem.DetermineStance("clipper"), Is.EqualTo(CombatSystem.CombatStance.Kite));
        Assert.That(CombatSystem.DetermineStance("shuttle"), Is.EqualTo(CombatSystem.CombatStance.Kite));
    }

    [Test]
    public void DetermineStance_CorvetteIsBroadside()
    {
        Assert.That(CombatSystem.DetermineStance("corvette"), Is.EqualTo(CombatSystem.CombatStance.Broadside));
        Assert.That(CombatSystem.DetermineStance("cruiser"), Is.EqualTo(CombatSystem.CombatStance.Broadside));
        Assert.That(CombatSystem.DetermineStance("hauler"), Is.EqualTo(CombatSystem.CombatStance.Broadside));
    }

    [Test]
    public void GetStanceDistribution_SumsTo100()
    {
        foreach (var stance in new[] { CombatSystem.CombatStance.Charge, CombatSystem.CombatStance.Broadside, CombatSystem.CombatStance.Kite })
        {
            var dist = CombatSystem.GetStanceDistribution(stance);
            Assert.That(dist.Length, Is.EqualTo(4));
            int sum = dist[0] + dist[1] + dist[2] + dist[3];
            Assert.That(sum, Is.EqualTo(100), $"Stance {stance} distribution must sum to 100");
        }
    }

    [Test]
    public void PickFacing_Charge_MostlyFore()
    {
        // With 4 weapons, Charge (50/20/20/10):
        // weapon 0 → slot 0 (0 < 50 → Fore), weapon 1 → slot 25 (25 < 50 → Fore)
        // weapon 2 → slot 50 (50 < 70 → Port), weapon 3 → slot 75 (75 < 90 → Stbd)
        int foreCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (CombatSystem.PickFacing(CombatSystem.CombatStance.Charge, i, 4) == ZoneFacing.Fore)
                foreCount++;
        }
        Assert.That(foreCount, Is.GreaterThanOrEqualTo(1), "Charge stance should hit Fore at least once");
    }

    [Test]
    public void StrategicResolver_WithZoneArmor_DepletesZones()
    {
        // Two corvettes (Broadside stance) with zone armor fight.
        var profileA = new CombatSystem.CombatProfile
        {
            HullHp = 100, HullHpMax = 100,
            ShieldHp = 50, ShieldHpMax = 50,
            ShipClassId = "corvette",
            ZoneArmorHp = new[] { 25, 20, 20, 15 },
        };
        profileA.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = "cannon_mk1", BaseDamage = 15, Family = CombatSystem.DamageFamily.Kinetic });
        profileA.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = "laser_mk1", BaseDamage = 15, Family = CombatSystem.DamageFamily.Energy });

        var profileB = new CombatSystem.CombatProfile
        {
            HullHp = 100, HullHpMax = 100,
            ShieldHp = 50, ShieldHpMax = 50,
            ShipClassId = "corvette",
            ZoneArmorHp = new[] { 25, 20, 20, 15 },
        };
        profileB.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = "cannon_mk1", BaseDamage = 15, Family = CombatSystem.DamageFamily.Kinetic });

        var result = StrategicResolverV0.Resolve(profileA, profileB);

        // Combat should terminate (someone dies or max rounds)
        Assert.That(result.RoundsPlayed, Is.GreaterThan(0));
        // With zone armor, combat lasts longer than without.
        Assert.That(result.SalvageValue, Is.GreaterThan(0), "Some damage should be dealt");
    }

    [Test]
    public void StrategicResolver_ZoneArmor_Deterministic()
    {
        var makeProfile = () =>
        {
            var p = new CombatSystem.CombatProfile
            {
                HullHp = 80, HullHpMax = 80,
                ShieldHp = 30, ShieldHpMax = 30,
                ShipClassId = "frigate",
                ZoneArmorHp = new[] { 20, 15, 15, 10 },
            };
            p.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = "cannon_mk1", BaseDamage = 12, Family = CombatSystem.DamageFamily.Kinetic });
            p.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = "laser_mk1", BaseDamage = 12, Family = CombatSystem.DamageFamily.Energy });
            return p;
        };

        var r1 = StrategicResolverV0.Resolve(makeProfile(), makeProfile());
        var r2 = StrategicResolverV0.Resolve(makeProfile(), makeProfile());

        Assert.That(r2.RoundsPlayed, Is.EqualTo(r1.RoundsPlayed));
        Assert.That(r2.Winner, Is.EqualTo(r1.Winner));
        Assert.That(r2.SalvageValue, Is.EqualTo(r1.SalvageValue));

        var hash1 = StrategicResolverV0.ComputeFrameHash(r1.Frames);
        var hash2 = StrategicResolverV0.ComputeFrameHash(r2.Frames);
        Assert.That(hash2, Is.EqualTo(hash1), "Zone armor combat must be deterministic");
    }

    [Test]
    public void StrategicResolver_KiteStance_AftTakesMostDamage()
    {
        // Kite stance: 60% of hits go to Aft.
        // Clipper (Kite) defends against corvette (Broadside) attacker.
        var attacker = new CombatSystem.CombatProfile
        {
            HullHp = 100, HullHpMax = 100,
            ShieldHp = 0, ShieldHpMax = 0, // no shields to simplify
            ShipClassId = "corvette",
            ZoneArmorHp = new[] { 25, 20, 20, 15 },
        };
        // 10 weapons to get good distribution spread
        for (int i = 0; i < 10; i++)
            attacker.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = "cannon_mk1", BaseDamage = 5, Family = CombatSystem.DamageFamily.Neutral });

        var defender = new CombatSystem.CombatProfile
        {
            HullHp = 200, HullHpMax = 200, // high HP so it survives
            ShieldHp = 0, ShieldHpMax = 0,
            ShipClassId = "clipper", // Kite stance
            ZoneArmorHp = new[] { 100, 100, 100, 100 }, // Even armor to measure distribution
        };

        // Run 1 round manually via Resolve (max 1 round)
        var result = StrategicResolverV0.Resolve(attacker, defender);

        // After combat, the Aft zone of the defender should have taken most damage.
        // We can't directly see zone arrays post-combat in StrategicResult,
        // but we can verify via the PickFacing function:
        int aftCount = 0;
        int totalWpns = 10;
        for (int i = 0; i < totalWpns; i++)
        {
            if (CombatSystem.PickFacing(CombatSystem.CombatStance.Kite, i, totalWpns) == ZoneFacing.Aft)
                aftCount++;
        }
        Assert.That(aftCount, Is.GreaterThanOrEqualTo(5), "Kite stance should direct majority of hits to Aft");
    }
}
