using NUnit.Framework;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class BandedTimeContractTests
{
        [Test]
        public void BAND_001_band_ticks_thresholds_are_stable()
        {
                // Use a stable ticksPerDay for the contract
                int tpd = IndustrySystem.TicksPerDay;
                Assert.That(tpd, Is.GreaterThan(0));

                int tph = System.Math.Max(1, tpd / 24);

                Assert.That(BandedTime.BandTicks(-1, tpd), Is.EqualTo("?"));
                Assert.That(BandedTime.BandTicks(int.MaxValue, tpd), Is.EqualTo("INF"));

                Assert.That(BandedTime.BandTicks(0, tpd), Is.EqualTo("NOW"));
                Assert.That(BandedTime.BandTicks(1, tpd), Is.EqualTo("<1h"));
                Assert.That(BandedTime.BandTicks(1 * tph - 1, tpd), Is.EqualTo("<1h"));
                Assert.That(BandedTime.BandTicks(1 * tph, tpd), Is.EqualTo("<6h"));

                Assert.That(BandedTime.BandTicks(6 * tph - 1, tpd), Is.EqualTo("<6h"));
                Assert.That(BandedTime.BandTicks(6 * tph, tpd), Is.EqualTo("<1d"));

                Assert.That(BandedTime.BandTicks(24 * tph - 1, tpd), Is.EqualTo("<1d"));
                Assert.That(BandedTime.BandTicks(24 * tph, tpd), Is.EqualTo("<3d"));

                Assert.That(BandedTime.BandTicks(3 * tpd - 1, tpd), Is.EqualTo("<3d"));
                Assert.That(BandedTime.BandTicks(3 * tpd, tpd), Is.EqualTo("<7d"));

                Assert.That(BandedTime.BandTicks(7 * tpd - 1, tpd), Is.EqualTo("<7d"));
                Assert.That(BandedTime.BandTicks(7 * tpd, tpd), Is.EqualTo("7d+"));
        }

        [Test]
        public void BAND_002_band_days_thresholds_are_stable()
        {
                Assert.That(BandedTime.BandDays(float.NaN), Is.EqualTo("?"));
                Assert.That(BandedTime.BandDays(-0.01f), Is.EqualTo("?"));
                Assert.That(BandedTime.BandDays(float.PositiveInfinity), Is.EqualTo("INF"));

                Assert.That(BandedTime.BandDays(0f), Is.EqualTo("NOW"));
                Assert.That(BandedTime.BandDays(0.01f), Is.EqualTo("<1h"));
                Assert.That(BandedTime.BandDays((1f / 24f) - 0.0001f), Is.EqualTo("<1h"));
                Assert.That(BandedTime.BandDays((1f / 24f)), Is.EqualTo("<6h"));
        }
}
