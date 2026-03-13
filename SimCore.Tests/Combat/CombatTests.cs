using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Combat;

[TestFixture]
[Category("CombatContract")]
public sealed class CombatDamageModelTests
{
	[Test]
	public void CalcDamage_KineticVsShields_HalfDamage()
	{
		var result = CombatSystem.CalcDamage(10, CombatSystem.DamageFamily.Kinetic, defenderShieldHp: 50, defenderHullHp: 100);

		// Kinetic vs shields = 10 * 50 / 100 = 5
		Assert.That(result.ShieldDmg, Is.EqualTo(5));
		Assert.That(result.HullDmg, Is.EqualTo(0));
		Assert.That(result.Overkill, Is.EqualTo(0));
	}

	[Test]
	public void CalcDamage_KineticVsHull_OneAndHalfDamage()
	{
		var result = CombatSystem.CalcDamage(10, CombatSystem.DamageFamily.Kinetic, defenderShieldHp: 0, defenderHullHp: 100);

		// Kinetic vs hull = 10 * 150 / 100 = 15
		Assert.That(result.ShieldDmg, Is.EqualTo(0));
		Assert.That(result.HullDmg, Is.EqualTo(15));
	}

	[Test]
	public void CalcDamage_EnergyVsShields_OneAndHalfDamage()
	{
		var result = CombatSystem.CalcDamage(10, CombatSystem.DamageFamily.Energy, defenderShieldHp: 50, defenderHullHp: 100);

		// Energy vs shields = 10 * 150 / 100 = 15
		Assert.That(result.ShieldDmg, Is.EqualTo(15));
		Assert.That(result.HullDmg, Is.EqualTo(0));
	}

	[Test]
	public void CalcDamage_EnergyVsHull_HalfDamage()
	{
		var result = CombatSystem.CalcDamage(10, CombatSystem.DamageFamily.Energy, defenderShieldHp: 0, defenderHullHp: 100);

		// Energy vs hull = 10 * 50 / 100 = 5
		Assert.That(result.ShieldDmg, Is.EqualTo(0));
		Assert.That(result.HullDmg, Is.EqualTo(5));
	}

	[Test]
	public void CalcDamage_NeutralFamily_NormalDamage()
	{
		var result = CombatSystem.CalcDamage(10, CombatSystem.DamageFamily.Neutral, defenderShieldHp: 0, defenderHullHp: 100);

		Assert.That(result.HullDmg, Is.EqualTo(10));
	}

	[Test]
	public void CalcDamage_ShieldOverflow_DamagesHull()
	{
		// Energy vs 3 shields, 100 hull. Energy does 15 to shields, overflow = 12.
		// Overflow converts at hull ratio: 12 * 50 / 150 = 4
		var result = CombatSystem.CalcDamage(10, CombatSystem.DamageFamily.Energy, defenderShieldHp: 3, defenderHullHp: 100);

		Assert.That(result.ShieldDmg, Is.EqualTo(3));
		Assert.That(result.HullDmg, Is.EqualTo(4));
	}

	[Test]
	public void CalcDamage_IsDeterministic()
	{
		var a = CombatSystem.CalcDamage(12, CombatSystem.DamageFamily.Kinetic, defenderShieldHp: 20, defenderHullHp: 50);
		var b = CombatSystem.CalcDamage(12, CombatSystem.DamageFamily.Kinetic, defenderShieldHp: 20, defenderHullHp: 50);

		Assert.That(a.ShieldDmg, Is.EqualTo(b.ShieldDmg));
		Assert.That(a.HullDmg, Is.EqualTo(b.HullDmg));
		Assert.That(a.Overkill, Is.EqualTo(b.Overkill));
	}

	[Test]
	public void CalcDamage_CounterRatio_KineticVsEnergy_DifferentEffectiveness()
	{
		int baseDmg = 20;

		var kinVsShield = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.Kinetic, defenderShieldHp: 100, defenderHullHp: 100);
		var enVsShield = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.Energy, defenderShieldHp: 100, defenderHullHp: 100);

		// Energy should do more shield damage than kinetic
		Assert.That(enVsShield.ShieldDmg, Is.GreaterThan(kinVsShield.ShieldDmg));

		var kinVsHull = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.Kinetic, defenderShieldHp: 0, defenderHullHp: 100);
		var enVsHull = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.Energy, defenderShieldHp: 0, defenderHullHp: 100);

		// Kinetic should do more hull damage than energy
		Assert.That(kinVsHull.HullDmg, Is.GreaterThan(enVsHull.HullDmg));
	}

	[Test]
	public void ClassifyWeapon_CannonIsKinetic()
	{
		Assert.That(CombatSystem.ClassifyWeapon("weapon_cannon_mk1"), Is.EqualTo(CombatSystem.DamageFamily.Kinetic));
	}

	[Test]
	public void ClassifyWeapon_LaserIsEnergy()
	{
		Assert.That(CombatSystem.ClassifyWeapon("weapon_laser_mk1"), Is.EqualTo(CombatSystem.DamageFamily.Energy));
	}

	[Test]
	public void ClassifyWeapon_UnknownIsNeutral()
	{
		Assert.That(CombatSystem.ClassifyWeapon("cap_module_refinery"), Is.EqualTo(CombatSystem.DamageFamily.Neutral));
		Assert.That(CombatSystem.ClassifyWeapon(""), Is.EqualTo(CombatSystem.DamageFamily.Neutral));
	}

	[Test]
	public void BuildProfile_ExtractsWeaponSlots()
	{
		var fleet = new Fleet { Id = "test_fleet" };
		CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);
		fleet.Slots.Add(new ModuleSlot { SlotId = "weapon_1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
		fleet.Slots.Add(new ModuleSlot { SlotId = "weapon_2", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });
		fleet.Slots.Add(new ModuleSlot { SlotId = "cargo_1", SlotKind = SlotKind.Cargo });

		var weaponDmg = new Dictionary<string, int>
		{
			["weapon_cannon_mk1"] = 12,
			["weapon_laser_mk1"] = 10,
		};
		var profile = CombatSystem.BuildProfile(fleet, weaponDmg);

		Assert.That(profile.HullHp, Is.EqualTo(CombatTweaksV0.DefaultHullHpMax));
		Assert.That(profile.ShieldHp, Is.EqualTo(CombatTweaksV0.DefaultShieldHpMax));
		Assert.That(profile.Weapons.Count, Is.EqualTo(2));
		Assert.That(profile.Weapons[0].Family, Is.EqualTo(CombatSystem.DamageFamily.Kinetic));
		Assert.That(profile.Weapons[0].BaseDamage, Is.EqualTo(12));
		Assert.That(profile.Weapons[1].Family, Is.EqualTo(CombatSystem.DamageFamily.Energy));
		Assert.That(profile.Weapons[1].BaseDamage, Is.EqualTo(10));
	}

	[Test]
	public void InitFleetCombatStats_PlayerDefaults()
	{
		var fleet = new Fleet { Id = "hero" };
		CombatSystem.InitFleetCombatStats(fleet, isPlayer: true);

		Assert.That(fleet.HullHp, Is.EqualTo(CombatTweaksV0.DefaultHullHpMax));
		Assert.That(fleet.HullHpMax, Is.EqualTo(CombatTweaksV0.DefaultHullHpMax));
		Assert.That(fleet.ShieldHp, Is.EqualTo(CombatTweaksV0.DefaultShieldHpMax));
		Assert.That(fleet.ShieldHpMax, Is.EqualTo(CombatTweaksV0.DefaultShieldHpMax));
	}

	[Test]
	public void InitFleetCombatStats_AiDefaults()
	{
		var fleet = new Fleet { Id = "pirate" };
		CombatSystem.InitFleetCombatStats(fleet, isPlayer: false);

		Assert.That(fleet.HullHp, Is.EqualTo(CombatTweaksV0.AiHullHpMax));
		Assert.That(fleet.ShieldHp, Is.EqualTo(CombatTweaksV0.AiShieldHpMax));
	}
}

// ── GATE.S5.COMBAT_LOCAL.COMBAT_TICK.001 ──

[TestFixture]
[Category("CombatTick")]
public sealed class CombatTickTests
{
	private static readonly Dictionary<string, int> WeaponDmg = new()
	{
		["weapon_cannon_mk1"] = 12,
		["weapon_laser_mk1"] = 10,
	};

	private static Fleet MakeArmedFleet(string id, bool isPlayer)
	{
		var fleet = new Fleet { Id = id };
		CombatSystem.InitFleetCombatStats(fleet, isPlayer);
		fleet.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
		fleet.Slots.Add(new ModuleSlot { SlotId = "w2", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });
		return fleet;
	}

	[Test]
	public void TickCombat_OneTick_ProducesEvents()
	{
		var player = MakeArmedFleet("hero", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		var log = new CombatSystem.CombatLog();

		CombatSystem.TickCombat(player, pirate, WeaponDmg, tick: 1, log);

		Assert.That(log.Events.Count, Is.GreaterThanOrEqualTo(2), "At least 2 events (player fires 2 weapons)");
		Assert.That(log.Events[0].AttackerId, Is.EqualTo("hero"));
		Assert.That(log.Events[0].Tick, Is.EqualTo(1));
	}

	[Test]
	public void TickCombat_DamageApplied_ToDefenderHp()
	{
		var player = MakeArmedFleet("hero", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		var log = new CombatSystem.CombatLog();

		int pirateTotalHpBefore = pirate.HullHp + pirate.ShieldHp;
		CombatSystem.TickCombat(player, pirate, WeaponDmg, tick: 1, log);
		int pirateTotalHpAfter = pirate.HullHp + pirate.ShieldHp;

		Assert.That(pirateTotalHpAfter, Is.LessThan(pirateTotalHpBefore));
	}

	[Test]
	public void RunEncounter_DeterministicOutcome()
	{
		var p1 = MakeArmedFleet("hero", true);
		var e1 = MakeArmedFleet("pirate_1", false);
		var log1 = CombatSystem.RunEncounter(p1, e1, WeaponDmg);

		var p2 = MakeArmedFleet("hero", true);
		var e2 = MakeArmedFleet("pirate_1", false);
		var log2 = CombatSystem.RunEncounter(p2, e2, WeaponDmg);

		Assert.That(log1.Outcome, Is.EqualTo(log2.Outcome));
		Assert.That(log1.Events.Count, Is.EqualTo(log2.Events.Count));
		Assert.That(log1.CauseOfDeath, Is.EqualTo(log2.CauseOfDeath));

		for (int i = 0; i < log1.Events.Count; i++)
		{
			Assert.That(log1.Events[i].DamageDealt, Is.EqualTo(log2.Events[i].DamageDealt));
			Assert.That(log1.Events[i].DefenderHullRemaining, Is.EqualTo(log2.Events[i].DefenderHullRemaining));
			Assert.That(log1.Events[i].DefenderShieldRemaining, Is.EqualTo(log2.Events[i].DefenderShieldRemaining));
		}
	}

	[Test]
	public void RunEncounter_Resolves_SomeoneWins()
	{
		var player = MakeArmedFleet("hero", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		var log = CombatSystem.RunEncounter(player, pirate, WeaponDmg);

		Assert.That(log.Outcome, Is.Not.EqualTo(CombatSystem.CombatOutcome.InProgress));
		Assert.That(log.Events.Count, Is.GreaterThan(0));
	}

	[Test]
	public void RunEncounter_EventLog_PopulatedCorrectly()
	{
		var player = MakeArmedFleet("hero", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		var log = CombatSystem.RunEncounter(player, pirate, WeaponDmg);

		foreach (var ev in log.Events)
		{
			Assert.That(ev.AttackerId, Is.Not.Empty);
			Assert.That(ev.DefenderId, Is.Not.Empty);
			Assert.That(ev.WeaponId, Is.Not.Empty);
			Assert.That(ev.Tick, Is.GreaterThan(0));
		}
	}
}

// ── GATE.S5.COMBAT_LOCAL.COMBAT_LOG.001 ──

[TestFixture]
[Category("CombatLog")]
public sealed class CombatLogTests
{
	private static readonly Dictionary<string, int> WeaponDmg = new()
	{
		["weapon_cannon_mk1"] = 12,
		["weapon_laser_mk1"] = 10,
	};

	private static Fleet MakeArmedFleet(string id, bool isPlayer)
	{
		var fleet = new Fleet { Id = id };
		CombatSystem.InitFleetCombatStats(fleet, isPlayer);
		fleet.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
		fleet.Slots.Add(new ModuleSlot { SlotId = "w2", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });
		return fleet;
	}

	[Test]
	public void CombatLog_Win_CauseOfDeathEmpty()
	{
		// Player has more HP (100/50) vs pirate (80/30) → player wins
		var player = MakeArmedFleet("hero", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		var log = CombatSystem.RunEncounter(player, pirate, WeaponDmg);

		Assert.That(log.Outcome, Is.EqualTo(CombatSystem.CombatOutcome.Win));
		Assert.That(log.CauseOfDeath, Is.Empty);
	}

	[Test]
	public void CombatLog_Loss_CauseOfDeathPopulated()
	{
		// Give pirate massive HP so player dies first
		var player = MakeArmedFleet("hero", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		pirate.HullHp = 5000;
		pirate.HullHpMax = 5000;
		pirate.ShieldHp = 5000;
		pirate.ShieldHpMax = 5000;

		var log = CombatSystem.RunEncounter(player, pirate, WeaponDmg);

		Assert.That(log.Outcome, Is.EqualTo(CombatSystem.CombatOutcome.Loss));
		Assert.That(log.CauseOfDeath, Does.Contain("hull destroyed by"));
		Assert.That(log.CauseOfDeath, Does.Contain("pirate_1"));
	}

	[Test]
	public void CombatLog_DeterministicReplay_IdenticalLog()
	{
		var p1 = MakeArmedFleet("hero", true);
		var e1 = MakeArmedFleet("pirate_1", false);
		var log1 = CombatSystem.RunEncounter(p1, e1, WeaponDmg);

		var p2 = MakeArmedFleet("hero", true);
		var e2 = MakeArmedFleet("pirate_1", false);
		var log2 = CombatSystem.RunEncounter(p2, e2, WeaponDmg);

		Assert.That(log1.Events.Count, Is.EqualTo(log2.Events.Count));
		Assert.That(log1.Outcome, Is.EqualTo(log2.Outcome));
		Assert.That(log1.CauseOfDeath, Is.EqualTo(log2.CauseOfDeath));
	}
}

// ── GATE.S5.COMBAT_LOCAL.BRIDGE_COMBAT.001: Combat bridge contract ──

[TestFixture]
[Category("CombatBridge")]
public sealed class CombatBridgeContractTests
{
	private static readonly Dictionary<string, int> WeaponDmg = new()
	{
		["weapon_cannon_mk1"] = 12,
		["weapon_laser_mk1"] = 10,
	};

	private static Fleet MakeArmedFleet(string id, bool isPlayer)
	{
		var fleet = new Fleet { Id = id };
		CombatSystem.InitFleetCombatStats(fleet, isPlayer);
		fleet.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" });
		fleet.Slots.Add(new ModuleSlot { SlotId = "w2", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });
		return fleet;
	}

	[Test]
	public void SimState_InCombat_DefaultFalse()
	{
		var state = new SimState();
		Assert.That(state.InCombat, Is.False);
		Assert.That(state.CombatOpponentId, Is.Null);
	}

	[Test]
	public void SimState_CombatLogs_StoreEncounterResult()
	{
		var state = new SimState();
		var player = MakeArmedFleet("fleet_trader_1", true);
		var pirate = MakeArmedFleet("pirate_1", false);
		state.Fleets["fleet_trader_1"] = player;
		state.Fleets["pirate_1"] = pirate;

		var log = CombatSystem.RunEncounter(player, pirate, WeaponDmg);
		state.CombatLogs.Add(log);

		Assert.That(state.CombatLogs.Count, Is.EqualTo(1));
		Assert.That(state.CombatLogs[0].Outcome, Is.Not.EqualTo(CombatSystem.CombatOutcome.InProgress));
		Assert.That(state.CombatLogs[0].Events.Count, Is.GreaterThan(0));
	}

	[Test]
	public void SimState_CombatFlags_SetDuringCombat()
	{
		var state = new SimState();
		state.InCombat = true;
		state.CombatOpponentId = "pirate_1";

		Assert.That(state.InCombat, Is.True);
		Assert.That(state.CombatOpponentId, Is.EqualTo("pirate_1"));
	}

	[Test]
	public void CombatLog_LastEntry_AccessibleViaIndexer()
	{
		var state = new SimState();
		var p1 = MakeArmedFleet("hero", true);
		var e1 = MakeArmedFleet("pirate_1", false);
		var log1 = CombatSystem.RunEncounter(p1, e1, WeaponDmg);
		state.CombatLogs.Add(log1);

		var p2 = MakeArmedFleet("hero", true);
		var e2 = MakeArmedFleet("pirate_2", false);
		var log2 = CombatSystem.RunEncounter(p2, e2, WeaponDmg);
		state.CombatLogs.Add(log2);

		var last = state.CombatLogs[^1];
		Assert.That(last, Is.SameAs(log2));
		Assert.That(last.Events.Any(e => e.AttackerId == "hero" || e.DefenderId == "hero"), Is.True);
	}
}

// ── GATE.S5.COMBAT.COUNTER_FAMILY.001: PointDefense counter family ──

[TestFixture]
[Category("CombatCounterFamily")]
public sealed class PointDefenseCounterFamilyTests
{
	[Test]
	public void CalcDamage_PointDefenseVsMissileTarget_AppliesDoubleBonus()
	{
		// PointDefense base damage 8, vs missile target → 2x bonus → effective 16.
		// No shield/hull multipliers on PointDefense (uses NeutralPct = 100).
		// All damage goes to hull (no shields).
		int baseDmg = 8;
		var result = CombatSystem.CalcDamage(
			baseDmg,
			CombatSystem.DamageFamily.PointDefense,
			defenderShieldHp: 0,
			defenderHullHp: 100,
			CombatSystem.TargetWeaponFamily.Missile);

		// effective = 8 * 200 / 100 = 16
		Assert.That(result.HullDmg, Is.EqualTo(16));
		Assert.That(result.ShieldDmg, Is.EqualTo(0));
	}

	[Test]
	public void CalcDamage_PointDefenseVsTorpedoTarget_AppliesDoubleBonus()
	{
		int baseDmg = 8;
		var result = CombatSystem.CalcDamage(
			baseDmg,
			CombatSystem.DamageFamily.PointDefense,
			defenderShieldHp: 0,
			defenderHullHp: 100,
			CombatSystem.TargetWeaponFamily.Torpedo);

		Assert.That(result.HullDmg, Is.EqualTo(16));
	}

	[Test]
	public void CalcDamage_PointDefenseVsCannonTarget_NoBonus()
	{
		// PointDefense vs cannon (Other) → base damage only, no counter bonus.
		int baseDmg = 8;
		var result = CombatSystem.CalcDamage(
			baseDmg,
			CombatSystem.DamageFamily.PointDefense,
			defenderShieldHp: 0,
			defenderHullHp: 100,
			CombatSystem.TargetWeaponFamily.Other);

		// No bonus: effective = 8 * 100 / 100 = 8
		Assert.That(result.HullDmg, Is.EqualTo(8));
	}

	[Test]
	public void CalcDamage_StandardWeaponVsMissileTarget_NoCounterBonus()
	{
		// Kinetic weapon vs missile target → standard kinetic multipliers, no counter bonus.
		int baseDmg = 10;
		var resultWithTarget = CombatSystem.CalcDamage(
			baseDmg,
			CombatSystem.DamageFamily.Kinetic,
			defenderShieldHp: 0,
			defenderHullHp: 100,
			CombatSystem.TargetWeaponFamily.Missile);

		var resultWithoutTarget = CombatSystem.CalcDamage(
			baseDmg,
			CombatSystem.DamageFamily.Kinetic,
			defenderShieldHp: 0,
			defenderHullHp: 100,
			CombatSystem.TargetWeaponFamily.Other);

		// Both should produce the same result (kinetic vs hull = 10 * 150 / 100 = 15).
		Assert.That(resultWithTarget.HullDmg, Is.EqualTo(15));
		Assert.That(resultWithoutTarget.HullDmg, Is.EqualTo(15));
		Assert.That(resultWithTarget.HullDmg, Is.EqualTo(resultWithoutTarget.HullDmg));
	}

	[Test]
	public void CalcDamage_PointDefense_IsDeterministic()
	{
		int baseDmg = 8;
		var a = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.PointDefense, 0, 100, CombatSystem.TargetWeaponFamily.Missile);
		var b = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.PointDefense, 0, 100, CombatSystem.TargetWeaponFamily.Missile);

		Assert.That(a.HullDmg, Is.EqualTo(b.HullDmg));
		Assert.That(a.ShieldDmg, Is.EqualTo(b.ShieldDmg));
	}

	[Test]
	public void ClassifyWeapon_PointDefenseModule_IsPointDefenseFamily()
	{
		Assert.That(
			CombatSystem.ClassifyWeapon("weapon_point_defense_mk1"),
			Is.EqualTo(CombatSystem.DamageFamily.PointDefense));
	}

	[Test]
	public void ClassifyTargetWeaponFamily_MissileModule_IsMissile()
	{
		Assert.That(
			CombatSystem.ClassifyTargetWeaponFamily("weapon_missile_mk1"),
			Is.EqualTo(CombatSystem.TargetWeaponFamily.Missile));
	}

	[Test]
	public void ClassifyTargetWeaponFamily_TorpedoModule_IsTorpedo()
	{
		Assert.That(
			CombatSystem.ClassifyTargetWeaponFamily("weapon_torpedo_mk1"),
			Is.EqualTo(CombatSystem.TargetWeaponFamily.Torpedo));
	}

	[Test]
	public void ClassifyTargetWeaponFamily_CannonModule_IsOther()
	{
		Assert.That(
			CombatSystem.ClassifyTargetWeaponFamily("weapon_cannon_mk1"),
			Is.EqualTo(CombatSystem.TargetWeaponFamily.Other));
	}

	[Test]
	public void CalcDamage_BackwardCompat_NoTargetFamilyOverload_SameBehavior()
	{
		// Existing CalcDamage(int, DamageFamily, int, int) must still work unchanged.
		// It defaults to TargetWeaponFamily.Other, so PointDefense gets no bonus.
		int baseDmg = 8;
		var result = CombatSystem.CalcDamage(baseDmg, CombatSystem.DamageFamily.PointDefense, 0, 100);

		// No target family supplied → Other → no bonus → 8 * 100 / 100 = 8
		Assert.That(result.HullDmg, Is.EqualTo(8));
	}
}

// ── GATE.S5.COMBAT.ESCORT_DOCTRINE.001: Escort doctrine v0 ──

[TestFixture]
[Category("CombatEscortDoctrine")]
public sealed class EscortDoctrineTests
{
	[Test]
	public void Fleet_SetEscortDoctrine_ActivatesWithTarget()
	{
		var escort = new Fleet { Id = "escort_fleet" };
		escort.SetEscortDoctrine("target_fleet");

		Assert.That(escort.EscortDoctrineActive, Is.True);
		Assert.That(escort.EscortTargetFleetId, Is.EqualTo("target_fleet"));
	}

	[Test]
	public void Fleet_ClearEscortDoctrine_Deactivates()
	{
		var escort = new Fleet { Id = "escort_fleet" };
		escort.SetEscortDoctrine("target_fleet");
		escort.ClearEscortDoctrine();

		Assert.That(escort.EscortDoctrineActive, Is.False);
		Assert.That(escort.EscortTargetFleetId, Is.Empty);
	}

	[Test]
	public void Fleet_SetEscortDoctrine_EmptyTarget_DoesNotActivate()
	{
		// Escort on non-existent (empty) target → doctrine stays inactive, no crash.
		var escort = new Fleet { Id = "escort_fleet" };
		escort.SetEscortDoctrine("");

		Assert.That(escort.EscortDoctrineActive, Is.False);
		Assert.That(escort.EscortTargetFleetId, Is.Empty);
	}

	[Test]
	public void Fleet_SetEscortDoctrine_WhitespaceTarget_DoesNotActivate()
	{
		// Whitespace-only target ID also treated as invalid.
		var escort = new Fleet { Id = "escort_fleet" };
		escort.SetEscortDoctrine("   ");

		Assert.That(escort.EscortDoctrineActive, Is.False);
	}

	[Test]
	public void Fleet_Default_EscortDoctrineInactive()
	{
		// No escort on a freshly constructed fleet (baseline: no bonus).
		var fleet = new Fleet { Id = "fleet_a" };

		Assert.That(fleet.EscortDoctrineActive, Is.False);
		Assert.That(fleet.EscortTargetFleetId, Is.Empty);
	}

	[Test]
	public void ApplyEscortShieldReduction_WithEscort_ReducesShieldDamage()
	{
		// Incoming shield damage 100 → reduced by 25% → 75.
		int incoming = 100;
		int reduced = CombatSystem.ApplyEscortShieldReduction(incoming);

		// 100 * (100 - 25) / 100 = 75
		Assert.That(reduced, Is.EqualTo(75));
	}

	[Test]
	public void ApplyEscortShieldReduction_ZeroDamage_StaysZero()
	{
		int reduced = CombatSystem.ApplyEscortShieldReduction(0);
		Assert.That(reduced, Is.EqualTo(0));
	}

	[Test]
	public void ApplyEscortShieldReduction_IsDeterministic()
	{
		int a = CombatSystem.ApplyEscortShieldReduction(40);
		int b = CombatSystem.ApplyEscortShieldReduction(40);
		Assert.That(a, Is.EqualTo(b));
	}

	[Test]
	public void EscortBonus_FleetWithEscort_TargetGetsBonusVsNoEscort()
	{
		// Demonstrate: with escort active the reduced shield damage is less than raw damage.
		int rawShieldDmg = 80;

		var target = new Fleet { Id = "target_fleet" };
		var escortFleet = new Fleet { Id = "escort_fleet" };
		escortFleet.SetEscortDoctrine(target.Id);

		// Simulate: shield damage to target is reduced because escort is active.
		int reducedDmg = escortFleet.EscortDoctrineActive && escortFleet.EscortTargetFleetId == target.Id
			? CombatSystem.ApplyEscortShieldReduction(rawShieldDmg)
			: rawShieldDmg;

		// Baseline: no escort, no reduction.
		var soloFleet = new Fleet { Id = "solo_fleet" };
		int baselineDmg = soloFleet.EscortDoctrineActive
			? CombatSystem.ApplyEscortShieldReduction(rawShieldDmg)
			: rawShieldDmg;

		Assert.That(reducedDmg, Is.LessThan(baselineDmg));
		Assert.That(reducedDmg, Is.EqualTo(60)); // 80 * 75 / 100 = 60
		Assert.That(baselineDmg, Is.EqualTo(80));
	}
}

// ── GATE.S5.COMBAT.STRATEGIC_RESOLVER.001 + GATE.S5.COMBAT.REPLAY_PROOF.001 ──

[TestFixture]
[Category("CombatStrategicResolver")]
public sealed class StrategicResolverTests
{
	// Build a CombatProfile directly from explicit parameters (no Fleet dependency needed).
	private static CombatSystem.CombatProfile MakeProfile(
		int hullHp, int shieldHp,
		params (string moduleId, int baseDamage, CombatSystem.DamageFamily family)[] weapons)
	{
		var profile = new CombatSystem.CombatProfile
		{
			HullHp = hullHp,
			HullHpMax = hullHp,
			ShieldHp = shieldHp,
			ShieldHpMax = shieldHp,
		};
		foreach (var (moduleId, baseDamage, family) in weapons)
		{
			profile.Weapons.Add(new CombatSystem.WeaponInfo
			{
				ModuleId = moduleId,
				BaseDamage = baseDamage,
				Family = family,
			});
		}
		return profile;
	}

	[Test]
	public void StrategicResolver_StrongerFleetWins()
	{
		// Fleet A: 3 weapons, Fleet B: 1 weapon — A should win.
		var profileA = MakeProfile(100, 50,
			("weapon_cannon_mk1", 15, CombatSystem.DamageFamily.Kinetic),
			("weapon_cannon_mk2", 15, CombatSystem.DamageFamily.Kinetic),
			("weapon_cannon_mk3", 15, CombatSystem.DamageFamily.Kinetic));

		var profileB = MakeProfile(100, 50,
			("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Kinetic));

		var result = StrategicResolverV0.Resolve(profileA, profileB);

		Assert.That(result.Winner, Is.EqualTo(StrategicResolverV0.Winner.A),
			"Fleet A with 3 weapons should defeat Fleet B with 1 weapon");
	}

	[Test]
	public void StrategicResolver_EqualFleets_DrawOrLong()
	{
		// Identical fleets — A fires first so has first-mover advantage.
		// Expect A wins or Draw, and battle takes multiple rounds.
		var profileA = MakeProfile(100, 50,
			("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Kinetic));
		var profileB = MakeProfile(100, 50,
			("weapon_cannon_mk1", 10, CombatSystem.DamageFamily.Kinetic));

		var result = StrategicResolverV0.Resolve(profileA, profileB);

		// With identical stats, A wins via first-mover advantage or it's a draw.
		bool validOutcome = result.Winner == StrategicResolverV0.Winner.A
			|| result.Winner == StrategicResolverV0.Winner.Draw;
		Assert.That(validOutcome, Is.True,
			$"Expected A-wins or draw; got winner={result.Winner} rounds={result.RoundsPlayed}");
		Assert.That(result.RoundsPlayed, Is.GreaterThan(1),
			"Equal fleets should fight for more than 1 round");
	}

	[Test]
	public void StrategicResolver_MaxRoundsEnforced()
	{
		// Tanky fleets with zero weapons → neither can deal damage → goes full 50 rounds → Draw.
		var profileA = MakeProfile(hullHp: 10000, shieldHp: 10000 /* no weapons */);
		var profileB = MakeProfile(hullHp: 10000, shieldHp: 10000 /* no weapons */);

		var result = StrategicResolverV0.Resolve(profileA, profileB);

		Assert.That(result.RoundsPlayed, Is.EqualTo(CombatTweaksV0.StrategicMaxRounds),
			"Should play exactly the max number of rounds");
		Assert.That(result.Winner, Is.EqualTo(StrategicResolverV0.Winner.Draw),
			"Neither fleet destroyed → Draw");
	}

	[Test]
	public void StrategicResolver_EscortReducesShieldDamage()
	{
		// Fleet A attacks Fleet B which is escorted.
		// Fleet B (escorted) should survive longer (more hull remaining) than without escort.
		var weaponA = ("weapon_cannon_mk1", 20, CombatSystem.DamageFamily.Kinetic);

		// B has shields; escort reduces incoming shield damage.
		var profileA = MakeProfile(100, 0, weaponA);
		var profileB_escorted = MakeProfile(50, 100, ("weapon_cannon_mk1", 1, CombatSystem.DamageFamily.Kinetic));
		var profileB_alone = MakeProfile(50, 100, ("weapon_cannon_mk1", 1, CombatSystem.DamageFamily.Kinetic));

		var resultEscorted = StrategicResolverV0.Resolve(profileA, profileB_escorted, fleetBEscorted: true);
		var resultAlone = StrategicResolverV0.Resolve(profileA, profileB_alone, fleetBEscorted: false);

		// Escorted B should take fewer total rounds to outlast, or have more hull remaining.
		// In all cases, escorted B should survive more rounds (or the same if B dies either way,
		// but never fewer rounds when escorted).
		Assert.That(resultEscorted.RoundsPlayed, Is.GreaterThanOrEqualTo(resultAlone.RoundsPlayed),
			"Escorted fleet should last at least as long");
	}

	[Test]
	public void StrategicResolver_PointDefenseCounterBonus()
	{
		// Fleet A uses PointDefense weapons. Fleet B has missile modules.
		// PointDefense vs missile target → 2x counter bonus → A destroys B faster.
		var profileA_pd = MakeProfile(100, 0,
			("weapon_point_defense_mk1", 8, CombatSystem.DamageFamily.PointDefense));
		var profileA_normal = MakeProfile(100, 0,
			("weapon_cannon_mk1", 8, CombatSystem.DamageFamily.Kinetic));

		// B has missile weapons (makes PD bonus apply) and low HP for fast resolution.
		var profileB_missiles = MakeProfile(80, 0,
			("weapon_missile_mk1", 5, CombatSystem.DamageFamily.Neutral));

		var resultPd = StrategicResolverV0.Resolve(profileA_pd, profileB_missiles);
		var resultNormal = StrategicResolverV0.Resolve(profileA_normal, profileB_missiles);

		// PD fleet should win in fewer or equal rounds against missile fleet.
		Assert.That(resultPd.Winner, Is.EqualTo(StrategicResolverV0.Winner.A),
			"PD fleet should win against missile fleet");
		Assert.That(resultPd.RoundsPlayed, Is.LessThanOrEqualTo(resultNormal.RoundsPlayed),
			"PD counter bonus should resolve combat in fewer or equal rounds");
	}

	[Test]
	public void StrategicResolver_IsDeterministic()
	{
		var profileA = MakeProfile(100, 40,
			("weapon_cannon_mk1", 12, CombatSystem.DamageFamily.Kinetic),
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));
		var profileB = MakeProfile(80, 30,
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));

		var r1 = StrategicResolverV0.Resolve(profileA, profileB);
		var r2 = StrategicResolverV0.Resolve(profileA, profileB);

		Assert.That(r1.Winner, Is.EqualTo(r2.Winner));
		Assert.That(r1.RoundsPlayed, Is.EqualTo(r2.RoundsPlayed));
		Assert.That(r1.FleetAHullRemaining, Is.EqualTo(r2.FleetAHullRemaining));
		Assert.That(r1.FleetBHullRemaining, Is.EqualTo(r2.FleetBHullRemaining));
		Assert.That(r1.SalvageValue, Is.EqualTo(r2.SalvageValue));
		Assert.That(r1.Frames.Count, Is.EqualTo(r2.Frames.Count));
	}

	// ── GATE.S5.COMBAT.REPLAY_PROOF.001 ──

	[Test]
	public void StrategicReplay_FrameCountMatchesRounds()
	{
		var profileA = MakeProfile(100, 40,
			("weapon_cannon_mk1", 12, CombatSystem.DamageFamily.Kinetic));
		var profileB = MakeProfile(80, 30,
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));

		var result = StrategicResolverV0.Resolve(profileA, profileB);

		Assert.That(result.Frames.Count, Is.EqualTo(result.RoundsPlayed),
			"One ReplayFrame per round played");
	}

	[Test]
	public void StrategicReplay_GoldenHash()
	{
		// Fixed inputs → fixed SHA256 of serialized frames.
		// GATE.S5.COMBAT.REPLAY_PROOF.001
		// Profile: A has kinetic + energy weapons vs B with energy weapon.
		var profileA = MakeProfile(100, 40,
			("weapon_cannon_mk1", 12, CombatSystem.DamageFamily.Kinetic),
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));
		var profileB = MakeProfile(80, 30,
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));

		var result = StrategicResolverV0.Resolve(profileA, profileB);
		string hash1 = StrategicResolverV0.ComputeFrameHash(result.Frames);

		// Replay from identical inputs.
		var profileA2 = MakeProfile(100, 40,
			("weapon_cannon_mk1", 12, CombatSystem.DamageFamily.Kinetic),
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));
		var profileB2 = MakeProfile(80, 30,
			("weapon_laser_mk1", 10, CombatSystem.DamageFamily.Energy));

		var result2 = StrategicResolverV0.Resolve(profileA2, profileB2);
		string hash2 = StrategicResolverV0.ComputeFrameHash(result2.Frames);

		// Both runs must produce byte-identical frame hashes (replay consistency proof).
		// GATE.S5.COMBAT.REPLAY_PROOF.001: same inputs → same output → byte-identical.
		Assert.That(hash1, Is.EqualTo(hash2), "Deterministic replay must yield identical frame hash");

		// Golden hash locked to the value produced by the v0 resolver algorithm.
		// NOTE: Update this value if the resolver algorithm is intentionally changed.
		// To find the current hash, run the test with goldenLocked=false and read the output.
		// GATE.S5.COMBAT.REPLAY_PROOF.001: Golden hash locked.
		const string GoldenHash = "3793469812a89366b188c9c09896e33f17ca3353269a393d3ca7f95148bf4228";
		Assert.That(hash1, Is.EqualTo(GoldenHash), "Frame hash must match golden value");
	}
}

// ── GATE.S5.COMBAT.SLICE_CLOSE.001: Slice 5 content wave scenario proof ──

[TestFixture]
[Category("CombatSliceClose")]
public sealed class CombatSliceCloseTests
{
	private static CombatSystem.CombatProfile MakeProfile(
		int hullHp, int shieldHp,
		params (string moduleId, int baseDamage, CombatSystem.DamageFamily family)[] weapons)
	{
		var profile = new CombatSystem.CombatProfile
		{
			HullHp = hullHp, HullHpMax = hullHp,
			ShieldHp = shieldHp, ShieldHpMax = shieldHp,
		};
		foreach (var (moduleId, baseDamage, family) in weapons)
			profile.Weapons.Add(new CombatSystem.WeaponInfo { ModuleId = moduleId, BaseDamage = baseDamage, Family = family });
		return profile;
	}

	[Test]
	public void SliceClose_EscortPlusAttackerWithPD_FullScenario()
	{
		// GATE.S5.COMBAT.SLICE_CLOSE.001: E2E scenario with doctrines.
		// Fleet A: attacker with point-defense cannon (counter bonus vs missiles).
		// Fleet B: missile carrier, escorted (shield damage reduction).
		// Verify: PD counter bonus applies, escort bonus applies, replay is deterministic.
		var attackerPD = MakeProfile(100, 50,
			("weapon_point_defense_mk1", CombatTweaksV0.PointDefenseBaseDamage, CombatSystem.DamageFamily.PointDefense));
		var escortedMissile = MakeProfile(80, 40,
			("weapon_missile_mk1", CombatTweaksV0.DefaultWeaponBaseDamage, CombatSystem.DamageFamily.Neutral));

		// Resolve WITH escort on fleet B.
		var resultEscorted = StrategicResolverV0.Resolve(attackerPD, escortedMissile, fleetAEscorted: false, fleetBEscorted: true);

		// Resolve WITHOUT escort for comparison.
		var attackerPD2 = MakeProfile(100, 50,
			("weapon_point_defense_mk1", CombatTweaksV0.PointDefenseBaseDamage, CombatSystem.DamageFamily.PointDefense));
		var unescortedMissile = MakeProfile(80, 40,
			("weapon_missile_mk1", CombatTweaksV0.DefaultWeaponBaseDamage, CombatSystem.DamageFamily.Neutral));
		var resultUnescorted = StrategicResolverV0.Resolve(attackerPD2, unescortedMissile, fleetAEscorted: false, fleetBEscorted: false);

		// PD counter bonus: fleet A fires point_defense vs missile target → 2x damage.
		// This means A should win in both cases.
		Assert.That(resultEscorted.Winner, Is.EqualTo(StrategicResolverV0.Winner.A),
			"PD attacker should beat missile carrier (even escorted)");
		Assert.That(resultUnescorted.Winner, Is.EqualTo(StrategicResolverV0.Winner.A),
			"PD attacker should beat unescorted missile carrier");

		// Escort makes fleet B last longer (more rounds needed).
		Assert.That(resultEscorted.RoundsPlayed, Is.GreaterThanOrEqualTo(resultUnescorted.RoundsPlayed),
			"Escort should make B survive at least as many rounds");

		// Replay determinism: both results have valid frame sequences.
		Assert.That(resultEscorted.Frames.Count, Is.EqualTo(resultEscorted.RoundsPlayed));
		Assert.That(resultUnescorted.Frames.Count, Is.EqualTo(resultUnescorted.RoundsPlayed));

		// Golden hash for the escorted scenario (determinism anchor for slice 5).
		string hashEscorted = StrategicResolverV0.ComputeFrameHash(resultEscorted.Frames);
		string hashEscorted2 = StrategicResolverV0.ComputeFrameHash(
			StrategicResolverV0.Resolve(
				MakeProfile(100, 50, ("weapon_point_defense_mk1", CombatTweaksV0.PointDefenseBaseDamage, CombatSystem.DamageFamily.PointDefense)),
				MakeProfile(80, 40, ("weapon_missile_mk1", CombatTweaksV0.DefaultWeaponBaseDamage, CombatSystem.DamageFamily.Neutral)),
				fleetAEscorted: false, fleetBEscorted: true).Frames);
		Assert.That(hashEscorted, Is.EqualTo(hashEscorted2),
			"Slice 5 scenario must be replay-deterministic");
	}
}

// GATE.S5.COMBAT_RES.SYSTEM.001: Combat resolution contract tests.
[TestFixture]
[Category("CombatResolution")]
public sealed class CombatResolutionTests
{
	private Fleet MakeFleet(string id, int hull, int shield, FleetRole role = FleetRole.Trader)
	{
		var f = new Fleet
		{
			Id = id,
			Role = role,
			HullHp = hull,
			HullHpMax = hull,
			ShieldHp = shield,
			ShieldHpMax = shield,
		};
		f.Slots.Add(new ModuleSlot { SlotId = "w1", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_laser_mk1" });
		return f;
	}

	[Test]
	public void ResolveCombatV0_StrongAttacker_Victory()
	{
		var attacker = MakeFleet("att", CombatTweaksV0.DefaultHullHpMax, CombatTweaksV0.DefaultShieldHpMax);
		var defender = MakeFleet("def", 20, 10); // weak defender

		var result = CombatSystem.ResolveCombatV0(attacker, defender);

		Assert.That(result.Outcome, Is.EqualTo(CombatSystem.CombatResolutionOutcome.Victory));
		Assert.That(result.AttackerId, Is.EqualTo("att"));
		Assert.That(result.DefenderId, Is.EqualTo("def"));
		Assert.That(result.RoundsPlayed, Is.GreaterThan(0));
		Assert.That(result.AttackerHullRemaining, Is.GreaterThan(0));
	}

	[Test]
	public void ResolveCombatV0_WeakAttacker_Defeat()
	{
		var attacker = MakeFleet("att", 20, 10); // weak attacker
		var defender = MakeFleet("def", CombatTweaksV0.DefaultHullHpMax, CombatTweaksV0.DefaultShieldHpMax);

		var result = CombatSystem.ResolveCombatV0(attacker, defender);

		Assert.That(result.Outcome, Is.EqualTo(CombatSystem.CombatResolutionOutcome.Defeat));
		Assert.That(result.DefenderHullRemaining, Is.GreaterThan(0));
	}

	[Test]
	public void ResolveCombatV0_EqualForces_ResolvesDeterministically()
	{
		var a1 = MakeFleet("a", 80, 30);
		var b1 = MakeFleet("b", 80, 30);
		var result1 = CombatSystem.ResolveCombatV0(a1, b1);

		var a2 = MakeFleet("a", 80, 30);
		var b2 = MakeFleet("b", 80, 30);
		var result2 = CombatSystem.ResolveCombatV0(a2, b2);

		Assert.That(result1.Outcome, Is.EqualTo(result2.Outcome));
		Assert.That(result1.RoundsPlayed, Is.EqualTo(result2.RoundsPlayed));
		Assert.That(result1.AttackerHullRemaining, Is.EqualTo(result2.AttackerHullRemaining));
	}

	[Test]
	public void ResolveCombatV0_FleeOutcome_MaxRoundsReached()
	{
		// Both fleets identical and strong — neither should destroy the other quickly
		// Use very high HP to exceed max rounds
		var attacker = MakeFleet("a", 10000, 5000);
		var defender = MakeFleet("b", 10000, 5000);

		var result = CombatSystem.ResolveCombatV0(attacker, defender);

		// With very high HP, likely hits max rounds → Draw → Flee
		Assert.That(result.RoundsPlayed, Is.EqualTo(CombatTweaksV0.StrategicMaxRounds));
		Assert.That(result.Outcome, Is.EqualTo(CombatSystem.CombatResolutionOutcome.Flee));
	}

	[Test]
	public void ResolveCombatV0_SalvageValue_NonNegative()
	{
		var attacker = MakeFleet("a", 100, 50);
		var defender = MakeFleet("b", 40, 20);

		var result = CombatSystem.ResolveCombatV0(attacker, defender);

		Assert.That(result.SalvageValue, Is.GreaterThanOrEqualTo(0));
	}
}
