using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S7.TERRITORY.REGIME_MODEL.001
public class TerritoryRegimeTests
{
    // Open policy + rep tier
    [TestCase(TradePolicy.Open, RepTier.Allied, TerritoryRegime.Open)]
    [TestCase(TradePolicy.Open, RepTier.Friendly, TerritoryRegime.Open)]
    [TestCase(TradePolicy.Open, RepTier.Neutral, TerritoryRegime.Guarded)]
    [TestCase(TradePolicy.Open, RepTier.Hostile, TerritoryRegime.Restricted)]
    [TestCase(TradePolicy.Open, RepTier.Enemy, TerritoryRegime.Hostile)]
    // Guarded policy + rep tier
    [TestCase(TradePolicy.Guarded, RepTier.Allied, TerritoryRegime.Guarded)]
    [TestCase(TradePolicy.Guarded, RepTier.Friendly, TerritoryRegime.Guarded)]
    [TestCase(TradePolicy.Guarded, RepTier.Neutral, TerritoryRegime.Restricted)]
    [TestCase(TradePolicy.Guarded, RepTier.Hostile, TerritoryRegime.Restricted)]
    [TestCase(TradePolicy.Guarded, RepTier.Enemy, TerritoryRegime.Hostile)]
    // Closed policy — always Hostile
    [TestCase(TradePolicy.Closed, RepTier.Allied, TerritoryRegime.Hostile)]
    [TestCase(TradePolicy.Closed, RepTier.Friendly, TerritoryRegime.Hostile)]
    [TestCase(TradePolicy.Closed, RepTier.Neutral, TerritoryRegime.Hostile)]
    [TestCase(TradePolicy.Closed, RepTier.Hostile, TerritoryRegime.Hostile)]
    [TestCase(TradePolicy.Closed, RepTier.Enemy, TerritoryRegime.Hostile)]
    public void ComputeRegime_Matrix(TradePolicy policy, RepTier rep, TerritoryRegime expected)
    {
        Assert.That(
            ReputationSystem.ComputeTerritoryRegime((int)policy, rep),
            Is.EqualTo(expected));
    }

    [Test]
    public void ComputeRegime_FromState_WithFaction()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        // Default rep = 0 = Neutral → Open+Neutral = Guarded
        Assert.That(
            ReputationSystem.ComputeTerritoryRegime(state, "node_a"),
            Is.EqualTo(TerritoryRegime.Guarded));
    }

    [Test]
    public void ComputeRegime_FromState_AlliedTrader()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        ReputationSystem.AdjustReputation(state, "faction_0", 80);
        Assert.That(
            ReputationSystem.ComputeTerritoryRegime(state, "node_a"),
            Is.EqualTo(TerritoryRegime.Open));
    }

    [Test]
    public void ComputeRegime_NoFaction_DefaultsOpen()
    {
        var state = new SimState(42);
        Assert.That(
            ReputationSystem.ComputeTerritoryRegime(state, "unowned_node"),
            Is.EqualTo(TerritoryRegime.Open));
    }

    [Test]
    public void ComputeRegime_NullState_DefaultsOpen()
    {
        Assert.That(
            ReputationSystem.ComputeTerritoryRegime(null, "node_a"),
            Is.EqualTo(TerritoryRegime.Open));
    }
}
