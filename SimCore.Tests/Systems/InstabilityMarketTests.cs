using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S7.INSTABILITY_EFFECTS.MARKET.001: Contract tests for instability market pricing.
[TestFixture]
public class InstabilityMarketTests
{
    private SimState MakeState(int instabilityLevel)
    {
        var state = new SimState();
        var node = new Node { Id = "n1", MarketId = "mkt1", InstabilityLevel = instabilityLevel };
        state.Nodes["n1"] = node;
        var market = new Market { Id = "mkt1" };
        market.Inventory["fuel"] = 50;
        market.Inventory["ore"] = 50;
        state.Markets["mkt1"] = market;
        return state;
    }

    [Test]
    public void StableNode_NoPriceModification()
    {
        var state = MakeState(0);
        int basePrice = MarketSystem.GetEffectivePrice("fuel", 50, null);
        int instPrice = MarketSystem.GetEffectivePrice(state, "mkt1", "fuel", 50, null);

        Assert.That(instPrice, Is.EqualTo(basePrice), "Stable node should not modify price.");
    }

    [Test]
    public void ShimmerNode_VolatilityApplied()
    {
        var state = MakeState(InstabilityTweaksV0.ShimmerMin); // level 25
        int basePrice = MarketSystem.GetEffectivePrice("ore", 50, null);
        int instPrice = MarketSystem.GetEffectivePrice(state, "mkt1", "ore", 50, null);

        Assert.That(instPrice, Is.GreaterThan(basePrice),
            "Shimmer node should increase price via volatility multiplier.");
    }

    [TestCase(0, 10000)]
    [TestCase(75, 12500)]  // 75/150 * 5000 = 2500 → 12500 bps = 1.25x
    [TestCase(99, 13300)]  // 99/150 * 5000 = 3300 → 13300 bps = 1.33x (max before Void closure)
    public void VolatilityMultiplier_ScalesLinearly(int instLevel, int expectedMultBps)
    {
        var state = MakeState(instLevel);
        int basePrice = MarketSystem.GetEffectivePrice("ore", 50, null);
        int instPrice = MarketSystem.GetEffectivePrice(state, "mkt1", "ore", 50, null);

        // Compute expected: basePrice * expectedMultBps / 10000
        int expected = (int)((long)basePrice * expectedMultBps / 10000);
        if (instLevel == 0) expected = basePrice; // no modification at stable

        Assert.That(instPrice, Is.EqualTo(expected),
            $"At instability={instLevel}, multiplier should be {expectedMultBps} bps.");
    }

    [Test]
    public void DriftPhase_SecurityGood_DemandSkew()
    {
        var state = MakeState(InstabilityTweaksV0.DriftMin); // level 50, phase 2
        int baseFuel = MarketSystem.GetEffectivePrice("fuel", 50, null);
        int baseOre = MarketSystem.GetEffectivePrice("ore", 50, null);
        int instFuel = MarketSystem.GetEffectivePrice(state, "mkt1", "fuel", 50, null);
        int instOre = MarketSystem.GetEffectivePrice(state, "mkt1", "ore", 50, null);

        // Both should have volatility, but fuel should have extra skew.
        Assert.That(instFuel, Is.GreaterThan(instOre),
            "Fuel (security good) should be more expensive than ore at Drift+ phase.");

        // Verify skew is applied: fuel should exceed base * volatility alone.
        int volatilityBps = 50 * InstabilityTweaksV0.VolatilityMaxBps / InstabilityTweaksV0.MaxInstability;
        int volatilityOnlyPrice = (int)((long)baseFuel * (10000 + volatilityBps) / 10000);
        Assert.That(instFuel, Is.GreaterThan(volatilityOnlyPrice),
            "Security good should have demand skew surcharge above volatility.");
    }

    [Test]
    public void FracturePhase_SecuritySkew_ScalesWithPhase()
    {
        var stateDrift = MakeState(InstabilityTweaksV0.DriftMin);       // phase 2
        var stateFracture = MakeState(InstabilityTweaksV0.FractureMin); // phase 3
        int driftFuel = MarketSystem.GetEffectivePrice(stateDrift, "mkt1", "fuel", 50, null);
        int fracFuel = MarketSystem.GetEffectivePrice(stateFracture, "mkt1", "fuel", 50, null);

        Assert.That(fracFuel, Is.GreaterThan(driftFuel),
            "Fracture phase should have higher security skew than Drift.");
    }

    [Test]
    public void MunitionsIsSecurityGood()
    {
        Assert.That(WellKnownGoodIds.IsSecurityGood("munitions"), Is.True);
        Assert.That(WellKnownGoodIds.IsSecurityGood("fuel"), Is.True);
        Assert.That(WellKnownGoodIds.IsSecurityGood("ore"), Is.False);
        Assert.That(WellKnownGoodIds.IsSecurityGood("metal"), Is.False);
    }

    [Test]
    public void VoidPhase_MarketClosed_ReturnsZero()
    {
        var state = MakeState(InstabilityTweaksV0.VoidMin); // level 100, phase 4
        int price = MarketSystem.GetEffectivePrice(state, "mkt1", "fuel", 50, null);

        Assert.That(price, Is.EqualTo(0), "Void phase should close market (price=0).");
    }

    [Test]
    public void NullState_ReturnsBasePrice()
    {
        int basePrice = MarketSystem.GetEffectivePrice("fuel", 50, null);
        int instPrice = MarketSystem.GetEffectivePrice(null, "mkt1", "fuel", 50, null);

        Assert.That(instPrice, Is.EqualTo(basePrice), "Null state should return base price.");
    }

    [Test]
    public void NonSecurityGood_NoDemandSkew()
    {
        var state = MakeState(InstabilityTweaksV0.FractureMin); // phase 3
        int baseOre = MarketSystem.GetEffectivePrice("ore", 50, null);
        int instOre = MarketSystem.GetEffectivePrice(state, "mkt1", "ore", 50, null);

        // Ore should only have volatility, no security skew.
        int volatilityBps = InstabilityTweaksV0.FractureMin * InstabilityTweaksV0.VolatilityMaxBps / InstabilityTweaksV0.MaxInstability;
        int expected = (int)((long)baseOre * (10000 + volatilityBps) / 10000);

        Assert.That(instOre, Is.EqualTo(expected),
            "Non-security good should have volatility only, no demand skew.");
    }
}
