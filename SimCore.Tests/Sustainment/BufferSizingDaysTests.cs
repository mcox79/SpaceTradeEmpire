using NUnit.Framework;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests.Sustainment;

public class BufferSizingDaysTests
{
    [Test]
    public void BufferTarget_Uses_Days_Converted_To_Ticks()
    {
        var site = new IndustrySite
        {
            Id = "tech0",
            NodeId = "n0",
            Inputs = new Dictionary<string, int>
            {
                { "ore", 2 },
                { "fuel", 1 }
            },
            BufferDays = 2
        };

        // 1 day = 1440 ticks; ore target = 2 * 2 * 1440 = 5760
        int oreTarget = IndustrySystem.ComputeBufferTargetUnits(site, "ore");
        int fuelTarget = IndustrySystem.ComputeBufferTargetUnits(site, "fuel");

        Assert.That(oreTarget, Is.EqualTo(2 * 2 * IndustrySystem.TicksPerDay));
        Assert.That(fuelTarget, Is.EqualTo(1 * 2 * IndustrySystem.TicksPerDay));
    }
}
