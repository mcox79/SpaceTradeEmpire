using System.Collections.Generic;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S4.TECH.SAVE.001: Round-trip save/load tests for TechState and refit slots.
[TestFixture]
[Category("SaveLoadTech")]
public sealed class SaveLoadTechTests
{
    [Test]
    public void TechState_SurvivesRoundTrip()
    {
        var state = new SimState(42);
        state.Tech.UnlockedTechIds.Add("improved_thrusters");
        state.Tech.CurrentResearchTechId = "shield_mk2";
        state.Tech.ResearchProgressTicks = 5;
        state.Tech.ResearchTotalTicks = 12;
        state.Tech.ResearchCreditsSpent = 25;
        state.Tech.EventLog.Add(new TechEvent
        {
            Seq = 1, Tick = 10, TechId = "improved_thrusters", EventType = "Completed"
        });
        state.Tech.NextEventSeq = 2;

        var json = SerializationSystem.Serialize(state);
        var loaded = SerializationSystem.Deserialize(json);

        Assert.That(loaded.Tech, Is.Not.Null);
        Assert.That(loaded.Tech.UnlockedTechIds.Contains("improved_thrusters"), Is.True);
        Assert.That(loaded.Tech.CurrentResearchTechId, Is.EqualTo("shield_mk2"));
        Assert.That(loaded.Tech.ResearchProgressTicks, Is.EqualTo(5));
        Assert.That(loaded.Tech.ResearchTotalTicks, Is.EqualTo(12));
        Assert.That(loaded.Tech.ResearchCreditsSpent, Is.EqualTo(25));
        Assert.That(loaded.Tech.EventLog, Has.Count.EqualTo(1));
        Assert.That(loaded.Tech.EventLog[0].EventType, Is.EqualTo("Completed"));
        Assert.That(loaded.Tech.NextEventSeq, Is.EqualTo(2));
    }

    [Test]
    public void RefitSlots_SurviveRoundTrip()
    {
        var state = new SimState(42);
        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            Slots = new List<ModuleSlot>
            {
                new ModuleSlot { SlotId = "weapon_0", SlotKind = SlotKind.Weapon, InstalledModuleId = "weapon_cannon_mk1" },
                new ModuleSlot { SlotId = "engine_0", SlotKind = SlotKind.Engine, InstalledModuleId = null },
                new ModuleSlot { SlotId = "utility_0", SlotKind = SlotKind.Utility, InstalledModuleId = "shield_mk2" },
            }
        };

        var json = SerializationSystem.Serialize(state);
        var loaded = SerializationSystem.Deserialize(json);

        Assert.That(loaded.Fleets.ContainsKey("fleet_trader_1"), Is.True);
        var fleet = loaded.Fleets["fleet_trader_1"];
        Assert.That(fleet.Slots, Has.Count.EqualTo(3));
        Assert.That(fleet.Slots[0].SlotId, Is.EqualTo("weapon_0"));
        Assert.That(fleet.Slots[0].InstalledModuleId, Is.EqualTo("weapon_cannon_mk1"));
        Assert.That(fleet.Slots[1].InstalledModuleId, Is.Null);
        Assert.That(fleet.Slots[2].InstalledModuleId, Is.EqualTo("shield_mk2"));
    }

    [Test]
    public void EmptyTechState_SurvivesRoundTrip()
    {
        var state = new SimState(42);
        var json = SerializationSystem.Serialize(state);
        var loaded = SerializationSystem.Deserialize(json);

        Assert.That(loaded.Tech, Is.Not.Null);
        Assert.That(loaded.Tech.UnlockedTechIds, Has.Count.EqualTo(0));
        Assert.That(loaded.Tech.IsResearching, Is.False);
    }
}
