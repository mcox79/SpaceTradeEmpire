using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Tests.Systems;

// GATE.S4.TECH.CORE.001: Contract tests for tech state, content, and prerequisite validation.
[TestFixture]
[Category("TechState")]
public sealed class TechStateTests
{
    [Test]
    public void TechState_DefaultsEmpty()
    {
        var ts = new TechState();
        Assert.That(ts.UnlockedTechIds, Is.Not.Null);
        Assert.That(ts.UnlockedTechIds, Has.Count.EqualTo(0));
        Assert.That(ts.CurrentResearchTechId, Is.EqualTo(""));
        Assert.That(ts.ResearchProgressTicks, Is.EqualTo(0));
        Assert.That(ts.IsResearching, Is.False);
    }

    [Test]
    public void TechState_IsResearching_TrueWhenActive()
    {
        var ts = new TechState { CurrentResearchTechId = "improved_thrusters" };
        Assert.That(ts.IsResearching, Is.True);
    }

    [Test]
    public void TechContentV0_AllTechs_NonEmpty()
    {
        Assert.That(TechContentV0.AllTechs, Has.Count.GreaterThanOrEqualTo(5));
        foreach (var tech in TechContentV0.AllTechs)
        {
            Assert.That(tech.TechId, Is.Not.Empty, "TechId must not be empty");
            Assert.That(tech.ResearchTicks, Is.GreaterThan(0), $"Tech {tech.TechId} must have positive ResearchTicks");
            Assert.That(tech.CreditCost, Is.GreaterThan(0), $"Tech {tech.TechId} must have positive CreditCost");
        }
    }

    [Test]
    public void TechContentV0_GetById_ReturnsCorrect()
    {
        var def = TechContentV0.GetById("improved_thrusters");
        Assert.That(def, Is.Not.Null);
        Assert.That(def!.DisplayName, Is.EqualTo("Improved Thrusters"));
    }

    [Test]
    public void TechContentV0_GetById_NullForUnknown()
    {
        Assert.That(TechContentV0.GetById("nonexistent"), Is.Null);
        Assert.That(TechContentV0.GetById(""), Is.Null);
        Assert.That(TechContentV0.GetById(null!), Is.Null);
    }

    [Test]
    public void TechContentV0_PrerequisitesMet_NoPrereqs()
    {
        var unlocked = new HashSet<string>();
        Assert.That(TechContentV0.PrerequisitesMet("improved_thrusters", unlocked), Is.True);
    }

    [Test]
    public void TechContentV0_PrerequisitesMet_RequiresUnlock()
    {
        var unlocked = new HashSet<string>();
        Assert.That(TechContentV0.PrerequisitesMet("advanced_refining", unlocked), Is.False);

        unlocked.Add("improved_thrusters");
        Assert.That(TechContentV0.PrerequisitesMet("advanced_refining", unlocked), Is.True);
    }

    [Test]
    public void TechContentV0_FractureDrive_RequiresMultiplePrereqs()
    {
        var unlocked = new HashSet<string> { "shield_mk2" };
        Assert.That(TechContentV0.PrerequisitesMet("fracture_drive", unlocked), Is.False);

        unlocked.Add("weapon_systems_2");
        Assert.That(TechContentV0.PrerequisitesMet("fracture_drive", unlocked), Is.True);
    }

    [Test]
    public void TechContentV0_UniqueIds()
    {
        var ids = TechContentV0.AllTechs.Select(t => t.TechId).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count), "Tech IDs must be unique");
    }

    [Test]
    public void SimState_HasTechState()
    {
        var state = new SimState(42);
        Assert.That(state.Tech, Is.Not.Null);
        Assert.That(state.Tech.UnlockedTechIds, Has.Count.EqualTo(0));
    }

    [Test]
    public void ResearchTweaksV0_Constants_Positive()
    {
        Assert.That(SimCore.Tweaks.ResearchTweaksV0.CreditCostPerTickBase, Is.GreaterThan(0));
        Assert.That(SimCore.Tweaks.ResearchTweaksV0.MaxConcurrentResearch, Is.EqualTo(1));
        Assert.That(SimCore.Tweaks.ResearchTweaksV0.ProgressPerTick, Is.GreaterThan(0));
    }
}
