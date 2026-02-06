using NUnit.Framework;
using SimCore.Time;

namespace SimCore.Tests.Time;

[TestFixture]
public sealed class TimeContractTests
{
	[Test]
	public void TimeContract_Constants_AreLocked()
	{
		Assert.That(SimTime.MinutesPerTick, Is.EqualTo(1));
		Assert.That(SimTime.GameMinutesPerRealSecond, Is.EqualTo(1));
		Assert.That(SimTime.SecondsPerGameMinute, Is.EqualTo(60));
		Assert.That(SimTime.GameMinutesPerGameHour, Is.EqualTo(60));
		Assert.That(SimTime.GameHoursPerGameDay, Is.EqualTo(24));
	}

	[Test]
	public void SimKernel_Step_AdvancesExactlyOneTick()
	{
		var k = new SimKernel(seed: 1);
		var s = k.State;

		Assert.That(s.Tick, Is.EqualTo(0));
		k.Step();
		Assert.That(s.Tick, Is.EqualTo(1));
		k.Step();
		Assert.That(s.Tick, Is.EqualTo(2));
	}
}
