using NUnit.Framework;
using SimCore;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S7.REPUTATION.PRICING_CURVES.001
public class ReputationPricingTests
{
    [Test]
    public void Allied_GetsDiscount()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", 80);
        int bps = MarketSystem.GetRepPricingBps(state, "faction_0");
        Assert.That(bps, Is.EqualTo(FactionTweaksV0.AlliedPriceBps));
        Assert.That(bps, Is.LessThan(0)); // discount
    }

    [Test]
    public void Friendly_GetsSmallDiscount()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", 30);
        int bps = MarketSystem.GetRepPricingBps(state, "faction_0");
        Assert.That(bps, Is.EqualTo(FactionTweaksV0.FriendlyPriceBps));
    }

    [Test]
    public void Neutral_NoPriceChange()
    {
        var state = new SimState(42);
        int bps = MarketSystem.GetRepPricingBps(state, "faction_0");
        Assert.That(bps, Is.EqualTo(0));
    }

    [Test]
    public void Hostile_GetsSurcharge()
    {
        var state = new SimState(42);
        ReputationSystem.AdjustReputation(state, "faction_0", -30);
        int bps = MarketSystem.GetRepPricingBps(state, "faction_0");
        Assert.That(bps, Is.EqualTo(FactionTweaksV0.HostilePriceBps));
        Assert.That(bps, Is.GreaterThan(0)); // surcharge
    }

    [Test]
    public void ApplyRepPricing_AlliedDiscount()
    {
        // -15% of 1000 = 850
        int adjusted = MarketSystem.ApplyRepPricing(1000, FactionTweaksV0.AlliedPriceBps);
        Assert.That(adjusted, Is.EqualTo(850));
    }

    [Test]
    public void ApplyRepPricing_HostileSurcharge()
    {
        // +20% of 1000 = 1200
        int adjusted = MarketSystem.ApplyRepPricing(1000, FactionTweaksV0.HostilePriceBps);
        Assert.That(adjusted, Is.EqualTo(1200));
    }

    [Test]
    public void ApplyRepPricing_NeutralNoChange()
    {
        int adjusted = MarketSystem.ApplyRepPricing(1000, 0);
        Assert.That(adjusted, Is.EqualTo(1000));
    }

    [Test]
    public void ApplyRepPricing_FloorAtOne()
    {
        // Extreme discount on cheap item should floor at 1
        int adjusted = MarketSystem.ApplyRepPricing(1, -9000);
        Assert.That(adjusted, Is.EqualTo(1));
    }

    [Test]
    public void NullFaction_ReturnsZeroBps()
    {
        var state = new SimState(42);
        Assert.That(MarketSystem.GetRepPricingBps(state, ""), Is.EqualTo(0));
        Assert.That(MarketSystem.GetRepPricingBps(null, "faction_0"), Is.EqualTo(0));
    }
}
