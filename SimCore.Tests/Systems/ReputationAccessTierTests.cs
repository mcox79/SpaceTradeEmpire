using NUnit.Framework;
using SimCore;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S7.REPUTATION.ACCESS_TIERS.001
public class ReputationAccessTierTests
{
    [TestCase(100, RepTier.Allied)]
    [TestCase(75, RepTier.Allied)]
    [TestCase(74, RepTier.Friendly)]
    [TestCase(25, RepTier.Friendly)]
    [TestCase(24, RepTier.Neutral)]
    [TestCase(0, RepTier.Neutral)]
    [TestCase(-25, RepTier.Neutral)]
    [TestCase(-26, RepTier.Hostile)]
    [TestCase(-75, RepTier.Hostile)]
    [TestCase(-76, RepTier.Enemy)]
    [TestCase(-100, RepTier.Enemy)]
    public void GetRepTier_ClassifiesCorrectly(int rep, RepTier expected)
    {
        Assert.That(ReputationSystem.GetRepTier(rep), Is.EqualTo(expected));
    }

    [Test]
    public void GetRepTier_FromState()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", 80);
        Assert.That(ReputationSystem.GetRepTier(state, "faction_0"), Is.EqualTo(RepTier.Allied));
    }

    [TestCase(75, true)]   // Allied
    [TestCase(25, true)]   // Friendly
    [TestCase(0, true)]    // Neutral
    [TestCase(-50, true)]  // Hostile
    [TestCase(-75, true)]  // Hostile boundary
    [TestCase(-76, false)] // Enemy
    [TestCase(-100, false)]
    public void CanDock_BlocksEnemy(int rep, bool expected)
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", rep);
        Assert.That(ReputationSystem.CanDock(state, "faction_0"), Is.EqualTo(expected));
    }

    [TestCase(75, true)]   // Allied
    [TestCase(25, true)]   // Friendly
    [TestCase(0, true)]    // Neutral
    [TestCase(-25, true)]  // Neutral boundary
    [TestCase(-26, false)] // Hostile
    [TestCase(-75, false)] // Hostile
    [TestCase(-100, false)]// Enemy
    public void CanTrade_BlocksHostileAndEnemy(int rep, bool expected)
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", rep);
        Assert.That(ReputationSystem.CanTrade(state, "faction_0"), Is.EqualTo(expected));
    }

    [TestCase(100, true)]  // Allied
    [TestCase(75, true)]   // Allied boundary
    [TestCase(25, true)]   // Friendly boundary
    [TestCase(24, false)]  // Neutral
    [TestCase(0, false)]
    [TestCase(-50, false)]
    [TestCase(-100, false)]
    public void CanBuyTech_RequiresFriendlyOrHigher(int rep, bool expected)
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", rep);
        Assert.That(ReputationSystem.CanBuyTech(state, "faction_0"), Is.EqualTo(expected));
    }

    [Test]
    public void DefaultRep_IsNeutral()
    {
        var state = new SimState(42);
        Assert.That(ReputationSystem.GetRepTier(state, "faction_0"), Is.EqualTo(RepTier.Neutral));
        Assert.That(ReputationSystem.CanDock(state, "faction_0"), Is.True);
        Assert.That(ReputationSystem.CanTrade(state, "faction_0"), Is.True);
        Assert.That(ReputationSystem.CanBuyTech(state, "faction_0"), Is.False);
    }
}
