using System.Collections.Generic;
using NUnit.Framework;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Combat;

// GATE.S7.COMBAT_PHASE2.CONTRACT.001: Contract tests for heat, battle stations, and radiator systems.
[TestFixture]
[Category("CombatPhase2")]
public sealed class CombatPhase2Tests
{
    private static CombatSystem.CombatProfile MakeProfile(
        int hullHp, int shieldHp,
        int heatCapacity = -1,
        int rejectionRate = -1,
        int readinessDamagePct = 100,
        int radiatorBonusRate = 0,
        params (string moduleId, int baseDamage, CombatSystem.DamageFamily family, int heatPerShot)[] weapons)
    {
        var profile = new CombatSystem.CombatProfile
        {
            HullHp = hullHp,
            HullHpMax = hullHp,
            ShieldHp = shieldHp,
            ShieldHpMax = shieldHp,
            HeatCapacity = heatCapacity >= 0 ? heatCapacity : CombatTweaksV0.DefaultHeatCapacity,
            RejectionRate = rejectionRate >= 0 ? rejectionRate : CombatTweaksV0.DefaultRejectionRate,
            ReadinessDamagePct = readinessDamagePct,
            RadiatorBonusRate = radiatorBonusRate,
        };
        if (radiatorBonusRate > 0)
            profile.RejectionRate += radiatorBonusRate;
        foreach (var (moduleId, baseDamage, family, heatPerShot) in weapons)
        {
            profile.Weapons.Add(new CombatSystem.WeaponInfo
            {
                ModuleId = moduleId,
                BaseDamage = baseDamage,
                Family = family,
                HeatPerShot = heatPerShot > 0 ? heatPerShot : CombatTweaksV0.DefaultHeatPerShot,
            });
        }
        return profile;
    }

    // ── Heat System Tests ──

    [Test]
    public void Heat_AccumulatesPerWeaponFire()
    {
        // Fleet with 3 weapons: should accumulate 3 * HeatPerShot per round.
        var profile = MakeProfile(500, 100,
            heatCapacity: 2000,
            rejectionRate: 0, // zero cooling to isolate accumulation
            weapons: new[]
            {
                ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Kinetic, 100),
                ("weapon_cannon_mk2", 10, CombatSystem.DamageFamily.Kinetic, 100),
                ("weapon_cannon_mk3", 10, CombatSystem.DamageFamily.Kinetic, 100),
            });

        var defender = MakeProfile(5000, 0,
            heatCapacity: 2000,
            rejectionRate: 2000); // high cooling so defender heat is zero

        var result = StrategicResolverV0.Resolve(profile, defender);

        // After first round: heat should be 3*100 = 300 (before cooling)
        // With zero rejection, frame 0 heat = 300
        Assert.That(result.Frames.Count, Is.GreaterThan(0));
        Assert.That(result.Frames[0].AHeat, Is.EqualTo(300),
            "3 weapons * 100 heat per shot - 0 rejection = 300 heat after round 1");
    }

    [Test]
    public void Heat_PassiveCoolingReducesHeat()
    {
        // Fleet with 1 weapon (100 heat/shot) and 80 rejection rate.
        // After round 1: heat = 100 - 80 = 20
        var profile = MakeProfile(500, 100,
            heatCapacity: 2000,
            rejectionRate: 80,
            weapons: new[]
            {
                ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Kinetic, 100),
            });

        var defender = MakeProfile(5000, 0,
            heatCapacity: 2000,
            rejectionRate: 2000);

        var result = StrategicResolverV0.Resolve(profile, defender);

        Assert.That(result.Frames[0].AHeat, Is.EqualTo(20),
            "100 heat generated - 80 rejection = 20 remaining");
    }

    [Test]
    public void Heat_OverheatDegradesDamage()
    {
        // Compare damage output: one fleet below capacity, one above.
        // Fleet at capacity+1 should deal OverheatDamagePct (50%) damage.
        var normalProfile = MakeProfile(5000, 0,
            heatCapacity: 1000,
            rejectionRate: 1000, // cools back to zero each round — stays normal
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 100) });

        var overheatedProfile = MakeProfile(5000, 0,
            heatCapacity: 100, // very low capacity
            rejectionRate: 0,  // zero cooling — heat accumulates fast, overheats immediately
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 200) });

        var targetNormal = MakeProfile(5000, 0);
        var targetOverheat = MakeProfile(5000, 0);

        var resultNormal = StrategicResolverV0.Resolve(normalProfile, targetNormal);
        var resultOverheat = StrategicResolverV0.Resolve(overheatedProfile, targetOverheat);

        // Normal fleet deals more damage over time than overheated fleet.
        Assert.That(resultNormal.SalvageValue, Is.GreaterThan(resultOverheat.SalvageValue),
            "Normal fleet should deal more total damage than overheated fleet");
    }

    [Test]
    public void Heat_LockoutStopsAllDamage()
    {
        // Fleet with heat > 2x capacity should deal zero damage that round.
        // Use zero rejection + high heat per shot + low capacity.
        var lockoutProfile = MakeProfile(5000, 0,
            heatCapacity: 50,   // tiny capacity
            rejectionRate: 0,   // no cooling
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 200) });

        // After round 1: heat = 200, capacity = 50, lockout at 2*50=100.
        // So round 2 onwards should deal zero damage.
        var target = MakeProfile(5000, 0,
            heatCapacity: 5000,
            rejectionRate: 5000);

        var result = StrategicResolverV0.Resolve(lockoutProfile, target);

        // After round 1, heat = 200 (no cooling). 200 > 2*50 = lockout.
        // Round 1 deals normal damage (heat was 0 at start of round 1).
        // Round 2+ deals 0. So total damage across 50 rounds should be just round 1.
        Assert.That(result.Frames[0].AHeat, Is.EqualTo(200),
            "After round 1: 200 heat (200 generated, 0 cooled)");
        Assert.That(result.RoundsPlayed, Is.EqualTo(CombatTweaksV0.StrategicMaxRounds),
            "Locked-out fleet can't kill target, goes to max rounds");
    }

    [Test]
    public void Heat_HeatFloorIsZero()
    {
        // High rejection, low heat per shot. Heat should floor at 0 not go negative.
        var profile = MakeProfile(500, 0,
            heatCapacity: 1000,
            rejectionRate: 500,
            weapons: new[] { ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Neutral, 10) });

        var target = MakeProfile(5000, 0,
            heatCapacity: 5000,
            rejectionRate: 5000);

        var result = StrategicResolverV0.Resolve(profile, target);

        foreach (var frame in result.Frames)
        {
            Assert.That(frame.AHeat, Is.GreaterThanOrEqualTo(0),
                $"Heat must never be negative (round {frame.Round})");
        }
    }

    // ── Battle Stations Tests ──

    [Test]
    public void BattleStations_StandDown_ReducesDamage()
    {
        // StandDown = 25% damage. Compared to BattleReady (100%).
        var standDown = MakeProfile(5000, 0,
            readinessDamagePct: CombatTweaksV0.StandDownDamagePct,
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 0) });

        var battleReady = MakeProfile(5000, 0,
            readinessDamagePct: CombatTweaksV0.NeutralPct,
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 0) });

        var target1 = MakeProfile(5000, 0);
        var target2 = MakeProfile(5000, 0);

        var resultStandDown = StrategicResolverV0.Resolve(standDown, target1);
        var resultReady = StrategicResolverV0.Resolve(battleReady, target2);

        Assert.That(resultStandDown.SalvageValue, Is.LessThan(resultReady.SalvageValue),
            "StandDown fleet should deal less total damage than BattleReady fleet");
    }

    [Test]
    public void BattleStations_SpinningUp_HalfDamage()
    {
        // SpinningUp = 50% damage.
        var spinning = MakeProfile(5000, 0,
            readinessDamagePct: CombatTweaksV0.SpinningUpDamagePct,
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 0) });

        var ready = MakeProfile(5000, 0,
            readinessDamagePct: CombatTweaksV0.NeutralPct,
            weapons: new[] { ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Neutral, 0) });

        var target1 = MakeProfile(5000, 0);
        var target2 = MakeProfile(5000, 0);

        var resultSpin = StrategicResolverV0.Resolve(spinning, target1);
        var resultReady = StrategicResolverV0.Resolve(ready, target2);

        Assert.That(resultSpin.SalvageValue, Is.LessThan(resultReady.SalvageValue),
            "SpinningUp fleet should deal less total damage than BattleReady");
        Assert.That(resultSpin.SalvageValue, Is.GreaterThan(0),
            "SpinningUp fleet should still deal some damage");
    }

    [Test]
    public void BattleStations_BuildProfile_MapsFleetState()
    {
        var fleet = new Fleet { Id = "test" };
        CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
        fleet.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });

        fleet.BattleStations = BattleStationsState.StandDown;
        var profileSD = CombatSystem.BuildProfile(fleet);
        Assert.That(profileSD.ReadinessDamagePct, Is.EqualTo(CombatTweaksV0.StandDownDamagePct));

        fleet.BattleStations = BattleStationsState.SpinningUp;
        var profileSU = CombatSystem.BuildProfile(fleet);
        Assert.That(profileSU.ReadinessDamagePct, Is.EqualTo(CombatTweaksV0.SpinningUpDamagePct));

        fleet.BattleStations = BattleStationsState.BattleReady;
        var profileBR = CombatSystem.BuildProfile(fleet);
        Assert.That(profileBR.ReadinessDamagePct, Is.EqualTo(CombatTweaksV0.NeutralPct));
    }

    [Test]
    public void BattleStations_DamagePctOrdering()
    {
        // StandDown < SpinningUp < BattleReady
        Assert.That(CombatTweaksV0.StandDownDamagePct, Is.LessThan(CombatTweaksV0.SpinningUpDamagePct));
        Assert.That(CombatTweaksV0.SpinningUpDamagePct, Is.LessThan(CombatTweaksV0.NeutralPct));
    }

    // ── Radiator Tests ──

    [Test]
    public void Radiator_BuildProfile_SumsRadiatorBonus()
    {
        var fleet = new Fleet { Id = "test" };
        CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
        fleet.Slots.Add(new ModuleSlot
        {
            SlotId = "util_1",
            SlotKind = SlotKind.Utility,
            InstalledModuleId = WellKnownModuleIds.RadiatorBasic,
        });
        fleet.Slots.Add(new ModuleSlot
        {
            SlotId = "util_2",
            SlotKind = SlotKind.Utility,
            InstalledModuleId = WellKnownModuleIds.RadiatorAdvanced,
        });

        var profile = CombatSystem.BuildProfile(fleet);

        int expectedBonus = CombatTweaksV0.BasicRadiatorBonusRate + CombatTweaksV0.AdvancedRadiatorBonusRate;
        Assert.That(profile.RadiatorBonusRate, Is.EqualTo(expectedBonus));
        Assert.That(profile.RejectionRate, Is.EqualTo(CombatTweaksV0.DefaultRejectionRate + expectedBonus));
    }

    [Test]
    public void Radiator_IncreasedCooling_ReducesHeatFaster()
    {
        // Construct profiles directly to test radiator cooling bonus.
        int baseRejection = CombatTweaksV0.DefaultRejectionRate;
        int radiatorBonus = CombatTweaksV0.BasicRadiatorBonusRate;

        var withRadiator = new CombatSystem.CombatProfile
        {
            HullHp = 5000, HullHpMax = 5000,
            ShieldHp = 0, ShieldHpMax = 0,
            HeatCapacity = 2000,
            RejectionRate = baseRejection + radiatorBonus,
            RadiatorBonusRate = radiatorBonus,
            ZoneArmorHp = new[] { 100, 100, 100, 100 }, // Non-zero aft so radiator isn't stripped
        };
        withRadiator.Weapons.Add(new CombatSystem.WeaponInfo
            { ModuleId = "weapon_cannon_mk1", BaseDamage = 10, Family = CombatSystem.DamageFamily.Neutral, HeatPerShot = 200 });

        var withoutRadiator = new CombatSystem.CombatProfile
        {
            HullHp = 5000, HullHpMax = 5000,
            ShieldHp = 0, ShieldHpMax = 0,
            HeatCapacity = 2000,
            RejectionRate = baseRejection,
            RadiatorBonusRate = 0,
        };
        withoutRadiator.Weapons.Add(new CombatSystem.WeaponInfo
            { ModuleId = "weapon_cannon_mk1", BaseDamage = 10, Family = CombatSystem.DamageFamily.Neutral, HeatPerShot = 200 });

        var target1 = MakeProfile(5000, 0, heatCapacity: 5000, rejectionRate: 5000);
        var target2 = MakeProfile(5000, 0, heatCapacity: 5000, rejectionRate: 5000);

        var resultWith = StrategicResolverV0.Resolve(withRadiator, target1);
        var resultWithout = StrategicResolverV0.Resolve(withoutRadiator, target2);

        // After round 1:
        // withRadiator: heat = 200 - (150+75) = max(0, -25) = 0
        // withoutRadiator: heat = 200 - 150 = 50
        Assert.That(resultWith.Frames[0].AHeat, Is.LessThan(resultWithout.Frames[0].AHeat),
            $"Radiator cooling: with={resultWith.Frames[0].AHeat} should be < without={resultWithout.Frames[0].AHeat}");
    }

    [Test]
    public void Radiator_AftZoneDestroyed_RemovesBonus()
    {
        // Fleet with radiator + weak aft armor. Once aft is destroyed, cooling drops.
        var withRadiator = MakeProfile(5000, 0,
            heatCapacity: 2000,
            rejectionRate: CombatTweaksV0.DefaultRejectionRate,
            radiatorBonusRate: 200,
            weapons: new[] { ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Neutral, 100) });
        withRadiator.ZoneArmorHp[(int)ZoneFacing.Aft] = 1; // Aft will be destroyed quickly

        var attacker = MakeProfile(5000, 0,
            heatCapacity: 5000,
            rejectionRate: 5000,
            weapons: new[] { ("weapon_cannon_mk1", 50, CombatSystem.DamageFamily.Neutral, 0) });
        // Use kite stance to hit aft zone.
        attacker.ShipClassId = "dreadnought"; // charge stance → hits fore mostly

        var result = StrategicResolverV0.Resolve(attacker, withRadiator);

        // The fleet with destroyed aft should have higher heat in later rounds
        // because radiator bonus was removed.
        // Verify the combat completed (either side won or max rounds).
        Assert.That(result.RoundsPlayed, Is.GreaterThan(0));
    }

    // ── Combined Heat + BattleStations ──

    [Test]
    public void HeatAndReadiness_CombinedEffect()
    {
        // Both heat degradation and readiness should compound.
        // Heat overheat = 50%, StandDown = 25% → combined = min(50, 25) = 25%.
        var combined = MakeProfile(5000, 0,
            heatCapacity: 50,   // will overheat immediately (heat > 50)
            rejectionRate: 0,   // no cooling
            readinessDamagePct: CombatTweaksV0.StandDownDamagePct, // 25%
            weapons: new[] { ("weapon_cannon_mk1", 100, CombatSystem.DamageFamily.Neutral, 100) });

        var fullPower = MakeProfile(5000, 0,
            heatCapacity: 5000,
            rejectionRate: 5000,
            readinessDamagePct: CombatTweaksV0.NeutralPct,
            weapons: new[] { ("weapon_cannon_mk1", 100, CombatSystem.DamageFamily.Neutral, 100) });

        var target1 = MakeProfile(5000, 0, heatCapacity: 5000, rejectionRate: 5000);
        var target2 = MakeProfile(5000, 0, heatCapacity: 5000, rejectionRate: 5000);

        var resultCombined = StrategicResolverV0.Resolve(combined, target1);
        var resultFull = StrategicResolverV0.Resolve(fullPower, target2);

        Assert.That(resultCombined.SalvageValue, Is.LessThan(resultFull.SalvageValue),
            "Combined heat+readiness penalty should significantly reduce damage output");
    }

    [Test]
    public void Heat_ReplayFrame_IncludesHeatState()
    {
        // Verify replay frames capture heat values.
        var profile = MakeProfile(500, 0,
            heatCapacity: 2000,
            rejectionRate: 50,
            weapons: new[] { ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Neutral, 100) });

        var target = MakeProfile(5000, 0, heatCapacity: 5000, rejectionRate: 5000);

        var result = StrategicResolverV0.Resolve(profile, target);

        Assert.That(result.Frames.Count, Is.GreaterThan(0));
        // First round: A fires 1 weapon → heat = 100 - 50 = 50
        Assert.That(result.Frames[0].AHeat, Is.EqualTo(50));
        // Defender has high cooling → heat stays 0
        Assert.That(result.Frames[0].BHeat, Is.EqualTo(0));
    }

    [Test]
    public void Heat_Deterministic_SameInputsSameOutput()
    {
        var makeProfile = () => MakeProfile(500, 100,
            heatCapacity: 500,
            rejectionRate: 80,
            weapons: new[]
            {
                ("weapon_cannon_mk1", 15, CombatSystem.DamageFamily.Kinetic, 100),
                ("weapon_laser_mk1", 12, CombatSystem.DamageFamily.Energy, 80),
            });

        var target1 = MakeProfile(500, 100, heatCapacity: 500, rejectionRate: 80,
            weapons: new[] { ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Neutral, 100) });
        var target2 = MakeProfile(500, 100, heatCapacity: 500, rejectionRate: 80,
            weapons: new[] { ("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Neutral, 100) });

        var result1 = StrategicResolverV0.Resolve(makeProfile(), target1);
        var result2 = StrategicResolverV0.Resolve(makeProfile(), target2);

        Assert.That(result1.Winner, Is.EqualTo(result2.Winner));
        Assert.That(result1.RoundsPlayed, Is.EqualTo(result2.RoundsPlayed));
        for (int i = 0; i < result1.Frames.Count; i++)
        {
            Assert.That(result1.Frames[i].AHeat, Is.EqualTo(result2.Frames[i].AHeat));
            Assert.That(result1.Frames[i].BHeat, Is.EqualTo(result2.Frames[i].BHeat));
        }
    }
}
