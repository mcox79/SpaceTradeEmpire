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

    // GATE.S7.TERRITORY.HYSTERESIS.001: Hysteresis tests.

    private static void AdvanceN(SimState state, int n)
    {
        for (int i = 0; i < n; i++) state.AdvanceTick(); // STRUCTURAL: loop
    }

    [Test]
    public void Hysteresis_InitialAssignment_CommitsImmediately()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        // Default rep=0 => Neutral => Open+Neutral = Guarded.
        ReputationSystem.ProcessRegimeHysteresis(state);
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Guarded));
        Assert.That(ReputationSystem.GetEffectiveRegime(state, "node_a"), Is.EqualTo(TerritoryRegime.Guarded));
    }

    [Test]
    public void Hysteresis_Worsening_CommitsInstantly()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        // Start with Allied => Open.
        ReputationSystem.AdjustReputation(state, "faction_0", 80);
        ReputationSystem.ProcessRegimeHysteresis(state);
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Open));

        // Drop rep to hostile range => raw becomes Restricted.
        state.FactionReputation["faction_0"] = -50;
        ReputationSystem.ProcessRegimeHysteresis(state);
        // Worsening should commit instantly (no delay).
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Restricted));
    }

    [Test]
    public void Hysteresis_Improvement_RequiresSustainedTicks()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        state.FactionReputation["faction_0"] = -50; // Hostile tier => Restricted.
        ReputationSystem.ProcessRegimeHysteresis(state);
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Restricted));

        // Improve rep to Neutral => raw would be Guarded (improvement from Restricted).
        state.FactionReputation["faction_0"] = 0;
        ReputationSystem.ProcessRegimeHysteresis(state);
        // Still Restricted — improvement not yet sustained. Proposal starts at tick 0.
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Restricted));
        Assert.That(state.NodeRegimeProposed["node_a"], Is.EqualTo((int)TerritoryRegime.Guarded));

        // Advance to threshold - 1.
        int threshold = SimCore.Tweaks.FactionTweaksV0.RegimeHysteresisMinTicks;
        AdvanceN(state, threshold - 1); // STRUCTURAL: boundary - tick now = threshold-1
        ReputationSystem.ProcessRegimeHysteresis(state);
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Restricted));

        // One more tick to reach threshold.
        state.AdvanceTick(); // tick = threshold
        ReputationSystem.ProcessRegimeHysteresis(state);
        // Now improvement commits.
        Assert.That(state.NodeRegimeCommitted["node_a"], Is.EqualTo((int)TerritoryRegime.Guarded));
        // Proposal cleared.
        Assert.That(state.NodeRegimeProposed.ContainsKey("node_a"), Is.False);
    }

    [Test]
    public void Hysteresis_ImprovementReset_WhenRawOscillates()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        state.FactionReputation["faction_0"] = -50; // Restricted.
        ReputationSystem.ProcessRegimeHysteresis(state);

        // Start improvement proposal at tick 0.
        state.FactionReputation["faction_0"] = 0; // Neutral => Guarded.
        ReputationSystem.ProcessRegimeHysteresis(state);
        Assert.That(state.NodeRegimeProposed["node_a"], Is.EqualTo((int)TerritoryRegime.Guarded));
        int proposalTick = state.Tick;

        // Advance a few ticks, then change proposal direction.
        AdvanceN(state, 5); // STRUCTURAL: arbitrary advance
        state.FactionReputation["faction_0"] = 30; // Friendly => Open (different improvement target).
        ReputationSystem.ProcessRegimeHysteresis(state);
        // New proposal for Open, timer restarted.
        Assert.That(state.NodeRegimeProposed["node_a"], Is.EqualTo((int)TerritoryRegime.Open));
        Assert.That(state.NodeRegimeProposedSinceTick["node_a"], Is.EqualTo(state.Tick));
    }

    [Test]
    public void GetEffectiveRegime_NoCommitted_FallsBackToComputed()
    {
        var state = new SimState(42);
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTradePolicy["faction_0"] = (int)TradePolicy.Open;
        ReputationSystem.AdjustReputation(state, "faction_0", 80);
        // No hysteresis processed yet — no committed value.
        Assert.That(ReputationSystem.GetEffectiveRegime(state, "node_a"), Is.EqualTo(TerritoryRegime.Open));
    }

    [Test]
    public void GetEffectiveRegime_NullState_ReturnsOpen()
    {
        Assert.That(ReputationSystem.GetEffectiveRegime(null, "node_a"), Is.EqualTo(TerritoryRegime.Open));
    }
}
