using NUnit.Framework;

namespace SimCore.Tests.Determinism;

[TestFixture]
public sealed class MarketFeeDeterminismTests
{
    [Test]
    public void MarketFees_FromTweaks_MarketFeeMultiplier_ChangesTransactionFee_Deterministically()
    {
        static int RunOnce(string? tweakOverrideJson)
        {
            var s = new SimCore.SimState(seed: 1);
            s.LoadTweaksFromJsonOverride(tweakOverrideJson);

            // gross chosen so base fee is exact and easy to reason about:
            // base bps=100 => 1.00% of 10000 = 100 credits (ceil is still 100).
            return SimCore.Systems.MarketSystem.ComputeTransactionFeeCredits(s, grossCredits: 10_000);
        }

        var d0 = RunOnce(null);
        var d1 = RunOnce(null);
        Assert.That(d1, Is.EqualTo(d0));
        Assert.That(d0, Is.EqualTo(100));

        var overrideJson = "{\"version\":0,\"market_fee_multiplier\":1.25}";
        var o0 = RunOnce(overrideJson);
        var o1 = RunOnce(overrideJson);
        Assert.That(o1, Is.EqualTo(o0));
        Assert.That(o0, Is.EqualTo(125));

        Assert.That(o0, Is.Not.EqualTo(d0));
    }

    [Test]
    public void MarketFees_BrokerUnlock_WaivesTransactionFee_Deterministically()
    {
        static int RunOnceWithBrokerUnlock()
        {
            var s = new SimCore.SimState(seed: 1);

            var brokerId = "unlock_broker_fee_waiver_v0";
            s.Intel.Unlocks[brokerId] = new SimCore.Entities.UnlockContractV0
            {
                UnlockId = brokerId,
                Kind = SimCore.Entities.UnlockKind.Broker,
                IsAcquired = true,
                IsBlocked = false
            };

            return SimCore.Systems.MarketSystem.ComputeTransactionFeeCredits(s, grossCredits: 10_000);
        }

        var a0 = RunOnceWithBrokerUnlock();
        var a1 = RunOnceWithBrokerUnlock();
        Assert.That(a1, Is.EqualTo(a0));
        Assert.That(a0, Is.EqualTo(0));
    }
}
