using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S7.WARFRONT.NEUTRALITY_TAX.001: Neutrality surcharge contract tests.
[TestFixture]
public class NeutralityTaxTests
{
    private SimState SetupWarZoneState(int playerRep, int warfrontIntensity)
    {
        var state = new SimState(42);
        // Create a node with a market.
        state.Nodes["node_a"] = new Node { MarketId = "mkt_a" };
        state.Markets["mkt_a"] = new Market();
        // Assign faction ownership.
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTariffRates["faction_0"] = 0.05f;
        state.FactionTradePolicy["faction_0"] = 0; // Open
        state.FactionAggressionLevel["faction_0"] = 0;
        // Set player reputation.
        ReputationSystem.AdjustReputation(state, "faction_0", playerRep);
        // Create warfront at specified intensity with node_a contested.
        state.Warfronts["wf_test"] = new WarfrontState
        {
            Id = "wf_test",
            CombatantA = "faction_0",
            CombatantB = "faction_1",
            Intensity = (WarfrontIntensity)warfrontIntensity,
            WarType = WarType.Hot,
            ContestedNodeIds = new List<string> { "node_a" },
        };
        return state;
    }

    [Test]
    public void NeutralPlayer_AtSkirmish_PaysNeutralityTax()
    {
        var state = SetupWarZoneState(0, 2); // Neutral rep, Skirmish
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");
        // Base tariff + war surcharge + neutrality tax.
        // Base: 0.05 * 10000 * (100-0)/100 = 500
        // War: 300 * 2 = 600
        // Neutrality: 500
        Assert.That(bps, Is.GreaterThanOrEqualTo(500 + 600 + 500));
    }

    [Test]
    public void FriendlyPlayer_NoNeutralityTax()
    {
        var state = SetupWarZoneState(30, 3); // Friendly rep, OpenWar
        int tariffWithFriendly = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");

        // Same setup but neutral rep.
        var stateNeutral = SetupWarZoneState(0, 3);
        int tariffNeutral = MarketSystem.GetEffectiveTariffBps(stateNeutral, "mkt_a");

        // Neutral player should pay more (neutrality tax).
        Assert.That(tariffNeutral, Is.GreaterThan(tariffWithFriendly));
    }

    [Test]
    public void NoWar_NoNeutralityTax()
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { MarketId = "mkt_a" };
        state.Markets["mkt_a"] = new Market();
        state.NodeFactionId["node_a"] = "faction_0";
        state.FactionTariffRates["faction_0"] = 0.05f;

        // No warfronts — should just be base tariff.
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");
        Assert.That(bps, Is.LessThanOrEqualTo(500)); // Just base tariff
    }

    [Test]
    public void TotalWar_MaxNeutralityTax()
    {
        var state = SetupWarZoneState(0, 4); // Neutral rep, TotalWar
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");
        // Should include maximum neutrality tax (1500 bps).
        int expectedMin = 500 + (300 * 4) + 1500; // base + war + neutrality
        Assert.That(bps, Is.GreaterThanOrEqualTo(expectedMin));
    }
}
