using NUnit.Framework;
using SimCore;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

public class ReputationSystemTests
{
    [Test]
    public void GetReputation_DefaultsToZero()
    {
        var state = new SimState(42);
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(0));
    }

    [Test]
    public void AdjustReputation_Increases()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", 10);
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(10));
    }

    [Test]
    public void AdjustReputation_Decreases()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", -30);
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(-30));
    }

    [Test]
    public void AdjustReputation_ClampsToMax100()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", 200);
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(100));
    }

    [Test]
    public void AdjustReputation_ClampsToMinNeg100()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", -200);
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(-100));
    }

    [Test]
    public void OnTradeAtFactionStation_IncreasesRep()
    {
        var state = new SimState(42);
        ReputationSystem.OnTradeAtFactionStation(state, "faction_0");
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"),
            Is.EqualTo(FactionTweaksV0.TradeRepGain));
    }

    [Test]
    public void OnAttackFactionShip_DecreasesRep()
    {
        var state = new SimState(42);
        ReputationSystem.OnAttackFactionShip(state, "faction_0");
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"),
            Is.EqualTo(FactionTweaksV0.AttackRepLoss));
    }

    [Test]
    public void MultipleTrades_AccumulateRep()
    {
        var state = new SimState(42);
        for (int i = 0; i < 50; i++)
            ReputationSystem.OnTradeAtFactionStation(state, "faction_1");
        Assert.That(ReputationSystem.GetReputation(state, "faction_1"), Is.EqualTo(50));
    }

    [Test]
    public void Reputation_IndependentPerFaction()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", 30);
        ReputationSystem.AdjustReputation(state, "faction_1", -20);
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(30));
        Assert.That(ReputationSystem.GetReputation(state, "faction_1"), Is.EqualTo(-20));
        Assert.That(ReputationSystem.GetReputation(state, "faction_2"), Is.EqualTo(0));
    }

    [Test]
    public void Reputation_InSignature()
    {
        var a = new SimState(42);
        var b = new SimState(42);
        Assert.That(a.GetSignature(), Is.EqualTo(b.GetSignature()));

        ReputationSystem.AdjustReputation(a, "faction_0", 10);
        Assert.That(a.GetSignature(), Is.Not.EqualTo(b.GetSignature()));
    }
}
