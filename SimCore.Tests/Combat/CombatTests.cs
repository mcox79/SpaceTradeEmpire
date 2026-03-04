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
